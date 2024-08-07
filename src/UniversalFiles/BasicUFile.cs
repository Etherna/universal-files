// Copyright 2023-present Etherna SA
// This file is part of UniversalFiles.
// 
// UniversalFiles is free software: you can redistribute it and/or modify it under the terms of the
// GNU Lesser General Public License as published by the Free Software Foundation,
// either version 3 of the License, or (at your option) any later version.
// 
// UniversalFiles is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
// without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License along with UniversalFiles.
// If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Etherna.UniversalFiles
{
    public class BasicUFile(
        BasicUUri fileUri,
        IHttpClientFactory httpClientFactory)
        : UFile(fileUri)
    {
        // Protected methods.
        protected override async Task<(bool Result, (byte[] ByteArray, Encoding? Encoding)? ContentCache)> ExistsAsync(
            string absoluteUri,
            UUriKind absoluteUriKind)
        {
            switch (absoluteUriKind)
            {
                case UUriKind.LocalAbsolute:
                    return (File.Exists(absoluteUri) || Directory.Exists(absoluteUri), null);

                case UUriKind.OnlineAbsolute:
                    // Try to get resource byte size with an HEAD request.
                    var byteSyze = await TryGetOnlineByteSizeWithHeadRequestAsync(absoluteUri).ConfigureAwait(false);
                    if (byteSyze.HasValue)
                        return (true, null);

                    // Otherwise, try to download it.
                    var onlineContent = await TryGetOnlineAsByteArrayAsync(absoluteUri).ConfigureAwait(false);
                    return (onlineContent != null, onlineContent);

                default: throw new InvalidOperationException(
                    "Invalid absolute uri kind. It should be well defined and absolute");
            }
        }

        protected override async Task<(long Result, (byte[] ByteArray, Encoding? Encoding)? ContentCache)> GetByteSizeAsync(
            string absoluteUri,
            UUriKind absoluteUriKind)
        {
            switch (absoluteUriKind)
            {
                case UUriKind.LocalAbsolute:
                    return (new FileInfo(absoluteUri).Length, null);

                case UUriKind.OnlineAbsolute:
                    // Try to get resource byte size with an HEAD request.
                    var byteSyze = await TryGetOnlineByteSizeWithHeadRequestAsync(absoluteUri).ConfigureAwait(false);
                    if (byteSyze.HasValue)
                        return (byteSyze.Value, null);

                    // Otherwise, try to download it.
                    var onlineContent = await TryGetOnlineAsByteArrayAsync(absoluteUri).ConfigureAwait(false) ??
                                        throw new IOException($"Can't retrieve online resource at {absoluteUri}");
                    return (onlineContent.ByteArray.LongLength, onlineContent);

                default: throw new InvalidOperationException(
                    "Invalid absolute uri kind. It should be well defined and absolute");
            }
        }

        protected override async Task<(byte[] ByteArray, Encoding? Encoding)> ReadToByteArrayAsync(
            string absoluteUri,
            UUriKind absoluteUriKind)
        {
            switch (absoluteUriKind)
            {
                case UUriKind.LocalAbsolute:
                    return (await File.ReadAllBytesAsync(absoluteUri).ConfigureAwait(false), null);

                case UUriKind.OnlineAbsolute:
                    return await TryGetOnlineAsByteArrayAsync(absoluteUri).ConfigureAwait(false) ??
                           throw new IOException($"Can't retrieve online resource at {absoluteUri}");

                default: throw new InvalidOperationException(
                    "Invalid absolute uri kind. It should be well defined and absolute");
            }
        }

        protected override async Task<(Stream Stream, Encoding? Encoding)> ReadToStreamAsync(
            string absoluteUri,
            UUriKind absoluteUriKind)
        {
            switch (absoluteUriKind)
            {
                case UUriKind.LocalAbsolute:
                    return (File.OpenRead(absoluteUri), null);
                
                case UUriKind.OnlineAbsolute:
                    return await TryGetOnlineAsStreamAsync(absoluteUri).ConfigureAwait(false) ??
                           throw new IOException($"Can't retrieve online resource at {absoluteUri}");
                
                default: throw new InvalidOperationException(
                    "Invalid absolute uri kind. It should be well defined and absolute");
            }
        }

        protected override Task<string?> TryGetFileNameAsync(string originalUri)
        {
            ArgumentNullException.ThrowIfNull(originalUri, nameof(originalUri));
            
            if (originalUri.EndsWith('/') ||
                originalUri.EndsWith('\\'))
                return Task.FromResult<string?>(null);
            return Task.FromResult<string?>(originalUri.Split('/', '\\').Last());
        }

        // Helpers.
        private async Task<(byte[] ByteArray, Encoding? Encoding)?> TryGetOnlineAsByteArrayAsync(
            string onlineAbsoluteUri)
        {
            var result = await TryGetOnlineAsStreamAsync(onlineAbsoluteUri).ConfigureAwait(false);
            if (result is null)
                return null;

            var (contentStream, encoding) = result.Value;
            
            // Copy stream to memory stream.
            using var memoryStream = new MemoryStream();
            await contentStream.CopyToAsync(memoryStream).ConfigureAwait(false);
            memoryStream.Position = 0;
            
            var byteArrayContent = memoryStream.ToArray();
            await contentStream.DisposeAsync().ConfigureAwait(false);

            return (byteArrayContent, encoding);
        }
        
        private async Task<(Stream Stream, Encoding? Encoding)?> TryGetOnlineAsStreamAsync(
            string onlineAbsoluteUri)
        {
            try
            {
                using var httpClient = httpClientFactory.CreateClient();
                using var response = await httpClient.GetAsync(onlineAbsoluteUri).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return null;

                // Get content with encoding.
                var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                Encoding? contentEncoding = null;

                // Try to extract the encoding from the Content-Type header.
                if (response.Content.Headers.ContentType?.CharSet != null)
                {
                    try { contentEncoding = Encoding.GetEncoding(response.Content.Headers.ContentType.CharSet); }
                    catch (ArgumentException) { }
                }

                return (contentStream, contentEncoding);
            }
            catch
            {
                return null;
            }
        }
        
        private async Task<long?> TryGetOnlineByteSizeWithHeadRequestAsync(string absoluteUri)
        {
            try
            {
                using var httpClient = httpClientFactory.CreateClient();
                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Head, absoluteUri);
                using var response = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);

                if (response.Headers.TryGetValues("Content-Length", out var values))
                {
                    using var enumerator = values.GetEnumerator();
                    if (long.TryParse(enumerator.Current, out var byteSize))
                        return byteSize;
                }
            }
            catch { }

            return default;
        }
    }
}
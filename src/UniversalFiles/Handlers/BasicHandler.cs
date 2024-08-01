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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Etherna.UniversalFiles.Handlers
{
    /// <summary>
    /// Supports local and online files with http protocol
    /// </summary>
    public class BasicHandler(
        IHttpClientFactory httpClientFactory)
        : IHandler
    {
        // Methods.
        public async Task<(bool Result, (byte[] ByteArray, Encoding? Encoding)? ContentCache)> ExistsAsync(
            string absoluteUri,
            UniversalUriKind absoluteUriKind)
        {
            switch (absoluteUriKind)
            {
                case UniversalUriKind.LocalAbsolute:
                    return (File.Exists(absoluteUri) || Directory.Exists(absoluteUri), null);

                case UniversalUriKind.OnlineAbsolute:
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

        public async Task<(long Result, (byte[] ByteArray, Encoding? Encoding)? ContentCache)> GetByteSizeAsync(
            string absoluteUri,
            UniversalUriKind absoluteUriKind)
        {
            switch (absoluteUriKind)
            {
                case UniversalUriKind.LocalAbsolute:
                    return (new FileInfo(absoluteUri).Length, null);

                case UniversalUriKind.OnlineAbsolute:
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

        public UniversalUriKind GetUriKind(string uri)
        {
            ArgumentNullException.ThrowIfNull(uri, nameof(uri));

            var uriKind = UniversalUriKind.None;

            if (uri.Length > 0)
            {
                //test online absolute
                if (Uri.TryCreate(uri, UriKind.Absolute, out var onlineAbsUriResult) &&
                    (onlineAbsUriResult.Scheme == Uri.UriSchemeHttp || onlineAbsUriResult.Scheme == Uri.UriSchemeHttps))
                    uriKind |= UniversalUriKind.OnlineAbsolute;

                //test online relative
                if (Uri.TryCreate(uri, UriKind.Relative, out var _))
                    uriKind |= UniversalUriKind.OnlineRelative;

                //test local absolute and relative
                if ((uriKind & UniversalUriKind.OnlineAbsolute) == 0)
                {
                    uriKind |= Path.IsPathRooted(uri) ?
                        UniversalUriKind.LocalAbsolute :
                        UniversalUriKind.LocalRelative;
                }
            }

            return uriKind;
        }

        public async Task<(byte[] ByteArray, Encoding? Encoding)> ReadToByteArrayAsync(
            string absoluteUri,
            UniversalUriKind absoluteUriKind)
        {
            switch (absoluteUriKind)
            {
                case UniversalUriKind.LocalAbsolute:
                    return (await File.ReadAllBytesAsync(absoluteUri).ConfigureAwait(false), null);

                case UniversalUriKind.OnlineAbsolute:
                    return await TryGetOnlineAsByteArrayAsync(absoluteUri).ConfigureAwait(false) ??
                           throw new IOException($"Can't retrieve online resource at {absoluteUri}");

                default: throw new InvalidOperationException(
                    "Invalid absolute uri kind. It should be well defined and absolute");
            }
        }

        public async Task<(Stream Stream, Encoding? Encoding)> ReadToStreamAsync(
            string absoluteUri,
            UniversalUriKind absoluteUriKind)
        {
            switch (absoluteUriKind)
            {
                case UniversalUriKind.LocalAbsolute:
                    return (File.OpenRead(absoluteUri), null);
                
                case UniversalUriKind.OnlineAbsolute:
                    return await TryGetOnlineAsStreamAsync(absoluteUri).ConfigureAwait(false) ??
                           throw new IOException($"Can't retrieve online resource at {absoluteUri}");
                
                default: throw new InvalidOperationException(
                    "Invalid absolute uri kind. It should be well defined and absolute");
            }
        }

        public Task<string?> TryGetFileNameAsync(string originalUri)
        {
            ArgumentNullException.ThrowIfNull(originalUri, nameof(originalUri));
            
            if (originalUri.EndsWith('/') ||
                originalUri.EndsWith('\\'))
                return Task.FromResult<string?>(null);
            return Task.FromResult<string?>(originalUri.Split('/', '\\').Last());
        }

        public (string AbsoluteUri, UniversalUriKind UriKind)? TryGetParentDirectoryAsAbsoluteUri(
            string absoluteUri,
            UniversalUriKind absoluteUriKind)
        {
            switch (absoluteUriKind)
            {
                case UniversalUriKind.LocalAbsolute:
                    var dirName = Path.GetDirectoryName(absoluteUri);
                    return dirName is null ? null :
                        (dirName, UniversalUriKind.LocalAbsolute);

                case UniversalUriKind.OnlineAbsolute:
                    var segments = new Uri(absoluteUri, UriKind.Absolute).Segments;
                    return segments.Length == 1 ? null : //if it's already root, return null
                        (absoluteUri[..^segments.Last().Length], UniversalUriKind.OnlineAbsolute);

                default: throw new InvalidOperationException("Invalid absolute uri kind. It should be well defined and absolute");
            }
        }

        public (string AbsoluteUri, UniversalUriKind UriKind) UriToAbsoluteUri(
            string originalUri,
            string? baseDirectory,
            UniversalUriKind uriKind)
        {
            ArgumentNullException.ThrowIfNull(originalUri, nameof(originalUri));
            
            // Verify base directory is absolute.
            if ((uriKind & UniversalUriKind.Relative) != 0 &&
                baseDirectory != null &&
                (GetUriKind(baseDirectory) & UniversalUriKind.Absolute) == 0)
                throw new InvalidOperationException("If uri kind can be relative and base directory is present, it must be absolute");
            
            // Resolve absolute url.
            return uriKind switch
            {
                UniversalUriKind.LocalAbsolute =>
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                    !Path.IsPathFullyQualified(originalUri) && //Ex: "/test"
                    baseDirectory is not null && Path.IsPathFullyQualified(baseDirectory) ?
                        (Path.GetFullPath(originalUri, baseDirectory), UniversalUriKind.LocalAbsolute) : //take unit from base directory
                        (Path.GetFullPath(originalUri), UniversalUriKind.LocalAbsolute),

                UniversalUriKind.LocalRelative =>
                    (Path.GetFullPath(
                            originalUri,
                            baseDirectory is not null ?
                                Path.GetFullPath(baseDirectory) : //GetFullPath is required when on windows baseDirectory is a root path without unit name. Ex: "/test"
                                Directory.GetCurrentDirectory()),
                        UniversalUriKind.LocalAbsolute),

                UniversalUriKind.OnlineAbsolute => (new Uri(originalUri, System.UriKind.Absolute).ToString(), UniversalUriKind.OnlineAbsolute),

                UniversalUriKind.OnlineRelative => (new Uri(
                    new Uri(baseDirectory!, UriKind.Absolute),
                    string.Join('/', originalUri.Split('/', '\\').Select(Uri.EscapeDataString))).ToString(), UniversalUriKind.OnlineAbsolute),

                _ => throw new InvalidOperationException("Can't find a valid uri kind")
            };
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
                    string contentLength = values.GetEnumerator().Current;
                    if (long.TryParse(contentLength, out var byteSize))
                        return byteSize;
                }
            }
            catch { }

            return default;
        }
    }
}
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

using Etherna.BeeNet;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Etherna.UniversalFiles
{
    public class SwarmUFile(
        IBeeClient beeClient,
        UUri fileUri)
        : UFile(fileUri)
    {
        protected override async Task<(bool Result, (byte[] ByteArray, Encoding? Encoding)? ContentCache)> ExistsAsync(
            string absoluteUri,
            UUriKind absoluteUriKind)
        {
            if (absoluteUriKind != UUriKind.OnlineAbsolute)
                throw new InvalidOperationException(
                    "Invalid online absolute uri kind. It can't be casted to SwarmAddress");

            // Try to get file head.
            try
            {
                var headers = await beeClient.TryGetFileHeadersAsync(absoluteUri).ConfigureAwait(false);
                if (headers is null)
                    return (false, null);
            }
            catch { return (false, null); }
            
            return (true, null);
        }

        protected override async Task<(long Result, (byte[] ByteArray, Encoding? Encoding)? ContentCache)> GetByteSizeAsync(
            string absoluteUri,
            UUriKind absoluteUriKind)
        {
            var size = await beeClient.TryGetFileSizeAsync(absoluteUri).ConfigureAwait(false);
            if (size is null)
                throw new InvalidOperationException();
            return (size.Value, null);
        }

        protected override async Task<(byte[] ByteArray, Encoding? Encoding)> ReadToByteArrayAsync(
            string absoluteUri,
            UUriKind absoluteUriKind)
        {
            var (contentStream, encoding) = await ReadToStreamAsync(absoluteUri, absoluteUriKind).ConfigureAwait(false);
            
            // Copy stream to memory stream.
            using var memoryStream = new MemoryStream();
            await contentStream.CopyToAsync(memoryStream).ConfigureAwait(false);
            memoryStream.Position = 0;
            
            var byteArrayContent = memoryStream.ToArray();
            await contentStream.DisposeAsync().ConfigureAwait(false);

            return (byteArrayContent, encoding);
        }

        protected override async Task<(Stream Stream, Encoding? Encoding)> ReadToStreamAsync(
            string absoluteUri,
            UUriKind absoluteUriKind)
        {
            var result = await beeClient.GetFileAsync(absoluteUri).ConfigureAwait(false);
            
            // Try to extract the encoding from the Content-Type header.
            Encoding? contentEncoding = null;
            if (result.ContentHeaders?.ContentType?.CharSet != null)
            {
                try { contentEncoding = Encoding.GetEncoding(result.ContentHeaders.ContentType.CharSet); }
                catch (ArgumentException) { }
            }
            
            return (result.Stream, contentEncoding);
        }

        protected override Task<string?> TryGetFileNameAsync(string originalUri) =>
            beeClient.TryGetFileNameAsync(originalUri);
    }
}
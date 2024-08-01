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
using Etherna.BeeNet.Models;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Etherna.UniversalFiles.Handlers
{
    /// <summary>
    /// Supports swarm files
    /// </summary>
    public class SwarmHandler(
        IBeeClient beeClient)
        : IHandler
    {
        public async Task<(bool Result, (byte[] ByteArray, Encoding? Encoding)? ContentCache)> ExistsAsync(
            string absoluteUri,
            UniversalUriKind absoluteUriKind)
        {
            if (absoluteUriKind != UniversalUriKind.OnlineAbsolute)
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

        public async Task<(long Result, (byte[] ByteArray, Encoding? Encoding)? ContentCache)> GetByteSizeAsync(
            string absoluteUri,
            UniversalUriKind absoluteUriKind)
        {
            var size = await beeClient.TryGetFileSizeAsync(absoluteUri).ConfigureAwait(false);
            if (size is null)
                throw new InvalidOperationException();
            return (size.Value, null);
        }

        public UniversalUriKind GetUriKind(string uri)
        {
            ArgumentNullException.ThrowIfNull(uri, nameof(uri));
            return SwarmHash.IsValidHash(uri.Split(SwarmAddress.Separator)[0])
                ? UniversalUriKind.OnlineAbsolute
                : UniversalUriKind.OnlineRelative;
        }

        public async Task<(byte[] ByteArray, Encoding? Encoding)> ReadToByteArrayAsync(
            string absoluteUri,
            UniversalUriKind absoluteUriKind)
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

        public async Task<(Stream Stream, Encoding? Encoding)> ReadToStreamAsync(
            string absoluteUri,
            UniversalUriKind absoluteUriKind)
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

        public Task<string?> TryGetFileNameAsync(string originalUri) =>
            beeClient.TryGetFileNameAsync(originalUri);

        public (string AbsoluteUri, UniversalUriKind UriKind)? TryGetParentDirectoryAsAbsoluteUri(
            string absoluteUri,
            UniversalUriKind absoluteUriKind) =>
            throw new InvalidOperationException("Swarm doesn't implement concept of directories");

        public (string AbsoluteUri, UniversalUriKind UriKind) UriToAbsoluteUri(
            string originalUri,
            string? baseDirectory,
            UniversalUriKind uriKind)
        {
            ArgumentNullException.ThrowIfNull(originalUri, nameof(originalUri));

            // Resolve absolute url.
            switch (uriKind)
            {
                case UniversalUriKind.OnlineAbsolute:
                    return (new SwarmAddress(originalUri).ToString(), UniversalUriKind.OnlineAbsolute);

                case UniversalUriKind.OnlineRelative:
                    if (baseDirectory is null)
                        throw new ArgumentNullException(nameof(baseDirectory),
                            "Base directory can't be null with relative original uri");

                    if ((GetUriKind(baseDirectory) & UniversalUriKind.Absolute) == 0)
                        throw new InvalidOperationException(
                            "If uri kind is relative, base directory must be absolute");
                    
                    var swarmUri = new SwarmUri(originalUri, UriKind.Relative);
                    var swarmAddress = swarmUri.ToSwarmAddress(baseDirectory);
            
                    return (swarmAddress.ToString(), UniversalUriKind.OnlineAbsolute);

                default: throw new InvalidOperationException("Can't find a valid uri kind");
            }
        }
    }
}
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

using Etherna.UniversalFiles.Handlers;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Etherna.UniversalFiles
{
    public class UniversalFile
    {
        // Fields.
        private readonly IHandler handler;
        private (byte[], Encoding?)? onlineResourceCache;

        // Constructor.
        internal UniversalFile(
            UniversalUri fileUri,
            IHandler handler)
        {
            ArgumentNullException.ThrowIfNull(fileUri, nameof(fileUri));
            
            FileUri = fileUri;
            this.handler = handler;
        }

        // Properties.
        public UniversalUri FileUri { get; }

        // Methods.
        public void ClearOnlineCache() => onlineResourceCache = null;

        public async Task<bool> ExistsAsync(
            bool useCacheIfOnline = false,
            UniversalUriKind allowedUriKinds = UniversalUriKind.All,
            string? baseDirectory = null)
        {
            // Use cache if enabled and available.
            if (useCacheIfOnline && onlineResourceCache != null)
                return true;

            // Get result from handler.
            var (absoluteUri, absoluteUriKind) = FileUri.ToAbsoluteUri(allowedUriKinds, baseDirectory);
            var (result, resultCache) = await handler.ExistsAsync(absoluteUri, absoluteUriKind).ConfigureAwait(false);
            
            // Update cache if required.
            if (absoluteUriKind == UniversalUriKind.OnlineAbsolute &&
                resultCache != null &&
                useCacheIfOnline)
                onlineResourceCache = resultCache;

            return result;
        }

        public async Task<long> GetByteSizeAsync(
            bool useCacheIfOnline = false,
            UniversalUriKind allowedUriKinds = UniversalUriKind.All,
            string? baseDirectory = null)
        {
            // Use cache if enabled and available.
            if (useCacheIfOnline && onlineResourceCache != null)
                return onlineResourceCache.Value.Item1.LongLength;

            // Get result from handler.
            var (absoluteUri, absoluteUriKind) = FileUri.ToAbsoluteUri(allowedUriKinds, baseDirectory);
            var (result, resultCache) = await handler.GetByteSizeAsync(absoluteUri, absoluteUriKind).ConfigureAwait(false);
            
            // Update cache if required.
            if (absoluteUriKind == UniversalUriKind.OnlineAbsolute &&
                resultCache != null &&
                useCacheIfOnline)
                onlineResourceCache = resultCache;

            return result;
        }

        public async Task<(byte[] ByteArray, Encoding? Encoding)> ReadToByteArrayAsync(
            bool useCacheIfOnline = false,
            UniversalUriKind allowedUriKinds = UniversalUriKind.All,
            string? baseDirectory = null)
        {
            // Use cache if enabled and available.
            if (useCacheIfOnline && onlineResourceCache != null)
                return onlineResourceCache.Value;

            // Get resource.
            var (absoluteUri, absoluteUriKind) = FileUri.ToAbsoluteUri(allowedUriKinds, baseDirectory);
            var result = await handler.ReadToByteArrayAsync(absoluteUri, absoluteUriKind).ConfigureAwait(false);
            
            // Update cache if required.
            if (absoluteUriKind == UniversalUriKind.OnlineAbsolute &&
                useCacheIfOnline)
                onlineResourceCache = result;

            return result;
        }

        public Task<(Stream Stream, Encoding? Encoding)> ReadToStreamAsync(
            UniversalUriKind allowedUriKinds = UniversalUriKind.All,
            string? baseDirectory = null)
        {
            // Get resource.
            var (absoluteUri, absoluteUriKind) = FileUri.ToAbsoluteUri(allowedUriKinds, baseDirectory);
            return handler.ReadToStreamAsync(absoluteUri, absoluteUriKind);
        }

        public async Task<string> ReadToStringAsync(
            bool useCacheIfOnline = false,
            UniversalUriKind allowedUriKinds = UniversalUriKind.All,
            string? baseDirectory = null)
        {
            var (content, encoding) = await ReadToByteArrayAsync(
                useCacheIfOnline,
                allowedUriKinds,
                baseDirectory).ConfigureAwait(false);
            encoding ??= Encoding.UTF8;
            return encoding.GetString(content);
        }

        public string? TryGetFileName() => handler.TryGetFileName(FileUri);
    }
}

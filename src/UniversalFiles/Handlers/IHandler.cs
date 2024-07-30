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

using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Etherna.UniversalFiles.Handlers
{
    public interface IHandler
    {
        Task<(bool Result, (byte[] ByteArray, Encoding? Encoding)? ContentCache)> ExistsAsync(
            string absoluteUri,
            UniversalUriKind absoluteUriKind);
        
        Task<(long Result, (byte[] ByteArray, Encoding? Encoding)? ContentCache)> GetByteSizeAsync(
            string absoluteUri,
            UniversalUriKind absoluteUriKind);

        /// <summary>
        /// Try to identify the uri kind, doesn't validate paths
        /// </summary>
        /// <param name="uri">The input uri</param>
        /// <returns>Identified uri kind</returns>
        UniversalUriKind GetUriKind(string uri);
        
        Task<(byte[] ByteArray, Encoding? Encoding)> ReadToByteArrayAsync(
            string absoluteUri,
            UniversalUriKind absoluteUriKind);
        
        Task<(Stream Stream, Encoding? Encoding)> ReadToStreamAsync(
            string absoluteUri,
            UniversalUriKind absoluteUriKind);

        Task<string?> TryGetFileNameAsync(
            string originalUri);

        (string AbsoluteUri, UniversalUriKind UriKind)? TryGetParentDirectoryAsAbsoluteUri(
            string absoluteUri,
            UniversalUriKind absoluteUriKind);

        (string AbsoluteUri, UniversalUriKind UriKind) UriToAbsoluteUri(
            string originalUri,
            string? baseDirectory,
            UniversalUriKind uriKind);
    }
}
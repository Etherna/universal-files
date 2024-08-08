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
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Etherna.UniversalFiles
{
    public class UFileProvider : IUFileProvider
    {
        // Fields.
        private readonly Dictionary<Type, Func<UUri, UFile>> uFileBuilders = new(); //<typeof(UUri), UFile builder>

        public UFileProvider(IHttpClientFactory httpClientFactory)
        {
            uFileBuilders[typeof(BasicUUri)] = uuri => new BasicUFile((BasicUUri)uuri, httpClientFactory);
        }
        
        // Methods.
        public BasicUFile BuildNewUFile(BasicUUri uuri) =>
            (BasicUFile)BuildNewUFile((UUri)uuri);
        
        public UFile BuildNewUFile(UUri uuri)
        {
            ArgumentNullException.ThrowIfNull(uuri, nameof(uuri));
            
            var builder = uFileBuilders[uuri.GetType()];
            return builder(uuri);
        }
        
        public void RegisterUUriType<TUUri>(Func<TUUri, UFile> builder) where TUUri : UUri =>
            uFileBuilders[typeof(TUUri)] = uuri => builder((TUUri)uuri);

        public async Task<BasicUFile> ToLocalUFileAsync(
            UFile inputUFile,
            UUriKind allowedUriKinds = UUriKind.All,
            string? baseDirectory = null,
            BasicUUri? outputUUri = null)
        {
            ArgumentNullException.ThrowIfNull(inputUFile, nameof(inputUFile));

            // If it's already a local file, skip file retrieve.
            if (inputUFile is BasicUFile basicUFile &&
                (basicUFile.FileUri.UriKind & allowedUriKinds & UUriKind.Local) != 0)
                return basicUFile;
            
            if (outputUUri != null && outputUUri.UriKind != UUriKind.LocalAbsolute)
                throw new InvalidOperationException("If provided, output uuri must be local and absolute");

            if (outputUUri is null)
            {
                var fileName = await inputUFile.TryGetFileNameAsync().ConfigureAwait(false);
                outputUUri = fileName is null ?
                    new BasicUUri(Path.GetTempFileName()) :
                    new BasicUUri(Path.Join(Path.GetTempPath(), fileName));
            }

            using var inputFileStream = (await inputUFile.ReadToStreamAsync(allowedUriKinds, baseDirectory).ConfigureAwait(false)).Stream;
            using var outputFileStream = new FileStream(outputUUri.ToAbsoluteUri().OriginalUri, FileMode.Create);
            await inputFileStream.CopyToAsync(outputFileStream).ConfigureAwait(false);

            return (BasicUFile)BuildNewUFile(outputUUri);
        }
    }
}
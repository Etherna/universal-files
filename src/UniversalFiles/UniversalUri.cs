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
using System.Threading.Tasks;

namespace Etherna.UniversalFiles
{
    public class UniversalUri
    {
        // Fields.
        private readonly string? defaultBaseDirectory;

        // Constructor.
        public UniversalUri(
            string uri,
            IHandler handler,
            UniversalUriKind allowedUriKinds = UniversalUriKind.All,
            string? defaultBaseDirectory = null)
        {
            ArgumentNullException.ThrowIfNull(handler, nameof(handler));
            if (string.IsNullOrWhiteSpace(uri))
                throw new ArgumentException("Uri cannot be null or white spaces", nameof(uri));

            this.defaultBaseDirectory = defaultBaseDirectory;
            Handler = handler;
            OriginalUri = uri;
            UriKind = handler.GetUriKind(uri) & allowedUriKinds;

            // Final check.
            if (UriKind == UniversalUriKind.None)
                throw new ArgumentException("Invalid uri with allowed uri types", nameof(uri));
        }

        // Properties.
        public IHandler Handler { get; }
        public string OriginalUri { get; }
        public UniversalUriKind UriKind { get; }

        // Methods.
        /// <summary>
        /// Get current uri as an absolute uri
        /// </summary>
        /// <param name="allowedUriKinds">Optional restrictions for original uri kind</param>
        /// <param name="baseDirectory">Optional base directory, required for online relative uri</param>
        /// <returns>Absolute uri and its kind</returns>
        public (string, UniversalUriKind) ToAbsoluteUri(
            UniversalUriKind allowedUriKinds = UniversalUriKind.All,
            string? baseDirectory = null)
        {
            // Define actual allowed uri kinds.
            var actualAllowedUriKinds = UriKind & allowedUriKinds;

            // Check with base directory.
            baseDirectory ??= defaultBaseDirectory;
            if ((actualAllowedUriKinds & UniversalUriKind.Relative) != 0 &&
                baseDirectory is not null)
            {
                var baseDirectoryUriKind = Handler.GetUriKind(baseDirectory) & UniversalUriKind.Absolute;

                actualAllowedUriKinds &= baseDirectoryUriKind switch
                {
                    UniversalUriKind.LocalAbsolute => UniversalUriKind.Local,
                    UniversalUriKind.OnlineAbsolute => UniversalUriKind.Online,
                    _ => throw new InvalidOperationException("Base directory can only be absolute"),
                };
            }

            // Checks.
            //local and online ambiguity
            if ((actualAllowedUriKinds & UniversalUriKind.Local) != 0 &&
                (actualAllowedUriKinds & UniversalUriKind.Online) != 0)
                throw new InvalidOperationException("Unable to distinguish between local and online uri. Try to restrict allowed uri kinds");

            //check if could be an online relative uri, and base directory is null
            if ((actualAllowedUriKinds & UniversalUriKind.OnlineRelative) != 0 &&
                baseDirectory is null)
                throw new InvalidOperationException("Can't resolve online relative uri. Specify a base directory");

            // Resolve with handler.
            /*
             * At this point we know what the exact uri kind is:
             * - if uri can be local absolute, then it can't be a relative or an online uri.
             * - if uri can be online absolute, then it can't be a relative or a local uri.
             * - if uri can be local relative, then it can't be an absolute or an online relative.
             *   This because if online relative was an option, it already verified presence of a base directory.
             *   And if base directory is present and valid, it already defined if uri is local or online.
             * - if uri can be online relative, then it can't be an absoulute or a local relative.
             *   It implies that a base directory must be present, and this implies same previous considerations.
             */
            return Handler.UriToAbsoluteUri(OriginalUri, baseDirectory, actualAllowedUriKinds);
        }

        public Task<string?> TryGetFileNameAsync() =>
            Handler.TryGetFileNameAsync(OriginalUri);

        /// <summary>
        /// Get parent directory as an absolute uri
        /// </summary>
        /// <param name="allowedUriKinds">Optional restrictions for original uri kind</param>
        /// <param name="baseDirectory">Optional base directory, required for online relative uri</param>
        /// <returns>Parent directory absolute uri and its kind</returns>
        public (string, UniversalUriKind)? TryGetParentDirectoryAsAbsoluteUri(
            UniversalUriKind allowedUriKinds = UniversalUriKind.All,
            string? baseDirectory = null)
        {
            var (absoluteUri, absoluteUriKind) = ToAbsoluteUri(allowedUriKinds, baseDirectory);
            return Handler.TryGetParentDirectoryAsAbsoluteUri(absoluteUri, absoluteUriKind);
        }
    }
}

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
using System.Diagnostics.CodeAnalysis;

namespace Etherna.UniversalFiles
{
    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings")]
    public abstract class UUri
    {
        // Constructor.
        protected UUri(
            string uri,
            UUriKind uriKind,
            string? defaultBaseDirectory = null)
        {
            if (string.IsNullOrWhiteSpace(uri))
                throw new ArgumentException("Uri cannot be null or white spaces", nameof(uri));

            DefaultBaseDirectory = defaultBaseDirectory;
            OriginalUri = uri;
            UriKind = uriKind;

            // Final check.
            if (UriKind == UUriKind.None)
                throw new ArgumentException("Invalid uri with allowed uri types", nameof(uri));
        }

        // Properties.
        public string? DefaultBaseDirectory { get; }
        
        [SuppressMessage("Design", "CA1056:URI-like properties should not be strings")]
        public string OriginalUri { get; }
        public UUriKind UriKind { get; }

        // Methods.
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj is not UUri uuriObj) return false;
            return GetType() == uuriObj.GetType() &&
                   DefaultBaseDirectory == uuriObj.DefaultBaseDirectory &&
                   OriginalUri == uuriObj.OriginalUri &&
                   UriKind == uuriObj.UriKind;
        }

        public override int GetHashCode() =>
            (DefaultBaseDirectory?.GetHashCode(StringComparison.InvariantCulture) ?? 0) ^
            OriginalUri.GetHashCode(StringComparison.InvariantCulture) ^
            UriKind.GetHashCode();

        /// <summary>
        /// Get current uri as an absolute uri
        /// </summary>
        /// <param name="allowedUriKinds">Optional restrictions for original uri kind</param>
        /// <param name="baseDirectory">Optional base directory, required for online relative uri</param>
        /// <returns>Absolute uri and uri kind</returns>
        public UUri ToAbsoluteUri(
            UUriKind allowedUriKinds = UUriKind.All,
            string? baseDirectory = null)
        {
            // Define actual allowed uri kinds.
            var actualAllowedUriKinds = UriKind & allowedUriKinds;

            // Check with base directory.
            baseDirectory ??= DefaultBaseDirectory;
            if ((actualAllowedUriKinds & UUriKind.Relative) != 0 &&
                baseDirectory is not null)
            {
                var baseDirectoryUriKind = GetUriKindHelper(baseDirectory) & UUriKind.Absolute;

                actualAllowedUriKinds &= baseDirectoryUriKind switch
                {
                    UUriKind.LocalAbsolute => UUriKind.Local,
                    UUriKind.OnlineAbsolute => UUriKind.Online,
                    _ => throw new InvalidOperationException("Base directory can only be absolute"),
                };
            }

            // Checks.
            //none allowed uri kinds.
            if (actualAllowedUriKinds == UUriKind.None)
                throw new InvalidOperationException("Can't identify a valid uri kind");
            
            //local and online ambiguity
            if ((actualAllowedUriKinds & UUriKind.Local) != 0 &&
                (actualAllowedUriKinds & UUriKind.Online) != 0)
                throw new InvalidOperationException("Unable to distinguish between local and online uri. Try to restrict allowed uri kinds");

            //check if it could be an online relative uri, and base directory is null
            if ((actualAllowedUriKinds & UUriKind.OnlineRelative) != 0 &&
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
            return UriToAbsoluteUri(OriginalUri, baseDirectory, actualAllowedUriKinds);
        }

        /// <summary>
        /// Get parent directory as an absolute uri
        /// </summary>
        /// <param name="allowedUriKinds">Optional restrictions for original uri kind</param>
        /// <param name="baseDirectory">Optional base directory, required for online relative uri</param>
        /// <returns>Parent directory absolute uri and its kind</returns>
        public UUri? TryGetParentDirectoryAsAbsoluteUri(
            UUriKind allowedUriKinds = UUriKind.All,
            string? baseDirectory = null)
        {
            var absoluteUri = ToAbsoluteUri(allowedUriKinds, baseDirectory);
            return TryGetParentDirectoryAsAbsoluteUri(absoluteUri);
        }
        
        // Protected methods.
        protected internal abstract UUriKind GetUriKindHelper(string uri);

        protected internal abstract UUri? TryGetParentDirectoryAsAbsoluteUri(UUri absoluteUri);
        
        protected internal abstract UUri UriToAbsoluteUri(
            string originalUri,
            string? baseDirectory,
            UUriKind uriKind);
    }
}

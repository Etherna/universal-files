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

using Etherna.BeeNet.Models;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Etherna.UniversalFiles
{
    public class SwarmUUri(
        SwarmUri uri,
        UUriKind allowedUriKinds = UUriKind.All,
        string? defaultBaseDirectory = null)
        : UUri(uri.ToString(), GetUriKind(uri.ToString()) & allowedUriKinds, defaultBaseDirectory)
    {
        // Public static methods.
        [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings")]
        public static UUriKind GetUriKind(string uri)
        {
            ArgumentNullException.ThrowIfNull(uri, nameof(uri));
            return SwarmHash.IsValidHash(uri.Split(SwarmAddress.Separator)[0])
                ? UUriKind.OnlineAbsolute
                : UUriKind.OnlineRelative;
        }
        
        // Protected methods.
        protected internal override UUriKind GetUriKindHelper(string uri) => GetUriKind(uri);

        protected internal override UUri? TryGetParentDirectoryAsAbsoluteUri(UUri absoluteUri) =>
            throw new InvalidOperationException("Swarm doesn't implement concept of directories");

        protected internal override UUri UriToAbsoluteUri(
            string originalUri,
            string? baseDirectory,
            UUriKind uriKind)
        {
            ArgumentNullException.ThrowIfNull(originalUri, nameof(originalUri));

            // Resolve absolute url.
            switch (uriKind)
            {
                case UUriKind.OnlineAbsolute:
                    return new SwarmUUri(new SwarmAddress(originalUri).ToString(), UUriKind.OnlineAbsolute);

                case UUriKind.OnlineRelative:
                    if (baseDirectory is null)
                        throw new ArgumentNullException(nameof(baseDirectory),
                            "Base directory can't be null with relative original uri");

                    if ((GetUriKind(baseDirectory) & UUriKind.Absolute) == 0)
                        throw new InvalidOperationException(
                            "If uri kind is relative, base directory must be absolute");
                    
                    var swarmUri = new SwarmUri(originalUri, System.UriKind.Relative);
                    var swarmAddress = swarmUri.ToSwarmAddress(baseDirectory);
            
                    return new SwarmUUri(swarmAddress.ToString(), UUriKind.OnlineAbsolute);

                default: throw new InvalidOperationException("Can't find a valid uri kind");
            }
        }
    }
}
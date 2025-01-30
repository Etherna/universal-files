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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Etherna.UniversalFiles
{
    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings")]
    public class BasicUUri(
        string uri,
        UUriKind allowedUriKinds = UUriKind.All,
        string? defaultBaseDirectory = null)
        : UUri(uri, GetUriKind(uri) & allowedUriKinds, defaultBaseDirectory)
    {
        // Public static methods.
        public static UUriKind GetUriKind(string uri)
        {
            ArgumentNullException.ThrowIfNull(uri, nameof(uri));

            var uriKind = UUriKind.None;

            if (uri.Length > 0)
            {
                //test online absolute
                if (Uri.TryCreate(uri, System.UriKind.Absolute, out var onlineAbsUriResult) &&
                    (onlineAbsUriResult.Scheme == Uri.UriSchemeHttp || onlineAbsUriResult.Scheme == Uri.UriSchemeHttps))
                    uriKind |= UUriKind.OnlineAbsolute;

                //test online relative
                if (Uri.TryCreate(uri, System.UriKind.Relative, out _))
                    uriKind |= UUriKind.OnlineRelative;

                //test local absolute and relative
                if ((uriKind & UUriKind.OnlineAbsolute) == 0)
                {
                    uriKind |= Path.IsPathRooted(uri) ?
                        UUriKind.LocalAbsolute :
                        UUriKind.LocalRelative;
                }
            }

            return uriKind;
        }

        // Protected methods.
        protected internal override UUriKind GetUriKindHelper(string uri) => GetUriKind(uri);

        protected internal override UUri? TryGetParentDirectoryAsAbsoluteUri(UUri absoluteUri)
        {
            ArgumentNullException.ThrowIfNull(absoluteUri, nameof(absoluteUri));
            
            switch (absoluteUri.UriKind)
            {
                case UUriKind.LocalAbsolute:
                    var dirName = Path.GetDirectoryName(absoluteUri.OriginalUri);
                    return dirName is null ? null :
                        new BasicUUri(dirName, UUriKind.LocalAbsolute);

                case UUriKind.OnlineAbsolute:
                    var segments = new Uri(absoluteUri.OriginalUri, System.UriKind.Absolute).Segments;
                    return segments.Length == 1 ? null : //if it's already root, return null
                        new BasicUUri(absoluteUri.OriginalUri[..^segments.Last().Length], UUriKind.OnlineAbsolute);

                default: throw new InvalidOperationException("Invalid absolute uri kind. It should be well defined and absolute");
            }
        }

        protected internal override UUri UriToAbsoluteUri(
            string originalUri,
            string? baseDirectory,
            UUriKind uriKind)
        {
            ArgumentNullException.ThrowIfNull(originalUri, nameof(originalUri));
            
            // Verify base directory is absolute.
            if ((uriKind & UUriKind.Relative) != 0 &&
                baseDirectory != null &&
                (GetUriKind(baseDirectory) & UUriKind.Absolute) == 0)
                throw new InvalidOperationException("If uri kind can be relative and base directory is present, it must be absolute");
            
            // Resolve absolute url.
            return uriKind switch
            {
                UUriKind.LocalAbsolute =>
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                    !Path.IsPathFullyQualified(originalUri) && //Ex: "/test"
                    baseDirectory is not null && Path.IsPathFullyQualified(baseDirectory) ?
                        new BasicUUri(Path.GetFullPath(originalUri, baseDirectory), UUriKind.LocalAbsolute) : //take unit from base directory
                        new BasicUUri(Path.GetFullPath(originalUri), UUriKind.LocalAbsolute),

                UUriKind.LocalRelative =>
                    new BasicUUri(
                        Path.GetFullPath(
                            originalUri,
                            baseDirectory is not null ?
                                Path.GetFullPath(baseDirectory) : //GetFullPath is required when on windows baseDirectory is a root path without unit name. Ex: "/test"
                                Directory.GetCurrentDirectory()),
                        UUriKind.LocalAbsolute),

                UUriKind.OnlineAbsolute => new BasicUUri(
                    new Uri(originalUri, System.UriKind.Absolute).ToString(),
                    UUriKind.OnlineAbsolute),

                UUriKind.OnlineRelative => new BasicUUri(
                    new Uri(
                        new Uri(baseDirectory!, System.UriKind.Absolute),
                        string.Join('/', originalUri.Split('/', '\\').Select(Uri.EscapeDataString))).ToString(),
                    UUriKind.OnlineAbsolute),

                _ => throw new InvalidOperationException("Can't find a valid uri kind")
            };
        }
    }
}
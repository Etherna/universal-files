//   Copyright 2023-present Etherna Sagl
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Etherna.UniversalFiles
{
    public class UniversalUri
    {
        // Fields.
        private readonly string? defaultBaseDirectory;

        // Constructor.
        public UniversalUri(
            string uri,
            UniversalUriKind allowedUriKinds = UniversalUriKind.All,
            string? defaultBaseDirectory = null)
        {
            if (string.IsNullOrWhiteSpace(uri))
                throw new ArgumentException("Uri cannot be null or white spaces", nameof(uri));

            this.defaultBaseDirectory = defaultBaseDirectory;
            OriginalUri = uri;
            UriKind = GetUriKind(uri) & allowedUriKinds;

            // Final check.
            if (UriKind == UniversalUriKind.None)
                throw new ArgumentException("Invalid uri with allowed uri types", nameof(uri));
        }

        // Properties.
        public string OriginalUri { get; }
        public UniversalUriKind UriKind { get; }

        // Methods.
        /// <summary>
        /// Get current uri as an absolute uri
        /// </summary>
        /// <param name="allowedUriKinds">Optional restrictions for original uri kind</param>
        /// <param name="baseDirectory">Optional base directory, required for online relative uri</param>
        /// <returns>Absolute uri and its kind</returns>
        public (string, UniversalUriKind) ToAbsoluteUri(UniversalUriKind allowedUriKinds = UniversalUriKind.All, string? baseDirectory = null)
        {
            // Define actual allowed uri kinds.
            var actualAllowedUriKinds = UriKind & allowedUriKinds;

            // Check with base directory.
            baseDirectory ??= defaultBaseDirectory;
            if ((actualAllowedUriKinds & UniversalUriKind.Relative) != 0 &&
                baseDirectory is not null)
            {
                var baseDirectoryUriKind = GetUriKind(baseDirectory) & UniversalUriKind.Absolute;

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

            // Resolve.
            /*
             * At this point we know what the exact kind is:
             * - if can be local absolute, then it can't be a relative or an online uri.
             * - if can be online absolute, then it can't be a relative or a local uri.
             * - if can be local relative, then it can't be an absolute or an online relative.
             *   This because if online relative was an option, it already verified presence of a base directory.
             *   And if base directory is present and valid, it already defined if uri is local or online.
             * - if can be online relative, then it can't be an absoulute or a local relative.
             *   It implies that a base directory must be present, and this implies same previus considerations.
             */
            return actualAllowedUriKinds switch
            {
                UniversalUriKind.LocalAbsolute =>
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                    !Path.IsPathFullyQualified(OriginalUri) && //Ex: "/test"
                    baseDirectory is not null && Path.IsPathFullyQualified(baseDirectory) ?
                        (Path.GetFullPath(OriginalUri, baseDirectory), UniversalUriKind.LocalAbsolute) : //take unit from base directory
                        (Path.GetFullPath(OriginalUri), UniversalUriKind.LocalAbsolute),

                UniversalUriKind.LocalRelative =>
                    (Path.GetFullPath(
                        OriginalUri,
                        baseDirectory is not null ?
                            Path.GetFullPath(baseDirectory) : //GetFullPath is required when on windows baseDirectory is a root path without unit name. Ex: "/test"
                            Directory.GetCurrentDirectory()),
                     UniversalUriKind.LocalAbsolute),

                UniversalUriKind.OnlineAbsolute => (new Uri(OriginalUri, System.UriKind.Absolute).ToString(), UniversalUriKind.OnlineAbsolute),

                UniversalUriKind.OnlineRelative => (new Uri(
                    new Uri(baseDirectory!, System.UriKind.Absolute),
                    string.Join('/', OriginalUri.Split('/', '\\').Select(Uri.EscapeDataString))).ToString(), UniversalUriKind.OnlineAbsolute),

                _ => throw new InvalidOperationException("Can't find a valid uri kind")
            };
        }

        /// <summary>
        /// Get parent directory as an absolute uri
        /// </summary>
        /// <param name="allowedUriKinds">Optional restrictions for original uri kind</param>
        /// <param name="baseDirectory">Optional base directory, required for online relative uri</param>
        /// <returns>Parent directory absolute uri and its kind</returns>
        public (string, UniversalUriKind)? TryGetParentDirectoryAsAbsoluteUri(UniversalUriKind allowedUriKinds = UniversalUriKind.All, string? baseDirectory = null)
        {
            var (absoluteUri, absoluteUriKind) = ToAbsoluteUri(allowedUriKinds, baseDirectory);

            switch (absoluteUriKind)
            {
                case UniversalUriKind.LocalAbsolute:
                    var dirName = Path.GetDirectoryName(absoluteUri);
                    return dirName is null ? null :
                        (dirName, UniversalUriKind.LocalAbsolute);

                case UniversalUriKind.OnlineAbsolute:
                    var segments = new Uri(absoluteUri, System.UriKind.Absolute).Segments;
                    return segments.Length == 1 ? null : //if it's already root, return null
                        (absoluteUri[..^segments.Last().Length], UniversalUriKind.OnlineAbsolute);

                default: throw new InvalidOperationException("Invalid absolute uri kind. It should be well defined and absolute");
            }
        }

        /// <summary>
        /// Try to identify the uri kind, doesn't validate local paths. Online absolute paths can't be local
        /// </summary>
        /// <param name="uri">The input uti</param>
        /// <returns>Identified uri kind</returns>
        public static UniversalUriKind GetUriKind(string uri)
        {
            if (uri is null)
                throw new ArgumentNullException(nameof(uri));

            var uriKind = UniversalUriKind.None;

            if (uri.Length > 0)
            {
                //test online absolute
                if (Uri.TryCreate(uri, System.UriKind.Absolute, out var onlineAbsUriResult) &&
                (onlineAbsUriResult.Scheme == Uri.UriSchemeHttp || onlineAbsUriResult.Scheme == Uri.UriSchemeHttps))
                    uriKind |= UniversalUriKind.OnlineAbsolute;

                //test online relative
                if (Uri.TryCreate(uri, System.UriKind.Relative, out var _))
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
    }
}

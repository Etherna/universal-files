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
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace Etherna.UniversalFiles
{
    public class BasicUUriTest
    {
        // // Classes.
        public class GetUriKindTestElement(
            string uri,
            UUriKind expectedUriKind)
        {
            public string Uri { get; } = uri;
            public UUriKind ExpectedUriKind { get; } = expectedUriKind;
        }

        public class TryGetParentDirectoryAsAbsoluteUriTestElement(
            BasicUUri absoluteUri,
            BasicUUri? expectedResult)
        {
            public BasicUUri AbsoluteUri { get; } = absoluteUri;
            public BasicUUri? ExpectedResult { get; } = expectedResult;
        }

        public class UriToAbsoluteUriTestElement(
            string originalUri,
            string? baseDirectory,
            UUriKind uriKind,
            BasicUUri? expectedResult = null,
            Type? expectedExceptionType = null)
        {
            public string OriginalUri { get; } = originalUri;
            public string? BaseDirectory { get; } = baseDirectory;
            public UUriKind UriKind { get; } = uriKind;
            public BasicUUri? ExpectedResult { get; } = expectedResult;
            public Type? ExpectedExceptionType { get; } = expectedExceptionType;
        }
        
        // Data.
        public static IEnumerable<object[]> GetUriKindTests
        {
            get
            {
                var tests = new List<GetUriKindTestElement>
                {
                    new("",
                        UUriKind.None),

                    new("test.txt",
                        UUriKind.Relative),

                    new("dir/test.txt",
                        UUriKind.Relative),

                    new("dir\\test.txt",
                        UUriKind.Relative),

                    new("/test.txt",
                        UUriKind.LocalAbsolute | UUriKind.OnlineRelative),

                    new("\\test.txt",
                        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                            ? //different behavior on windows host
                            UUriKind.LocalAbsolute | UUriKind.OnlineRelative
                            : UUriKind.Relative),

                    new("C:/dir/",
                        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                            ? //different behavior on windows host
                            UUriKind.LocalAbsolute | UUriKind.OnlineRelative
                            : UUriKind.Relative),

                    new("C:\\dir\\",
                        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                            ? //different behavior on windows host
                            UUriKind.LocalAbsolute | UUriKind.OnlineRelative
                            : UUriKind.Relative),

                    new("C:\\dir/file.txt",
                        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                            ? //different behavior on windows host
                            UUriKind.LocalAbsolute | UUriKind.OnlineRelative
                            : UUriKind.Relative),

                    new("/dir/",
                        UUriKind.LocalAbsolute | UUriKind.OnlineRelative),

                    new("\\dir\\",
                        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                            ? //different behavior on windows host
                            UUriKind.LocalAbsolute | UUriKind.OnlineRelative
                            : UUriKind.Relative),

                    new("\\dir/file.txt",
                        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                            ? //different behavior on windows host
                            UUriKind.LocalAbsolute | UUriKind.OnlineRelative
                            : UUriKind.Relative),

                    new("https://example.com",
                        UUriKind.OnlineAbsolute),

                    new("https://example.com/dir/",
                        UUriKind.OnlineAbsolute),

                    new("http://example.com/dir/file.txt",
                        UUriKind.OnlineAbsolute),
                };

                return tests.Select(t => new object[] { t });
            }
        }

        public static IEnumerable<object[]> TryGetParentDirectoryAsAbsoluteUriTests
        {
            get
            {
                var tests = new List<TryGetParentDirectoryAsAbsoluteUriTestElement>
                {
                    //local without parent
                    new(new BasicUUri("/", UUriKind.LocalAbsolute),
                        null),

                    //local with parent
                    new(new BasicUUri("/parent/test", UUriKind.LocalAbsolute),
                        new BasicUUri(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                                ? //different behavior on windows host
                                "\\parent"
                                : "/parent",
                            UUriKind.LocalAbsolute)),

                    //online without parent
                    new(new BasicUUri("https://example.com", UUriKind.OnlineAbsolute),
                        null),

                    new(new BasicUUri("https://example.com/", UUriKind.OnlineAbsolute),
                        null),

                    //online with parent
                    new(new BasicUUri("https://example.com/test", UUriKind.OnlineAbsolute),
                        new BasicUUri("https://example.com/", UUriKind.OnlineAbsolute)),

                    new(new BasicUUri("https://example.com/test/", UUriKind.OnlineAbsolute),
                        new BasicUUri("https://example.com/", UUriKind.OnlineAbsolute)),

                    new(new BasicUUri("https://example.com/parent/test", UUriKind.OnlineAbsolute),
                        new BasicUUri("https://example.com/parent/", UUriKind.OnlineAbsolute)),
                };

                return tests.Select(t => new object[] { t });
            }
        }

        public static IEnumerable<object[]> UriToAbsoluteUriTests
        {
            get
            {
                var tests = new List<UriToAbsoluteUriTestElement>
                {
                    //local absolute unix-like, no base directory
                    new("/test",
                        null,
                        UUriKind.LocalAbsolute,
                        new BasicUUri(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) //different behavior on windows host
                                ? Path.Combine(Path.GetPathRoot(Directory.GetCurrentDirectory())!, "test") //ex: "C:\\test"
                                : "/test",
                            UUriKind.LocalAbsolute)),

                    //local absolute windows-like, no base directory
                    new("D:\\test",
                        null,
                        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                            ? UUriKind.LocalAbsolute
                            : UUriKind.LocalRelative,
                        new BasicUUri(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) //different behavior on windows host
                                ? "D:\\test"
                                : Path.Combine(Directory.GetCurrentDirectory(), "D:\\test"),
                            UUriKind.LocalAbsolute)),

                    //local absolute unix-like, with local base directory unix-like
                    new("/test",
                        "/absolute/local",
                        UUriKind.LocalAbsolute,
                        new BasicUUri(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) //different behavior on windows host
                            ? Path.Combine(Path.GetPathRoot(Directory.GetCurrentDirectory())!, "test") //ex: "C:\\test"
                            :"/test", UUriKind.LocalAbsolute)),

                    //local absolute windows-like, with local base directory unix-like
                    new("D:\\test",
                        "/absolute/local",
                        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                            ? UUriKind.LocalAbsolute
                            : UUriKind.LocalRelative,
                        new BasicUUri(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) //different behavior on windows host
                                ? "D:\\test"
                                : "/absolute/local/D:\\test",
                            UUriKind.LocalAbsolute)),

                    //local absolute unix-like, with local base directory windows-like
                    new("/test",
                        "E:\\absolute\\local",
                        UUriKind.LocalAbsolute,
                        expectedResult: RuntimeInformation.IsOSPlatform(OSPlatform.Windows) //different behavior on windows host
                            ? new BasicUUri("E:\\test", UUriKind.LocalAbsolute)
                            : new BasicUUri("/test", UUriKind.LocalAbsolute)),

                    //local absolute windows-like, with local base directory windows-like
                    new("D:\\test",
                        "E:\\absolute\\local",
                        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                            ? UUriKind.LocalAbsolute
                            : UUriKind.LocalRelative,
                        expectedResult: RuntimeInformation.IsOSPlatform(OSPlatform.Windows) //different behavior on windows host
                            ? new BasicUUri("D:\\test", UUriKind.LocalAbsolute)
                            : null,
                        expectedExceptionType: RuntimeInformation.IsOSPlatform(OSPlatform.Windows) //different behavior on windows host
                            ? null
                            : typeof(InvalidOperationException)), //throws exception because base directory is not absolute

                    //rooted online relative, with online base directory
                    new("/test",
                        "https://example.com/dir/",
                        UUriKind.OnlineRelative,
                        new BasicUUri("https://example.com/test", UUriKind.OnlineAbsolute)),

                    //not rooted online relative, with online base directory
                    new("my/test",
                        "https://example.com/dir/",
                        UUriKind.OnlineRelative,
                        new BasicUUri("https://example.com/dir/my/test", UUriKind.OnlineAbsolute)),

                    //local relative (or online relative) with local restriction
                    new("test",
                        null,
                        UUriKind.LocalRelative,
                        new BasicUUri(Path.Combine(Directory.GetCurrentDirectory(), "test"), UUriKind.LocalAbsolute)),

                    //local relative (or online relative), with local base directory unix-like
                    new("test",
                        "/absolute/local",
                        UUriKind.LocalRelative,
                        new BasicUUri(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) //different behavior on windows host
                            ? Path.Combine(Path.GetPathRoot(Directory.GetCurrentDirectory())!, "absolute\\local\\test")
                            : "/absolute/local/test", UUriKind.LocalAbsolute)),

                    //local relative (or online relative), with local base directory windows-like
                    new("test",
                        "D:\\absolute\\local",
                        UUriKind.LocalRelative,
                        expectedResult: RuntimeInformation.IsOSPlatform(OSPlatform.Windows) //different behavior on windows host
                            ? new BasicUUri("D:\\absolute\\local\\test", UUriKind.LocalAbsolute)
                            : null,
                        expectedExceptionType: RuntimeInformation.IsOSPlatform(OSPlatform.Windows) //different behavior on windows host
                            ? null
                            : typeof(InvalidOperationException)), //throws exception because is ambiguous, and anyway base directory is not absolute

                    //local relative (or online relative) with online base directory
                    new("test",
                        "https://example.com/dir/",
                        UUriKind.OnlineRelative,
                        new BasicUUri("https://example.com/dir/test", UUriKind.OnlineAbsolute)),

                    //online absolute without restrictions
                    new("https://example.com/",
                        null,
                        UUriKind.OnlineAbsolute,
                        new BasicUUri("https://example.com/", UUriKind.OnlineAbsolute)),

                    //online absolute, with local base directory unix-like
                    new("https://example.com/",
                        "/absolute/local",
                        UUriKind.OnlineAbsolute,
                        new BasicUUri("https://example.com/", UUriKind.OnlineAbsolute)),

                    //online absolute, with local base directory windows-like
                    new("https://example.com/",
                        "C:\\absolute\\local",
                        UUriKind.OnlineAbsolute,
                        new BasicUUri("https://example.com/", UUriKind.OnlineAbsolute)),

                    //online absolute with online base directory
                    new("https://example.com/",
                        "https://other-site.com/",
                        UUriKind.OnlineAbsolute,
                        new BasicUUri("https://example.com/", UUriKind.OnlineAbsolute)),

                    //online absolute with relative base directory
                    new("https://example.com/",
                        "not/absolute",
                        UUriKind.All,
                        expectedExceptionType: typeof(InvalidOperationException)),
                };

                return tests.Select(t => new object[] { t });
            }
        }

        // Tests.
        [Theory]
        [InlineData("relativeUri", UUriKind.All, "relativeUri", UUriKind.Relative, null)]
        [InlineData("relativeUri", UUriKind.Local, "relativeUri", UUriKind.LocalRelative, null)]
        [InlineData("http://test.com", UUriKind.All, "http://test.com", UUriKind.OnlineAbsolute, null)]
        [InlineData("relativeUri", UUriKind.Absolute, null, null, typeof(ArgumentException))] //no valid uri kind found
        public void ConstructorEvaluateUriKind(
            string uri,
            UUriKind allowedUriKinds,
            string? expectedOriginalUri,
            UUriKind? expectedUriKind,
            Type? expectedExceptionType)
        {
            if (expectedExceptionType is null)
            {
                var universalUri = new BasicUUri(uri, allowedUriKinds);

                Assert.Equal(expectedOriginalUri, universalUri.OriginalUri);
                Assert.Equal(expectedUriKind, universalUri.UriKind);
            }
            else
            {
                Assert.Throws(expectedExceptionType,
                    () => new BasicUUri(uri, allowedUriKinds));
            }
        }

        [Theory, MemberData(nameof(GetUriKindTests))]
        public void GetUriKind(GetUriKindTestElement test)
        {
            var result = BasicUUri.GetUriKind(test.Uri);

            Assert.Equal(test.ExpectedUriKind, result);
        }

        [Theory, MemberData(nameof(TryGetParentDirectoryAsAbsoluteUriTests))]
        public void TryGetParentDirectoryAsAbsoluteUri(TryGetParentDirectoryAsAbsoluteUriTestElement test)
        {
            var basicUUri = new BasicUUri("test");
            var result = basicUUri.TryGetParentDirectoryAsAbsoluteUri(test.AbsoluteUri);

            Assert.Equal(test.ExpectedResult, result);
        }

        [Theory, MemberData(nameof(UriToAbsoluteUriTests))]
        public void UriToAbsoluteUri(UriToAbsoluteUriTestElement test)
        {
            var basicUUri = new BasicUUri("test");
            if (test.ExpectedExceptionType is null)
            {
                var result = basicUUri.UriToAbsoluteUri(
                    test.OriginalUri,
                    test.BaseDirectory,
                    test.UriKind);

                Assert.Equal(test.ExpectedResult, result);
            }
            else
            {
                Assert.Throws(test.ExpectedExceptionType,
                    () => basicUUri.UriToAbsoluteUri(
                        test.OriginalUri,
                        test.BaseDirectory,
                        test.UriKind));
            }
        }
    }
}

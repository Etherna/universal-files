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
    public class UniversalUriTest
    {
        // Classes.
        public class ToAbsoluteUriTestElement
        {
            public ToAbsoluteUriTestElement(
                UniversalUri universalUri,
                UniversalUriKind allowedUriKinds,
                string? baseDirectory,
                (string, UniversalUriKind)? expectedResult = null,
                Type? expectedExceptionType = null)
            {
                UniversalUri = universalUri;
                AllowedUriKinds = allowedUriKinds;
                BaseDirectory = baseDirectory;
                ExpectedResult = expectedResult;
                ExpectedExceptionType = expectedExceptionType;
            }

            public UniversalUri UniversalUri { get; }
            public UniversalUriKind AllowedUriKinds { get; }
            public string? BaseDirectory { get; }
            public (string, UniversalUriKind)? ExpectedResult { get; }
            public Type? ExpectedExceptionType { get; }
        }

        public class ToAbsoluteUriUsesBaseDirectoryTestElement
        {
            public ToAbsoluteUriUsesBaseDirectoryTestElement(
                UniversalUri universalUri,
                string? argBaseDirectory,
                (string, UniversalUriKind) expectedResult)
            {
                UniversalUri = universalUri;
                ArgBaseDirectory = argBaseDirectory;
                ExpectedResult = expectedResult;
            }

            public ToAbsoluteUriUsesBaseDirectoryTestElement(
                UniversalUri universalUri,
                string? argBaseDirectory,
                Type expectedExceptionType)
            {
                UniversalUri = universalUri;
                ArgBaseDirectory = argBaseDirectory;
                ExpectedExceptionType = expectedExceptionType;
            }

            public UniversalUri UniversalUri { get; }
            public string? ArgBaseDirectory { get; }
            public (string, UniversalUriKind)? ExpectedResult { get; }
            public Type? ExpectedExceptionType { get; }
        }

        public class ToAbsoluteUriUsesAllowedUriKindsTestElement
        {
            public ToAbsoluteUriUsesAllowedUriKindsTestElement(
                UniversalUri universalUri,
                UniversalUriKind argAllowedUriKinds,
                (string, UniversalUriKind) expectedResult)
            {
                UniversalUri = universalUri;
                ArgAllowedUriKinds = argAllowedUriKinds;
                ExpectedResult = expectedResult;
            }

            public ToAbsoluteUriUsesAllowedUriKindsTestElement(
                UniversalUri universalUri,
                UniversalUriKind argAllowedUriKinds,
                Type expectedExceptionType)
            {
                UniversalUri = universalUri;
                ArgAllowedUriKinds = argAllowedUriKinds;
                ExpectedExceptionType = expectedExceptionType;
            }

            public UniversalUri UniversalUri { get; }
            public UniversalUriKind ArgAllowedUriKinds { get; }
            public (string, UniversalUriKind)? ExpectedResult { get; }
            public Type? ExpectedExceptionType { get; }
        }

        public class TryGetParentDirectoryAsAbsoluteUriTestElement
        {
            public TryGetParentDirectoryAsAbsoluteUriTestElement(
                UniversalUri universalUri,
                (string, UniversalUriKind)? expectedResult)
            {
                UniversalUri = universalUri;
                ExpectedResult = expectedResult;
            }

            public TryGetParentDirectoryAsAbsoluteUriTestElement(
                UniversalUri universalUri,
                Type expectedExceptionType)
            {
                UniversalUri = universalUri;
                ExpectedExceptionType = expectedExceptionType;
            }

            public UniversalUri UniversalUri { get; }
            public (string, UniversalUriKind)? ExpectedResult { get; }
            public Type? ExpectedExceptionType { get; }
        }

        public class GetUriKindTestElement
        {
            public GetUriKindTestElement(
                string uri,
                UniversalUriKind expectedUriKind)
            {
                Uri = uri;
                ExpectedUriKind = expectedUriKind;
            }

            public string Uri { get; }
            public UniversalUriKind ExpectedUriKind { get; }
        }

        // Data.
        public static IEnumerable<object[]> ToAbsoluteUriTests
        {
            get
            {
                var tests = new List<ToAbsoluteUriTestElement>
                {
                    //local absolute (or online relative) without restrictions. Throws exception because is ambiguous
                    new ToAbsoluteUriTestElement(
                        new UniversalUri("/test"), //unix-like
                        UniversalUriKind.All,
                        null,
                        expectedExceptionType: typeof(InvalidOperationException)),

                    new ToAbsoluteUriTestElement(
                        new UniversalUri("D:\\test"), //windows-like
                        UniversalUriKind.All,
                        null,
                        expectedExceptionType: typeof(InvalidOperationException)),

                    //local absolute (or online relative) with local restriction
                    new ToAbsoluteUriTestElement(
                        new UniversalUri("/test"), //unix-like
                        UniversalUriKind.Local,
                        null,
                        (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            Path.Combine(Path.GetPathRoot(Directory.GetCurrentDirectory())!, "test") : //ex: "C:\\test"
                            "/test", UniversalUriKind.LocalAbsolute)),

                    new ToAbsoluteUriTestElement(
                        new UniversalUri("D:\\test"), //windows-like
                        UniversalUriKind.Local,
                        null,
                        (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            "D:\\test" :
                            Path.Combine(Directory.GetCurrentDirectory(), "D:\\test"), UniversalUriKind.LocalAbsolute)),

                    //local absolute (or online relative) with online restriction. Throws exception because base directory is null
                    new ToAbsoluteUriTestElement(
                        new UniversalUri("/test"), //unix-like
                        UniversalUriKind.Online,
                        null,
                        expectedExceptionType: typeof(InvalidOperationException)),

                    new ToAbsoluteUriTestElement(
                        new UniversalUri("D:\\test"), //windows-like
                        UniversalUriKind.Online,
                        null,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local absolute (or online relative) with local base directory
                    new ToAbsoluteUriTestElement(
                        new UniversalUri("/test"), //unix-like
                        UniversalUriKind.All,
                        "/absolute/local", //unix-like
                        (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            Path.Combine(Path.GetPathRoot(Directory.GetCurrentDirectory())!, "test") : //ex: "C:\\test"
                            "/test", UniversalUriKind.LocalAbsolute)),

                    new ToAbsoluteUriTestElement(
                        new UniversalUri("D:\\test"), //windows-like
                        UniversalUriKind.All,
                        "/absolute/local", //unix-like
                        (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            "D:\\test" :
                            "/absolute/local/D:\\test",
                         UniversalUriKind.LocalAbsolute)),

                    new ToAbsoluteUriTestElement(
                        new UniversalUri("/test"), //unix-like
                        UniversalUriKind.All, //with no restrictions
                        "E:\\absolute\\local", //windows-like
                        expectedResult: RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            ("E:\\test", UniversalUriKind.LocalAbsolute) :
                            null,
                        expectedExceptionType: RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            null :
                            typeof(InvalidOperationException)), //throws exception because is ambiguous and base directory is not absolute

                    new ToAbsoluteUriTestElement(
                        new UniversalUri("/test"), //unix-like
                        UniversalUriKind.Local, //with local restriction
                        "E:\\absolute\\local", //windows-like
                        (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            "E:\\test" :
                            "/test", UniversalUriKind.LocalAbsolute)),

                    new ToAbsoluteUriTestElement(
                        new UniversalUri("D:\\test"), //windows-like
                        UniversalUriKind.All,
                        "E:\\absolute\\local", //windows-like
                        expectedResult: RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            ("D:\\test", UniversalUriKind.LocalAbsolute) :
                            null,
                        expectedExceptionType: RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            null :
                            typeof(InvalidOperationException)), //throws exception because is ambiguous, and anyway base directory is not absolute

                    new ToAbsoluteUriTestElement(
                        new UniversalUri("D:\\test"), //unix-like
                        UniversalUriKind.Local, //with local restriction
                        "E:\\absolute\\local", //windows-like
                        expectedResult: RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            ("D:\\test", UniversalUriKind.LocalAbsolute) :
                            null,
                        expectedExceptionType: RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            null :
                            typeof(InvalidOperationException)), //throws exception because base directory is not absolute

                    //local absolute (or online relative) with online base directory
                    new ToAbsoluteUriTestElement(
                        new UniversalUri("/test"), //unix-like
                        UniversalUriKind.All,
                        "https://example.com/dir/",
                        ("https://example.com/test", UniversalUriKind.OnlineAbsolute)),

                    new ToAbsoluteUriTestElement(
                        new UniversalUri("D:\\test"), //windows-like
                        UniversalUriKind.All,
                        "https://example.com/dir/",
                        ("https://example.com/dir/D%3A/test", UniversalUriKind.OnlineAbsolute)),

                    //local absolute (or online relative) with relative base directory. Throws exception because is ambiguous and base directory is not absolute
                    new ToAbsoluteUriTestElement(
                        new UniversalUri("/test"), //unix-like
                        UniversalUriKind.All,
                        "not/absolute",
                        expectedExceptionType: typeof(InvalidOperationException)),

                    new ToAbsoluteUriTestElement(
                        new UniversalUri("D:\\test"), //windows-like
                        UniversalUriKind.All,
                        "not/absolute",
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local relative (or online relative) without restrictions. Throws exception because is ambiguous
                    new ToAbsoluteUriTestElement(
                        new UniversalUri("test"),
                        UniversalUriKind.All,
                        null,
                        expectedExceptionType: typeof(InvalidOperationException)),

                    //local relative (or online relative) with local restriction
                    new ToAbsoluteUriTestElement(
                        new UniversalUri("test"),
                        UniversalUriKind.Local,
                        null,
                        (Path.Combine(Directory.GetCurrentDirectory(), "test"), UniversalUriKind.LocalAbsolute)),

                    //local relative (or online relative) with online restriction. Throws exception because base directory is null
                    new ToAbsoluteUriTestElement(
                        new UniversalUri("test"),
                        UniversalUriKind.Online,
                        null,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local relative (or online relative) with local base directory
                    new ToAbsoluteUriTestElement(
                        new UniversalUri("test"),
                        UniversalUriKind.All,
                        "/absolute/local", //unix-like
                        (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            Path.Combine(Path.GetPathRoot(Directory.GetCurrentDirectory())!, "absolute\\local\\test") :
                            "/absolute/local/test", UniversalUriKind.LocalAbsolute)),

                    new ToAbsoluteUriTestElement(
                        new UniversalUri("test"),
                        UniversalUriKind.All,
                        "D:\\absolute\\local", //windows-like
                        expectedResult: RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            ("D:\\absolute\\local\\test", UniversalUriKind.LocalAbsolute) :
                            null,
                        expectedExceptionType: RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            null :
                            typeof(InvalidOperationException)), //throws exception because is ambiguous, and anyway base directory is not absolute

                    //local relative (or online relative) with online base directory
                    new ToAbsoluteUriTestElement(
                        new UniversalUri("test"),
                        UniversalUriKind.All,
                        "https://example.com/dir/",
                        ("https://example.com/dir/test", UniversalUriKind.OnlineAbsolute)),

                    //local relative (or online relative) with relative base directory. Throws exception because is ambiguous and base directory is not absolute
                    new ToAbsoluteUriTestElement(
                        new UniversalUri("test"),
                        UniversalUriKind.All,
                        "not/absolute",
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //online absolute without restrictions
                    new ToAbsoluteUriTestElement(
                        new UniversalUri("https://example.com/"),
                        UniversalUriKind.All,
                        null,
                        ("https://example.com/", UniversalUriKind.OnlineAbsolute)),

                    //online absolute with local restriction
                    new ToAbsoluteUriTestElement(
                        new UniversalUri("https://example.com/"),
                        UniversalUriKind.Local,
                        null,
                        expectedExceptionType: typeof(InvalidOperationException)),

                    //online absolute with online restriction
                    new ToAbsoluteUriTestElement(
                        new UniversalUri("https://example.com/"),
                        UniversalUriKind.Online,
                        null,
                        ("https://example.com/", UniversalUriKind.OnlineAbsolute)),
                    
                    //online absolute with local base directory
                    new ToAbsoluteUriTestElement(
                        new UniversalUri("https://example.com/"),
                        UniversalUriKind.All,
                        "/absolute/local", //unix-like
                        ("https://example.com/", UniversalUriKind.OnlineAbsolute)),

                    new ToAbsoluteUriTestElement(
                        new UniversalUri("https://example.com/"),
                        UniversalUriKind.All,
                        "C:\\absolute\\local", //windows-like
                        ("https://example.com/", UniversalUriKind.OnlineAbsolute)),

                    //online absolute with online base directory
                    new ToAbsoluteUriTestElement(
                        new UniversalUri("https://example.com/"),
                        UniversalUriKind.All,
                        "https://other-site.com/",
                        ("https://example.com/", UniversalUriKind.OnlineAbsolute)),

                    //online absolute with relative base directory
                    new ToAbsoluteUriTestElement(
                        new UniversalUri("https://example.com/"),
                        UniversalUriKind.All,
                        "not/absolute",
                        ("https://example.com/", UniversalUriKind.OnlineAbsolute)),
                };

                return tests.Select(t => new object[] { t });
            }
        }

        public static IEnumerable<object[]> ToAbsoluteUriUsesBaseDirectoryTests
        {
            get
            {
                var tests = new List<ToAbsoluteUriUsesBaseDirectoryTestElement>
                {
                    //null constructor, null method
                    new ToAbsoluteUriUsesBaseDirectoryTestElement(
                        new UniversalUri("test"),
                        null,
                        typeof(InvalidOperationException)),

                    //set constructor, null method
                    new ToAbsoluteUriUsesBaseDirectoryTestElement(
                        new UniversalUri("test",
                            defaultBaseDirectory: "https://constructor.com"),
                        null,
                        ("https://constructor.com/test", UniversalUriKind.OnlineAbsolute)),

                    //null constructor, set method
                    new ToAbsoluteUriUsesBaseDirectoryTestElement(
                        new UniversalUri("test"),
                        "https://method.com",
                        ("https://method.com/test", UniversalUriKind.OnlineAbsolute)),

                    //set constructor, set method
                    new ToAbsoluteUriUsesBaseDirectoryTestElement(
                        new UniversalUri("test",
                            defaultBaseDirectory: "https://constructor.com"),
                        "https://method.com",
                        ("https://method.com/test", UniversalUriKind.OnlineAbsolute))
                };

                return tests.Select(t => new object[] { t });
            }
        }

        public static IEnumerable<object[]> ToAbsoluteUriUsesAllowedUriKindsTests
        {
            get
            {
                var tests = new List<ToAbsoluteUriUsesAllowedUriKindsTestElement>
                {
                    //all constructor, all method.
                    new ToAbsoluteUriUsesAllowedUriKindsTestElement(
                        new UniversalUri("test"), //UriKind == Urikinds.Relative
                        UniversalUriKind.All,
                        typeof(InvalidOperationException)), //throws exception because is ambiguous

                    //limit constructor, all method
                    new ToAbsoluteUriUsesAllowedUriKindsTestElement(
                        new UniversalUri("test", UniversalUriKind.Local), //UriKind == Urikinds.LocalRelative
                        UniversalUriKind.All,
                        (Path.GetFullPath("test"), UniversalUriKind.LocalAbsolute)),

                    //all constructor, limit method
                    new ToAbsoluteUriUsesAllowedUriKindsTestElement(
                        new UniversalUri("test"), //UriKind == Urikinds.Relative
                        UniversalUriKind.Local,
                        (Path.GetFullPath("test"), UniversalUriKind.LocalAbsolute)),
                    
                    //limit constructor, limit method
                    new ToAbsoluteUriUsesAllowedUriKindsTestElement(
                        new UniversalUri("test", UniversalUriKind.Local), //UriKind == Urikinds.LocalRelative
                        UniversalUriKind.Online,
                        typeof(InvalidOperationException)), //throws exception because can't find a valid uri type
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
                    new TryGetParentDirectoryAsAbsoluteUriTestElement(
                        new UniversalUri("/", UniversalUriKind.Local),
                        ((string, UniversalUriKind)?)null),

                    //local with parent
                    new TryGetParentDirectoryAsAbsoluteUriTestElement(
                        new UniversalUri("parent/test", UniversalUriKind.Local),
                        (Path.GetFullPath("parent"), UniversalUriKind.LocalAbsolute)),

                    //online without parent
                    new TryGetParentDirectoryAsAbsoluteUriTestElement(
                        new UniversalUri("https://example.com"),
                        ((string, UniversalUriKind)?)null),

                    new TryGetParentDirectoryAsAbsoluteUriTestElement(
                        new UniversalUri("https://example.com/"),
                        ((string, UniversalUriKind)?)null),

                    //online with parent
                    new TryGetParentDirectoryAsAbsoluteUriTestElement(
                        new UniversalUri("https://example.com/test"),
                        ("https://example.com/", UniversalUriKind.OnlineAbsolute)),

                    new TryGetParentDirectoryAsAbsoluteUriTestElement(
                        new UniversalUri("https://example.com/test/"),
                        ("https://example.com/", UniversalUriKind.OnlineAbsolute)),

                    new TryGetParentDirectoryAsAbsoluteUriTestElement(
                        new UniversalUri("https://example.com/parent/test"),
                        ("https://example.com/parent/", UniversalUriKind.OnlineAbsolute)),

                    //exception because of invalid absolute uri
                    new TryGetParentDirectoryAsAbsoluteUriTestElement(
                        new UniversalUri("test"),
                        typeof(InvalidOperationException)), //throws exception because can't resolve absolute uri
                };

                return tests.Select(t => new object[] { t });
            }
        }

        public static IEnumerable<object[]> GetUriKindTests
        {
            get
            {
                var tests = new List<GetUriKindTestElement>
                {
                    new GetUriKindTestElement(
                        "",
                        UniversalUriKind.None),

                    new GetUriKindTestElement(
                        "test.txt",
                        UniversalUriKind.Relative),

                    new GetUriKindTestElement(
                        "dir/test.txt",
                        UniversalUriKind.Relative),

                    new GetUriKindTestElement(
                        "dir\\test.txt",
                        UniversalUriKind.Relative),

                    new GetUriKindTestElement(
                        "/test.txt",
                        UniversalUriKind.LocalAbsolute | UniversalUriKind.OnlineRelative),

                    new GetUriKindTestElement(
                        "\\test.txt",
                        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            UniversalUriKind.LocalAbsolute | UniversalUriKind.OnlineRelative :
                            UniversalUriKind.Relative),

                    new GetUriKindTestElement(
                        "C:/dir/",
                        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            UniversalUriKind.LocalAbsolute | UniversalUriKind.OnlineRelative :
                            UniversalUriKind.Relative),

                    new GetUriKindTestElement(
                        "C:\\dir\\",
                        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            UniversalUriKind.LocalAbsolute | UniversalUriKind.OnlineRelative :
                            UniversalUriKind.Relative),

                    new GetUriKindTestElement(
                        "C:\\dir/file.txt",
                        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            UniversalUriKind.LocalAbsolute | UniversalUriKind.OnlineRelative :
                            UniversalUriKind.Relative),

                    new GetUriKindTestElement(
                        "/dir/",
                        UniversalUriKind.LocalAbsolute | UniversalUriKind.OnlineRelative),

                    new GetUriKindTestElement(
                        "\\dir\\",
                        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            UniversalUriKind.LocalAbsolute | UniversalUriKind.OnlineRelative :
                            UniversalUriKind.Relative),

                    new GetUriKindTestElement(
                        "\\dir/file.txt",
                        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            UniversalUriKind.LocalAbsolute | UniversalUriKind.OnlineRelative :
                            UniversalUriKind.Relative),

                    new GetUriKindTestElement(
                        "https://example.com",
                        UniversalUriKind.OnlineAbsolute),

                    new GetUriKindTestElement(
                        "https://example.com/dir/",
                        UniversalUriKind.OnlineAbsolute),

                    new GetUriKindTestElement(
                        "http://example.com/dir/file.txt",
                        UniversalUriKind.OnlineAbsolute),
                };

                return tests.Select(t => new object[] { t });
            }
        }

        // Tests.
        [Theory]
        [InlineData("test", UniversalUriKind.All, "test", UniversalUriKind.Relative, null)]
        [InlineData("test", UniversalUriKind.Local, "test", UniversalUriKind.LocalRelative, null)]
        [InlineData("https://example.com/", UniversalUriKind.All, "https://example.com/", UniversalUriKind.OnlineAbsolute, null)]
        [InlineData("test", UniversalUriKind.Absolute, null, null, typeof(ArgumentException))] // throws because "test" is relative
        public void ConstructorEvaluateProperties(
            string uri,
            UniversalUriKind allowedUriKinds,
            string? expectedOriginalUri,
            UniversalUriKind? expectedUriKind,
            Type? expectedExceptionType)
        {
            if (expectedExceptionType is null)
            {
                var universalUri = new UniversalUri(uri, allowedUriKinds);

                Assert.Equal(expectedOriginalUri, universalUri.OriginalUri);
                Assert.Equal(expectedUriKind, universalUri.UriKind);
            }
            else
            {
                Assert.Throws(expectedExceptionType, () => new UniversalUri(uri, allowedUriKinds));
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("   ")]
        public void EmptyUriThrowsException(string? uri)
        {
            Assert.Throws<ArgumentException>(() => new UniversalUri(uri!));
        }

        [Theory]
        [InlineData("https://example.com/", UniversalUriKind.None)]
        [InlineData("https://example.com/", UniversalUriKind.Local)]
        public void TooRestrictiveUriKindThrowsException(string uri, UniversalUriKind allowedUriKinds)
        {
            Assert.Throws<ArgumentException>(() => new UniversalUri(uri, allowedUriKinds));
        }

        [Theory, MemberData(nameof(ToAbsoluteUriTests))]
        public void ToAbsoluteUri(ToAbsoluteUriTestElement test)
        {
            if (test.ExpectedExceptionType is null)
            {
                var result = test.UniversalUri.ToAbsoluteUri(
                    test.AllowedUriKinds,
                    test.BaseDirectory);

                Assert.Equal(test.ExpectedResult, result);
            }
            else
            {
                Assert.Throws(test.ExpectedExceptionType,
                    () => test.UniversalUri.ToAbsoluteUri(
                        test.AllowedUriKinds,
                        test.BaseDirectory));
            }
        }

        [Theory, MemberData(nameof(ToAbsoluteUriUsesBaseDirectoryTests))]
        public void ToAbsoluteUriUsesBaseDirectory(ToAbsoluteUriUsesBaseDirectoryTestElement test)
        {
            if (test.ExpectedResult is not null)
            {
                var result = test.UniversalUri.ToAbsoluteUri(
                    baseDirectory: test.ArgBaseDirectory);

                Assert.Equal(test.ExpectedResult, result);
            }
            else
            {
                Assert.Throws(test.ExpectedExceptionType!,
                    () => test.UniversalUri.ToAbsoluteUri(
                        baseDirectory: test.ArgBaseDirectory));
            }
        }

        [Theory, MemberData(nameof(ToAbsoluteUriUsesAllowedUriKindsTests))]
        public void ToAbsoluteUriUsesAllowedUriKinds(ToAbsoluteUriUsesAllowedUriKindsTestElement test)
        {
            if (test.ExpectedResult is not null)
            {
                var result = test.UniversalUri.ToAbsoluteUri(
                    test.ArgAllowedUriKinds);

                Assert.Equal(test.ExpectedResult, result);
            }
            else
            {
                Assert.Throws(test.ExpectedExceptionType!,
                    () => test.UniversalUri.ToAbsoluteUri(
                        test.ArgAllowedUriKinds));
            }
        }

        [Theory, MemberData(nameof(TryGetParentDirectoryAsAbsoluteUriTests))]
        public void TryGetParentDirectoryAsAbsoluteUri(TryGetParentDirectoryAsAbsoluteUriTestElement test)
        {
            if (test.ExpectedExceptionType is null)
            {
                var result = test.UniversalUri.TryGetParentDirectoryAsAbsoluteUri();

                Assert.Equal(test.ExpectedResult, result);
            }
            else
            {
                Assert.Throws(test.ExpectedExceptionType!,
                    () => test.UniversalUri.TryGetParentDirectoryAsAbsoluteUri());
            }
        }

        [Theory, MemberData(nameof(GetUriKindTests))]
        public void GetUriKind(GetUriKindTestElement test)
        {
            var result = UniversalUri.GetUriKind(test.Uri);

            Assert.Equal(test.ExpectedUriKind, result);
        }
    }
}

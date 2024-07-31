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

using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using Xunit;

namespace Etherna.UniversalFiles.Handlers
{
    public class BasicHandlerTest
    {
        // Fields.
        private static BasicHandler handler = new(new Mock<IHttpClientFactory>().Object);
        
        // Classes.
        public class UriToAbsoluteUriTestElement(
            string originalUri,
            string? baseDirectory,
            UniversalUriKind uriKind,
            (string, UniversalUriKind)? expectedResult = null,
            Type? expectedExceptionType = null)
        {
            public string OriginalUri { get; } = originalUri;
            public string? BaseDirectory { get; } = baseDirectory;
            public UniversalUriKind UriKind { get; } = uriKind;
            public (string, UniversalUriKind)? ExpectedResult { get; } = expectedResult;
            public Type? ExpectedExceptionType { get; } = expectedExceptionType;
        }

        // Data.
        public static IEnumerable<object[]> UriToAbsoluteUriTests
        {
            get
            {
                var tests = new List<UriToAbsoluteUriTestElement>
                {
                    //local absolute unix-like, no base directory
                    new("/test",
                        null,
                        UniversalUriKind.LocalAbsolute,
                        (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            Path.Combine(Path.GetPathRoot(Directory.GetCurrentDirectory())!, "test") : //ex: "C:\\test"
                            "/test",
                            UniversalUriKind.LocalAbsolute)),
                    
                    //local absolute windows-like, no base directory
                    new("D:\\test",
                        null,
                        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                            UniversalUriKind.LocalAbsolute :
                            UniversalUriKind.LocalRelative,
                        (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            "D:\\test" :
                            Path.Combine(Directory.GetCurrentDirectory(), "D:\\test"),
                            UniversalUriKind.LocalAbsolute)),
                    
                    //local absolute unix-like, with local base directory unix-like
                    new("/test",
                        "/absolute/local",
                        UniversalUriKind.LocalAbsolute,
                        (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            Path.Combine(Path.GetPathRoot(Directory.GetCurrentDirectory())!, "test") : //ex: "C:\\test"
                            "/test", UniversalUriKind.LocalAbsolute)),
                    
                    //local absolute windows-like, with local base directory unix-like
                    new("D:\\test",
                        "/absolute/local",
                        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                            UniversalUriKind.LocalAbsolute :
                            UniversalUriKind.LocalRelative,
                        (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            "D:\\test" :
                            "/absolute/local/D:\\test",
                            UniversalUriKind.LocalAbsolute)),
                    
                    //local absolute unix-like, with local base directory windows-like
                    new("/test",
                        "E:\\absolute\\local",
                        UniversalUriKind.LocalAbsolute,
                        expectedResult: RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            ("E:\\test", UniversalUriKind.LocalAbsolute) :
                            ("/test", UniversalUriKind.LocalAbsolute)),
                    
                    //local absolute windows-like, with local base directory windows-like
                    new("D:\\test",
                        "E:\\absolute\\local",
                        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                            UniversalUriKind.LocalAbsolute :
                            UniversalUriKind.LocalRelative,
                        expectedResult: RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            ("D:\\test", UniversalUriKind.LocalAbsolute) :
                            null,
                        expectedExceptionType: RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            null :
                            typeof(InvalidOperationException)), //throws exception because base directory is not absolute
                    
                    //rooted online relative, with online base directory
                    new("/test",
                        "https://example.com/dir/",
                        UniversalUriKind.OnlineRelative,
                        ("https://example.com/test", UniversalUriKind.OnlineAbsolute)),
                    
                    //not rooted online relative, with online base directory
                    new("my/test",
                        "https://example.com/dir/",
                        UniversalUriKind.OnlineRelative,
                        ("https://example.com/dir/my/test", UniversalUriKind.OnlineAbsolute)),
        
                    //local relative (or online relative) with local restriction
                    new("test",
                        null,
                        UniversalUriKind.LocalRelative,
                        (Path.Combine(Directory.GetCurrentDirectory(), "test"), UniversalUriKind.LocalAbsolute)),
                    
                    //local relative (or online relative), with local base directory unix-like
                    new("test",
                        "/absolute/local",
                        UniversalUriKind.LocalRelative,
                        (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            Path.Combine(Path.GetPathRoot(Directory.GetCurrentDirectory())!, "absolute\\local\\test") :
                            "/absolute/local/test", UniversalUriKind.LocalAbsolute)),
                    
                    //local relative (or online relative), with local base directory windows-like
                    new("test",
                        "D:\\absolute\\local",
                        UniversalUriKind.LocalRelative,
                        expectedResult: RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            ("D:\\absolute\\local\\test", UniversalUriKind.LocalAbsolute) :
                            null,
                        expectedExceptionType: RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? //different behavior on windows host
                            null :
                            typeof(InvalidOperationException)), //throws exception because is ambiguous, and anyway base directory is not absolute

                    //local relative (or online relative) with online base directory
                    new("test",
                        "https://example.com/dir/",
                        UniversalUriKind.OnlineRelative,
                        ("https://example.com/dir/test", UniversalUriKind.OnlineAbsolute)),
                    
                    //online absolute without restrictions
                    new("https://example.com/",
                        null,
                        UniversalUriKind.OnlineAbsolute,
                        ("https://example.com/", UniversalUriKind.OnlineAbsolute)),
                    
                    //online absolute, with local base directory unix-like
                    new("https://example.com/",
                        "/absolute/local",
                        UniversalUriKind.OnlineAbsolute,
                        ("https://example.com/", UniversalUriKind.OnlineAbsolute)),
                    
                    //online absolute, with local base directory windows-like
                    new("https://example.com/",
                        "C:\\absolute\\local",
                        UniversalUriKind.OnlineAbsolute,
                        ("https://example.com/", UniversalUriKind.OnlineAbsolute)),
        
                    //online absolute with online base directory
                    new("https://example.com/",
                        "https://other-site.com/",
                        UniversalUriKind.OnlineAbsolute,
                        ("https://example.com/", UniversalUriKind.OnlineAbsolute)),
        
                    //online absolute with relative base directory
                    new("https://example.com/",
                        "not/absolute",
                        UniversalUriKind.All,
                        expectedExceptionType: typeof(InvalidOperationException)),
                };
        
                return tests.Select(t => new object[] { t });
            }
        }
        
        // Tests.
        [Theory, MemberData(nameof(UriToAbsoluteUriTests))]
        public void UriToAbsoluteUri(UriToAbsoluteUriTestElement test)
        {
            if (test.ExpectedExceptionType is null)
            {
                var result = handler.UriToAbsoluteUri(
                    test.OriginalUri,
                    test.BaseDirectory,
                    test.UriKind);
        
                Assert.Equal(test.ExpectedResult, result);
            }
            else
            {
                Assert.Throws(test.ExpectedExceptionType,
                    () => handler.UriToAbsoluteUri(
                        test.OriginalUri,
                        test.BaseDirectory,
                        test.UriKind));
            }
        }
    }
}
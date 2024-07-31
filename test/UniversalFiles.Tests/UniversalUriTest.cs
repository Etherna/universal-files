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
using Moq;
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
        // Consts.
        private const string LocalAbsOrOnlineRelUri = "LocalAbsOrOnlineRelUri";
        private const string LocalAbsUri = "LocalAbsUri";
        private const string LocalRelOrOnlineRelUri = "LocalRelOrOnlineRelUri";
        private const string OnlineAbsUri = "OnlineAbsUri";
        private const string OnlineAbsUri2 = "OnlineAbsUri2";
        
        // Classes.
        public class ToAbsoluteUriTestElement
        {
            // Fields.
            private readonly Action<Mock<IHandler>>? assertHandlerMock;

            // Constructor.
            public ToAbsoluteUriTestElement(
                string uri,
                UniversalUriKind allowedUriKinds,
                string? baseDirectory,
                Action<Mock<IHandler>>? assertHandlerMock = null,
                Type? expectedExceptionType = null)
            {
                // Setup handler mock.
                HandlerMock = new Mock<IHandler>();
                HandlerMock.Setup(h => h.GetUriKind(LocalAbsOrOnlineRelUri))
                    .Returns(() => UniversalUriKind.LocalAbsolute | UniversalUriKind.OnlineRelative);
                HandlerMock.Setup(h => h.GetUriKind(LocalAbsUri))
                    .Returns(() => UniversalUriKind.LocalAbsolute);
                HandlerMock.Setup(h => h.GetUriKind(LocalRelOrOnlineRelUri))
                    .Returns(() => UniversalUriKind.LocalRelative | UniversalUriKind.OnlineRelative);
                HandlerMock.Setup(h => h.GetUriKind(OnlineAbsUri))
                    .Returns(() => UniversalUriKind.OnlineAbsolute);
                HandlerMock.Setup(h => h.GetUriKind(OnlineAbsUri2))
                    .Returns(() => UniversalUriKind.OnlineAbsolute);
                
                // Set properties.
                this.assertHandlerMock = assertHandlerMock;
                UniversalUri = new UniversalUri(uri, HandlerMock.Object);
                AllowedUriKinds = allowedUriKinds;
                BaseDirectory = baseDirectory;
                ExpectedExceptionType = expectedExceptionType;
            }
            
            // Properties.
            public UniversalUri UniversalUri { get; }
            public UniversalUriKind AllowedUriKinds { get; }
            public string? BaseDirectory { get; }
            public Mock<IHandler> HandlerMock { get; }
            public Type? ExpectedExceptionType { get; }
            
            // Methods.
            public void Assert() =>
                assertHandlerMock?.Invoke(HandlerMock);
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
                    //local absolute (or online relative), without restrictions. Throws exception because is ambiguous
                    new(LocalAbsOrOnlineRelUri,
                        UniversalUriKind.All,
                        null,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local absolute (or online relative), with local restriction
                    new(LocalAbsOrOnlineRelUri,
                        UniversalUriKind.Local,
                        null,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalAbsOrOnlineRelUri, null, UniversalUriKind.LocalAbsolute), Times.Once)),
                    
                    //local absolute (or online relative), with online restriction. Throws exception because base directory is null
                    new(LocalAbsOrOnlineRelUri,
                        UniversalUriKind.Online,
                        null,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local absolute (or online relative), with directory, without restrictions. Throws exception because is ambiguous
                    new(LocalAbsOrOnlineRelUri,
                        UniversalUriKind.All,
                        LocalAbsOrOnlineRelUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalAbsOrOnlineRelUri, LocalAbsOrOnlineRelUri, UniversalUriKind.LocalAbsolute), Times.Once)),
                    
                    //local absolute (or online relative), with directory, with local restriction
                    new(LocalAbsOrOnlineRelUri,
                        UniversalUriKind.Local,
                        LocalAbsOrOnlineRelUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalAbsOrOnlineRelUri, LocalAbsOrOnlineRelUri, UniversalUriKind.LocalAbsolute), Times.Once)),
                    
                    //local absolute (or online relative), with directory, with online restriction. Throws exception because can't find a valid uri kind
                    new(LocalAbsOrOnlineRelUri,
                        UniversalUriKind.Online,
                        LocalAbsOrOnlineRelUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local absolute (or online relative), with local base directory, without restrictions
                    new(LocalAbsOrOnlineRelUri,
                        UniversalUriKind.All,
                        LocalAbsUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalAbsOrOnlineRelUri, LocalAbsUri, UniversalUriKind.LocalAbsolute), Times.Once)),
                    
                    //local absolute (or online relative), with local base directory, with local restriction
                    new(LocalAbsOrOnlineRelUri,
                        UniversalUriKind.Local,
                        LocalAbsUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalAbsOrOnlineRelUri, LocalAbsUri, UniversalUriKind.LocalAbsolute), Times.Once)),
                    
                    //local absolute (or online relative), with local base directory, with online restriction
                    new(LocalAbsOrOnlineRelUri,
                        UniversalUriKind.Online,
                        LocalAbsUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local absolute (or online relative), with online base directory, without restrictions
                    new(LocalAbsOrOnlineRelUri,
                        UniversalUriKind.All,
                        OnlineAbsUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalAbsOrOnlineRelUri, OnlineAbsUri, UniversalUriKind.OnlineRelative), Times.Once)),
                    
                    //local absolute (or online relative), with online base directory, with local restriction
                    new(LocalAbsOrOnlineRelUri,
                        UniversalUriKind.Local,
                        OnlineAbsUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalAbsOrOnlineRelUri, OnlineAbsUri, UniversalUriKind.LocalAbsolute), Times.Once)),
                    
                    //local absolute (or online relative), with online base directory, with online restriction
                    new(LocalAbsOrOnlineRelUri,
                        UniversalUriKind.Online,
                        OnlineAbsUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalAbsOrOnlineRelUri, OnlineAbsUri, UniversalUriKind.OnlineRelative), Times.Once)),
                    
                    //local absolute (or online relative), with relative base directory. Throws exception because is ambiguous and base directory is not absolute
                    new(LocalAbsOrOnlineRelUri,
                        UniversalUriKind.All,
                        LocalRelOrOnlineRelUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local absolute (or online relative), with relative base directory. Throws exception because is ambiguous and base directory is not absolute
                    new(LocalAbsOrOnlineRelUri,
                        UniversalUriKind.Local,
                        LocalRelOrOnlineRelUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalAbsOrOnlineRelUri, LocalRelOrOnlineRelUri, UniversalUriKind.LocalAbsolute), Times.Once)),
                    
                    //local absolute (or online relative), with relative base directory. Throws exception because is ambiguous and base directory is not absolute
                    new(LocalAbsOrOnlineRelUri,
                        UniversalUriKind.Online,
                        LocalRelOrOnlineRelUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local relative (or online relative) without restrictions. Throws exception because is ambiguous
                    new(LocalRelOrOnlineRelUri,
                        UniversalUriKind.All,
                        null,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local relative (or online relative), with local restriction
                    new(LocalRelOrOnlineRelUri,
                        UniversalUriKind.Local,
                        null,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalRelOrOnlineRelUri, null, UniversalUriKind.LocalRelative), Times.Once)),
                    
                    //local relative (or online relative), with online restriction. Throws exception because base directory is null
                    new(LocalRelOrOnlineRelUri,
                        UniversalUriKind.Online,
                        null,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local relative (or online relative) with local base directory
                    new(LocalRelOrOnlineRelUri,
                        UniversalUriKind.All,
                        LocalAbsOrOnlineRelUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalRelOrOnlineRelUri, LocalAbsOrOnlineRelUri, UniversalUriKind.LocalRelative), Times.Once)),
                    
                    //local relative (or online relative), with local base directory, with local restriction
                    new(LocalRelOrOnlineRelUri,
                        UniversalUriKind.Local,
                        LocalAbsOrOnlineRelUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalRelOrOnlineRelUri, LocalAbsOrOnlineRelUri, UniversalUriKind.LocalRelative), Times.Once)),
                    
                    //local relative (or online relative), with local base directory, with online restriction. Throws exception because can't identify valid uri kind
                    new(LocalRelOrOnlineRelUri,
                        UniversalUriKind.Online,
                        LocalAbsOrOnlineRelUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local relative (or online relative), with online base directory
                    new(LocalRelOrOnlineRelUri,
                        UniversalUriKind.All,
                        OnlineAbsUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalRelOrOnlineRelUri, OnlineAbsUri, UniversalUriKind.OnlineRelative), Times.Once)),
                    
                    //local relative (or online relative), with online base directory, with local restriction. Throws exception because can't identify valid uri kind
                    new(LocalRelOrOnlineRelUri,
                        UniversalUriKind.Local,
                        OnlineAbsUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local relative (or online relative), with online base directory, with online restriction
                    new(LocalRelOrOnlineRelUri,
                        UniversalUriKind.Online,
                        OnlineAbsUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalRelOrOnlineRelUri, OnlineAbsUri, UniversalUriKind.OnlineRelative), Times.Once)),
                    
                    //local relative (or online relative), with local base directory
                    new(LocalRelOrOnlineRelUri,
                        UniversalUriKind.All,
                        LocalAbsUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalRelOrOnlineRelUri, LocalAbsUri, UniversalUriKind.LocalRelative), Times.Once)),
                    
                    //local relative (or online relative), with local base directory, with local restriction
                    new(LocalRelOrOnlineRelUri,
                        UniversalUriKind.Local,
                        LocalAbsUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalRelOrOnlineRelUri, LocalAbsUri, UniversalUriKind.LocalRelative), Times.Once)),
                    
                    //local relative (or online relative), with local base directory, with online restriction. Throws exception because can't identify valid uri kind
                    new(LocalRelOrOnlineRelUri,
                        UniversalUriKind.Online,
                        LocalAbsUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local relative (or online relative), with relative base directory. Throws exception because is ambiguous and base directory is not absolute
                    new(LocalRelOrOnlineRelUri,
                        UniversalUriKind.All,
                        LocalRelOrOnlineRelUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local relative (or online relative), with relative base directory, with local restriction. Throws exception because is ambiguous and base directory is not absolute
                    new(LocalRelOrOnlineRelUri,
                        UniversalUriKind.Local,
                        LocalRelOrOnlineRelUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local relative (or online relative), with relative base directory, with online restriction. Throws exception because is ambiguous and base directory is not absolute
                    new(LocalRelOrOnlineRelUri,
                        UniversalUriKind.Online,
                        LocalRelOrOnlineRelUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local absolute, with local base directory, without restrictions
                    new(LocalAbsUri,
                        UniversalUriKind.All,
                        LocalAbsUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalAbsUri, LocalAbsUri, UniversalUriKind.LocalAbsolute), Times.Once)),
                    
                    //local absolute, with local base directory, with local restriction
                    new(LocalAbsUri,
                        UniversalUriKind.Local,
                        LocalAbsUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalAbsUri, LocalAbsUri, UniversalUriKind.LocalAbsolute), Times.Once)),
                    
                    //local absolute, with local base directory, with online restriction
                    new(LocalAbsUri,
                        UniversalUriKind.Online,
                        LocalAbsUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //online absolute without restrictions
                    new(OnlineAbsUri,
                        UniversalUriKind.All,
                        null,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            OnlineAbsUri, null, UniversalUriKind.OnlineAbsolute), Times.Once)),
                    
                    //online absolute with local restriction. Throws exception because can't find valid uri kind
                    new(OnlineAbsUri,
                        UniversalUriKind.Local,
                        null,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //online absolute with online restriction
                    new(OnlineAbsUri,
                        UniversalUriKind.Online,
                        null,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            OnlineAbsUri, null, UniversalUriKind.OnlineAbsolute), Times.Once)),
                    
                    //online absolute with local base directory
                    new(OnlineAbsUri,
                        UniversalUriKind.All,
                        LocalAbsOrOnlineRelUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            OnlineAbsUri, LocalAbsOrOnlineRelUri, UniversalUriKind.OnlineAbsolute), Times.Once)),
                    
                    //online absolute with local base directory, with local restriction. Throws exception because can't find valid uri kind
                    new(OnlineAbsUri,
                        UniversalUriKind.Local,
                        LocalAbsOrOnlineRelUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //online absolute with local base directory, with online restriction
                    new(OnlineAbsUri,
                        UniversalUriKind.Online,
                        LocalAbsOrOnlineRelUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            OnlineAbsUri, LocalAbsOrOnlineRelUri, UniversalUriKind.OnlineAbsolute), Times.Once)),
                    
                    //online absolute with absolute online base directory, but different
                    new(OnlineAbsUri,
                        UniversalUriKind.All,
                        OnlineAbsUri2,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            OnlineAbsUri, OnlineAbsUri2, UniversalUriKind.OnlineAbsolute), Times.Once)),
                    
                    //online absolute, with relative base directory
                    new(OnlineAbsUri,
                        UniversalUriKind.All,
                        LocalRelOrOnlineRelUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            OnlineAbsUri, LocalRelOrOnlineRelUri, UniversalUriKind.OnlineAbsolute), Times.Once)),
                    
                    //online absolute, with relative base directory, with local restriction. Throws exception because can't find valid uri kind
                    new(OnlineAbsUri,
                        UniversalUriKind.Local,
                        LocalRelOrOnlineRelUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //online absolute, with relative base directory, with online restriction
                    new(OnlineAbsUri,
                        UniversalUriKind.Online,
                        LocalRelOrOnlineRelUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            OnlineAbsUri, LocalRelOrOnlineRelUri, UniversalUriKind.OnlineAbsolute), Times.Once)),
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

                test.Assert();
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

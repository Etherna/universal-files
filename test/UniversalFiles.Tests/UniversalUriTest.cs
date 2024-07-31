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
using System.Linq;
using Xunit;

namespace Etherna.UniversalFiles
{
    public class UniversalUriTest
    {
        // Consts.
        private const string LocalAbsOrOnlineRelUri = "LocalAbsOrOnlineRelUri";
        private const string LocalAbsUri = "LocalAbsUri";
        private const string OnlineAbsUri = "OnlineAbsUri";
        private const string OnlineAbsUri2 = "OnlineAbsUri2";
        private const string RelativeUri = "RelativeUri";
        
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
                HandlerMock.Setup(h => h.GetUriKind(OnlineAbsUri))
                    .Returns(() => UniversalUriKind.OnlineAbsolute);
                HandlerMock.Setup(h => h.GetUriKind(OnlineAbsUri2))
                    .Returns(() => UniversalUriKind.OnlineAbsolute);
                HandlerMock.Setup(h => h.GetUriKind(RelativeUri))
                    .Returns(() => UniversalUriKind.Relative);
                
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
            // Fields.
            private readonly Action<Mock<IHandler>>? assertHandlerMock;

            // Constructor.
            public ToAbsoluteUriUsesAllowedUriKindsTestElement(
                string uri,
                UniversalUriKind allowedUriKinds,
                UniversalUriKind argAllowedUriKinds,
                Action<Mock<IHandler>>? assertHandlerMock = null,
                Type? expectedExceptionType = null)
            {
                // Setup handler mock.
                HandlerMock = new Mock<IHandler>();
                HandlerMock.Setup(h => h.GetUriKind(RelativeUri))
                    .Returns(() => UniversalUriKind.Relative);
                
                // Set properties.
                this.assertHandlerMock = assertHandlerMock;
                UniversalUri = new UniversalUri(uri, HandlerMock.Object, allowedUriKinds);
                ArgAllowedUriKinds = argAllowedUriKinds;
                ExpectedExceptionType = expectedExceptionType;
            }

            public UniversalUri UniversalUri { get; }
            public UniversalUriKind ArgAllowedUriKinds { get; }
            public Type? ExpectedExceptionType { get; }
            public Mock<IHandler> HandlerMock { get; }
            
            // Methods.
            public void Assert() =>
                assertHandlerMock?.Invoke(HandlerMock);
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
                        RelativeUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local absolute (or online relative), with relative base directory. Throws exception because is ambiguous and base directory is not absolute
                    new(LocalAbsOrOnlineRelUri,
                        UniversalUriKind.Local,
                        RelativeUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalAbsOrOnlineRelUri, RelativeUri, UniversalUriKind.LocalAbsolute), Times.Once)),
                    
                    //local absolute (or online relative), with relative base directory. Throws exception because is ambiguous and base directory is not absolute
                    new(LocalAbsOrOnlineRelUri,
                        UniversalUriKind.Online,
                        RelativeUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local relative (or online relative) without restrictions. Throws exception because is ambiguous
                    new(RelativeUri,
                        UniversalUriKind.All,
                        null,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local relative (or online relative), with local restriction
                    new(RelativeUri,
                        UniversalUriKind.Local,
                        null,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            RelativeUri, null, UniversalUriKind.LocalRelative), Times.Once)),
                    
                    //local relative (or online relative), with online restriction. Throws exception because base directory is null
                    new(RelativeUri,
                        UniversalUriKind.Online,
                        null,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local relative (or online relative) with local base directory
                    new(RelativeUri,
                        UniversalUriKind.All,
                        LocalAbsOrOnlineRelUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            RelativeUri, LocalAbsOrOnlineRelUri, UniversalUriKind.LocalRelative), Times.Once)),
                    
                    //local relative (or online relative), with local base directory, with local restriction
                    new(RelativeUri,
                        UniversalUriKind.Local,
                        LocalAbsOrOnlineRelUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            RelativeUri, LocalAbsOrOnlineRelUri, UniversalUriKind.LocalRelative), Times.Once)),
                    
                    //local relative (or online relative), with local base directory, with online restriction. Throws exception because can't identify valid uri kind
                    new(RelativeUri,
                        UniversalUriKind.Online,
                        LocalAbsOrOnlineRelUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local relative (or online relative), with online base directory
                    new(RelativeUri,
                        UniversalUriKind.All,
                        OnlineAbsUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            RelativeUri, OnlineAbsUri, UniversalUriKind.OnlineRelative), Times.Once)),
                    
                    //local relative (or online relative), with online base directory, with local restriction. Throws exception because can't identify valid uri kind
                    new(RelativeUri,
                        UniversalUriKind.Local,
                        OnlineAbsUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local relative (or online relative), with online base directory, with online restriction
                    new(RelativeUri,
                        UniversalUriKind.Online,
                        OnlineAbsUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            RelativeUri, OnlineAbsUri, UniversalUriKind.OnlineRelative), Times.Once)),
                    
                    //local relative (or online relative), with local base directory
                    new(RelativeUri,
                        UniversalUriKind.All,
                        LocalAbsUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            RelativeUri, LocalAbsUri, UniversalUriKind.LocalRelative), Times.Once)),
                    
                    //local relative (or online relative), with local base directory, with local restriction
                    new(RelativeUri,
                        UniversalUriKind.Local,
                        LocalAbsUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            RelativeUri, LocalAbsUri, UniversalUriKind.LocalRelative), Times.Once)),
                    
                    //local relative (or online relative), with local base directory, with online restriction. Throws exception because can't identify valid uri kind
                    new(RelativeUri,
                        UniversalUriKind.Online,
                        LocalAbsUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local relative (or online relative), with relative base directory. Throws exception because is ambiguous and base directory is not absolute
                    new(RelativeUri,
                        UniversalUriKind.All,
                        RelativeUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local relative (or online relative), with relative base directory, with local restriction. Throws exception because is ambiguous and base directory is not absolute
                    new(RelativeUri,
                        UniversalUriKind.Local,
                        RelativeUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local relative (or online relative), with relative base directory, with online restriction. Throws exception because is ambiguous and base directory is not absolute
                    new(RelativeUri,
                        UniversalUriKind.Online,
                        RelativeUri,
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
                        RelativeUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            OnlineAbsUri, RelativeUri, UniversalUriKind.OnlineAbsolute), Times.Once)),
                    
                    //online absolute, with relative base directory, with local restriction. Throws exception because can't find valid uri kind
                    new(OnlineAbsUri,
                        UniversalUriKind.Local,
                        RelativeUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //online absolute, with relative base directory, with online restriction
                    new(OnlineAbsUri,
                        UniversalUriKind.Online,
                        RelativeUri,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            OnlineAbsUri, RelativeUri, UniversalUriKind.OnlineAbsolute), Times.Once)),
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
                    new(RelativeUri,
                        UniversalUriKind.Relative,
                        UniversalUriKind.All,
                        expectedExceptionType: typeof(InvalidOperationException)), //throws exception because is ambiguous
        
                    //limit constructor, all method
                    new(RelativeUri,
                        UniversalUriKind.Local,
                        UniversalUriKind.All,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            RelativeUri, null, UniversalUriKind.LocalRelative), Times.Once)),
        
                    //all constructor, limit method
                    new(RelativeUri,
                        UniversalUriKind.Relative,
                        UniversalUriKind.Local,
                        assertHandlerMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            RelativeUri, null, UniversalUriKind.LocalRelative), Times.Once)),
                    
                    //limit constructor, limit method
                    new(RelativeUri,
                        UniversalUriKind.Local,
                        UniversalUriKind.Online,
                        expectedExceptionType: typeof(InvalidOperationException)), //throws exception because can't find a valid uri kind
                };
        
                return tests.Select(t => new object[] { t });
            }
        }
        
        // Tests.
        [Theory]
        [InlineData(RelativeUri, UniversalUriKind.All, RelativeUri, UniversalUriKind.Relative, null)]
        [InlineData(RelativeUri, UniversalUriKind.Local, RelativeUri, UniversalUriKind.LocalRelative, null)]
        [InlineData(OnlineAbsUri, UniversalUriKind.All, OnlineAbsUri, UniversalUriKind.OnlineAbsolute, null)]
        [InlineData(RelativeUri, UniversalUriKind.Absolute, null, null, typeof(ArgumentException))] // throws because uri is relative
        public void ConstructorEvaluateProperties(
            string uri,
            UniversalUriKind allowedUriKinds,
            string? expectedOriginalUri,
            UniversalUriKind? expectedUriKind,
            Type? expectedExceptionType)
        {
            // Setup handler mock.
            var handlerMock = new Mock<IHandler>();
            handlerMock.Setup(h => h.GetUriKind(OnlineAbsUri))
                .Returns(() => UniversalUriKind.OnlineAbsolute);
            handlerMock.Setup(h => h.GetUriKind(RelativeUri))
                .Returns(() => UniversalUriKind.Relative);
            
            if (expectedExceptionType is null)
            {
                var universalUri = new UniversalUri(uri, handlerMock.Object, allowedUriKinds);
        
                Assert.Equal(expectedOriginalUri, universalUri.OriginalUri);
                Assert.Equal(expectedUriKind, universalUri.UriKind);
            }
            else
            {
                Assert.Throws(expectedExceptionType, () => new UniversalUri(uri, handlerMock.Object, allowedUriKinds));
            }
        }
        
        [Theory]
        [InlineData(null)]
        [InlineData("   ")]
        public void EmptyUriThrowsException(string? uri)
        {
            var handlerMock = new Mock<IHandler>();
            Assert.Throws<ArgumentException>(() => new UniversalUri(uri!, handlerMock.Object));
        }
        
        [Theory]
        [InlineData(OnlineAbsUri, UniversalUriKind.None)]
        [InlineData(OnlineAbsUri, UniversalUriKind.Local)]
        public void TooRestrictiveUriKindThrowsException(string uri, UniversalUriKind allowedUriKinds)
        {
            var handlerMock = new Mock<IHandler>();
            handlerMock.Setup(h => h.GetUriKind(OnlineAbsUri))
                .Returns(() => UniversalUriKind.OnlineAbsolute);
            Assert.Throws<ArgumentException>(() => new UniversalUri(uri, handlerMock.Object, allowedUriKinds));
        }
        
        [Theory, MemberData(nameof(ToAbsoluteUriTests))]
        public void ToAbsoluteUri(ToAbsoluteUriTestElement test)
        {
            if (test.ExpectedExceptionType is null)
            {
                test.UniversalUri.ToAbsoluteUri(
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
            if (test.ExpectedExceptionType is null)
            {
                test.UniversalUri.ToAbsoluteUri(
                    test.ArgAllowedUriKinds);

                test.Assert();
            }
            else
            {
                Assert.Throws(test.ExpectedExceptionType!,
                    () => test.UniversalUri.ToAbsoluteUri(
                        test.ArgAllowedUriKinds));
            }
        }
    }
}

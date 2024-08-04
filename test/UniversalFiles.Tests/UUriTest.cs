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
using System.Linq;
using System.Reflection;
using Xunit;

namespace Etherna.UniversalFiles
{
    public class UUriTest
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
            private readonly Action<Mock<UUri>>? assertUUriMock;
            private readonly Mock<UUri> uuriMock;
        
            // Constructor.
            public ToAbsoluteUriTestElement(
                string uri,
                UUriKind uuriKind,
                string? baseDirectory,
                Action<Mock<UUri>>? assertUUriMock = null,
                Type? expectedExceptionType = null)
            {
                // Set properties.
                this.assertUUriMock = assertUUriMock;
                BaseDirectory = baseDirectory;
                ExpectedExceptionType = expectedExceptionType;
                UuriKind = uuriKind;
                uuriMock = new Mock<UUri>(
                    uri,
                    UUriToUUriKind(uri),
                    null!);
                
                // Setup uuri mock.
                uuriMock.Setup(u => u.GetUriKindHelper(It.IsAny<string>()))
                    .Returns<string>(UUriToUUriKind);
            }

            // Properties.
            public UUriKind UuriKind { get; }
            public string? BaseDirectory { get; }
            public Type? ExpectedExceptionType { get; }
            public UUri UUri => uuriMock.Object;
            
            // Methods.
            public void Assert() =>
                assertUUriMock?.Invoke(uuriMock);
        }
        
        public class ToAbsoluteUriUsesAllowedUriKindsTestElement
        {
            // Fields.
            private readonly Action<Mock<UUri>>? assertUUriMock;
            private readonly Mock<UUri> uuriMock;
        
            // Constructor.
            public ToAbsoluteUriUsesAllowedUriKindsTestElement(
                string uri,
                UUriKind uriKind,
                UUriKind argAllowedUriKinds,
                Action<Mock<UUri>>? assertUUriMock = null,
                Type? expectedExceptionType = null)
            {
                // Set properties.
                this.assertUUriMock = assertUUriMock;
                ArgAllowedUriKinds = argAllowedUriKinds;
                ExpectedExceptionType = expectedExceptionType;
                uuriMock = new Mock<UUri>(
                    uri,
                    uriKind,
                    null!);
                
                // Setup uuri mock.
                uuriMock.Setup(u => u.GetUriKindHelper(It.IsAny<string>()))
                    .Returns<string>(UUriToUUriKind);
            }
        
            public UUriKind ArgAllowedUriKinds { get; }
            public Type? ExpectedExceptionType { get; }
            public UUri UUri => uuriMock.Object;
            
            // Methods.
            public void Assert() =>
                assertUUriMock?.Invoke(uuriMock);
        }
        
        public class ToAbsoluteUriUsesBaseDirectoryTestElement
        {
            // Fields.
            private readonly Action<Mock<UUri>>? assertUUriMock;
            private readonly Mock<UUri> uuriMock;
        
            // Constructor.
            public ToAbsoluteUriUsesBaseDirectoryTestElement(
                string uri,
                string? defaultBaseDirectory,
                string? argBaseDirectory,
                Action<Mock<UUri>>? assertUUriMock = null,
                Type? expectedExceptionType = null)
            {
                // Set properties.
                this.assertUUriMock = assertUUriMock;
                ArgBaseDirectory = argBaseDirectory;
                ExpectedExceptionType = expectedExceptionType;
                uuriMock = new Mock<UUri>(
                    uri,
                    UUriToUUriKind(uri),
                    defaultBaseDirectory!);
                
                // Setup uuri mock.
                uuriMock.Setup(u => u.GetUriKindHelper(It.IsAny<string>()))
                    .Returns<string>(UUriToUUriKind);
            }
        
            // Properties.
            public string? ArgBaseDirectory { get; }
            public Type? ExpectedExceptionType { get; }
            public UUri UUri => uuriMock.Object;
            
            // Methods.
            public void Assert() =>
                assertUUriMock?.Invoke(uuriMock);
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
                        UUriKind.All,
                        null,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local absolute (or online relative), with local restriction
                    new(LocalAbsOrOnlineRelUri,
                        UUriKind.Local,
                        null,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalAbsOrOnlineRelUri, null, UUriKind.LocalAbsolute), Times.Once)),
                    
                    //local absolute (or online relative), with online restriction. Throws exception because base directory is null
                    new(LocalAbsOrOnlineRelUri,
                        UUriKind.Online,
                        null,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local absolute (or online relative), with directory, without restrictions. Throws exception because is ambiguous
                    new(LocalAbsOrOnlineRelUri,
                        UUriKind.All,
                        LocalAbsOrOnlineRelUri,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalAbsOrOnlineRelUri, LocalAbsOrOnlineRelUri, UUriKind.LocalAbsolute), Times.Once)),
                    
                    //local absolute (or online relative), with directory, with local restriction
                    new(LocalAbsOrOnlineRelUri,
                        UUriKind.Local,
                        LocalAbsOrOnlineRelUri,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalAbsOrOnlineRelUri, LocalAbsOrOnlineRelUri, UUriKind.LocalAbsolute), Times.Once)),
                    
                    //local absolute (or online relative), with directory, with online restriction. Throws exception because can't find a valid uri kind
                    new(LocalAbsOrOnlineRelUri,
                        UUriKind.Online,
                        LocalAbsOrOnlineRelUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local absolute (or online relative), with local base directory, without restrictions
                    new(LocalAbsOrOnlineRelUri,
                        UUriKind.All,
                        LocalAbsUri,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalAbsOrOnlineRelUri, LocalAbsUri, UUriKind.LocalAbsolute), Times.Once)),
                    
                    //local absolute (or online relative), with local base directory, with local restriction
                    new(LocalAbsOrOnlineRelUri,
                        UUriKind.Local,
                        LocalAbsUri,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalAbsOrOnlineRelUri, LocalAbsUri, UUriKind.LocalAbsolute), Times.Once)),
                    
                    //local absolute (or online relative), with local base directory, with online restriction
                    new(LocalAbsOrOnlineRelUri,
                        UUriKind.Online,
                        LocalAbsUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local absolute (or online relative), with online base directory, without restrictions
                    new(LocalAbsOrOnlineRelUri,
                        UUriKind.All,
                        OnlineAbsUri,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalAbsOrOnlineRelUri, OnlineAbsUri, UUriKind.OnlineRelative), Times.Once)),
                    
                    //local absolute (or online relative), with online base directory, with local restriction
                    new(LocalAbsOrOnlineRelUri,
                        UUriKind.Local,
                        OnlineAbsUri,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalAbsOrOnlineRelUri, OnlineAbsUri, UUriKind.LocalAbsolute), Times.Once)),
                    
                    //local absolute (or online relative), with online base directory, with online restriction
                    new(LocalAbsOrOnlineRelUri,
                        UUriKind.Online,
                        OnlineAbsUri,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalAbsOrOnlineRelUri, OnlineAbsUri, UUriKind.OnlineRelative), Times.Once)),
                    
                    //local absolute (or online relative), with relative base directory. Throws exception because is ambiguous and base directory is not absolute
                    new(LocalAbsOrOnlineRelUri,
                        UUriKind.All,
                        RelativeUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local absolute (or online relative), with relative base directory. Throws exception because is ambiguous and base directory is not absolute
                    new(LocalAbsOrOnlineRelUri,
                        UUriKind.Local,
                        RelativeUri,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalAbsOrOnlineRelUri, RelativeUri, UUriKind.LocalAbsolute), Times.Once)),
                    
                    //local absolute (or online relative), with relative base directory. Throws exception because is ambiguous and base directory is not absolute
                    new(LocalAbsOrOnlineRelUri,
                        UUriKind.Online,
                        RelativeUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local relative (or online relative) without restrictions. Throws exception because is ambiguous
                    new(RelativeUri,
                        UUriKind.All,
                        null,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local relative (or online relative), with local restriction
                    new(RelativeUri,
                        UUriKind.Local,
                        null,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            RelativeUri, null, UUriKind.LocalRelative), Times.Once)),
                    
                    //local relative (or online relative), with online restriction. Throws exception because base directory is null
                    new(RelativeUri,
                        UUriKind.Online,
                        null,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local relative (or online relative) with local base directory
                    new(RelativeUri,
                        UUriKind.All,
                        LocalAbsOrOnlineRelUri,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            RelativeUri, LocalAbsOrOnlineRelUri, UUriKind.LocalRelative), Times.Once)),
                    
                    //local relative (or online relative), with local base directory, with local restriction
                    new(RelativeUri,
                        UUriKind.Local,
                        LocalAbsOrOnlineRelUri,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            RelativeUri, LocalAbsOrOnlineRelUri, UUriKind.LocalRelative), Times.Once)),
                    
                    //local relative (or online relative), with local base directory, with online restriction. Throws exception because can't identify valid uri kind
                    new(RelativeUri,
                        UUriKind.Online,
                        LocalAbsOrOnlineRelUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local relative (or online relative), with online base directory
                    new(RelativeUri,
                        UUriKind.All,
                        OnlineAbsUri,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            RelativeUri, OnlineAbsUri, UUriKind.OnlineRelative), Times.Once)),
                    
                    //local relative (or online relative), with online base directory, with local restriction. Throws exception because can't identify valid uri kind
                    new(RelativeUri,
                        UUriKind.Local,
                        OnlineAbsUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local relative (or online relative), with online base directory, with online restriction
                    new(RelativeUri,
                        UUriKind.Online,
                        OnlineAbsUri,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            RelativeUri, OnlineAbsUri, UUriKind.OnlineRelative), Times.Once)),
                    
                    //local relative (or online relative), with local base directory
                    new(RelativeUri,
                        UUriKind.All,
                        LocalAbsUri,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            RelativeUri, LocalAbsUri, UUriKind.LocalRelative), Times.Once)),
                    
                    //local relative (or online relative), with local base directory, with local restriction
                    new(RelativeUri,
                        UUriKind.Local,
                        LocalAbsUri,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            RelativeUri, LocalAbsUri, UUriKind.LocalRelative), Times.Once)),
                    
                    //local relative (or online relative), with local base directory, with online restriction. Throws exception because can't identify valid uri kind
                    new(RelativeUri,
                        UUriKind.Online,
                        LocalAbsUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local relative (or online relative), with relative base directory. Throws exception because is ambiguous and base directory is not absolute
                    new(RelativeUri,
                        UUriKind.All,
                        RelativeUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local relative (or online relative), with relative base directory, with local restriction. Throws exception because is ambiguous and base directory is not absolute
                    new(RelativeUri,
                        UUriKind.Local,
                        RelativeUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local relative (or online relative), with relative base directory, with online restriction. Throws exception because is ambiguous and base directory is not absolute
                    new(RelativeUri,
                        UUriKind.Online,
                        RelativeUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //local absolute, with local base directory, without restrictions
                    new(LocalAbsUri,
                        UUriKind.All,
                        LocalAbsUri,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalAbsUri, LocalAbsUri, UUriKind.LocalAbsolute), Times.Once)),
                    
                    //local absolute, with local base directory, with local restriction
                    new(LocalAbsUri,
                        UUriKind.Local,
                        LocalAbsUri,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            LocalAbsUri, LocalAbsUri, UUriKind.LocalAbsolute), Times.Once)),
                    
                    //local absolute, with local base directory, with online restriction
                    new(LocalAbsUri,
                        UUriKind.Online,
                        LocalAbsUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //online absolute without restrictions
                    new(OnlineAbsUri,
                        UUriKind.All,
                        null,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            OnlineAbsUri, null, UUriKind.OnlineAbsolute), Times.Once)),
                    
                    //online absolute with local restriction. Throws exception because can't find valid uri kind
                    new(OnlineAbsUri,
                        UUriKind.Local,
                        null,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //online absolute with online restriction
                    new(OnlineAbsUri,
                        UUriKind.Online,
                        null,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            OnlineAbsUri, null, UUriKind.OnlineAbsolute), Times.Once)),
                    
                    //online absolute with local base directory
                    new(OnlineAbsUri,
                        UUriKind.All,
                        LocalAbsOrOnlineRelUri,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            OnlineAbsUri, LocalAbsOrOnlineRelUri, UUriKind.OnlineAbsolute), Times.Once)),
                    
                    //online absolute with local base directory, with local restriction. Throws exception because can't find valid uri kind
                    new(OnlineAbsUri,
                        UUriKind.Local,
                        LocalAbsOrOnlineRelUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //online absolute with local base directory, with online restriction
                    new(OnlineAbsUri,
                        UUriKind.Online,
                        LocalAbsOrOnlineRelUri,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            OnlineAbsUri, LocalAbsOrOnlineRelUri, UUriKind.OnlineAbsolute), Times.Once)),
                    
                    //online absolute with absolute online base directory, but different
                    new(OnlineAbsUri,
                        UUriKind.All,
                        OnlineAbsUri2,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            OnlineAbsUri, OnlineAbsUri2, UUriKind.OnlineAbsolute), Times.Once)),
                    
                    //online absolute, with relative base directory
                    new(OnlineAbsUri,
                        UUriKind.All,
                        RelativeUri,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            OnlineAbsUri, RelativeUri, UUriKind.OnlineAbsolute), Times.Once)),
                    
                    //online absolute, with relative base directory, with local restriction. Throws exception because can't find valid uri kind
                    new(OnlineAbsUri,
                        UUriKind.Local,
                        RelativeUri,
                        expectedExceptionType: typeof(InvalidOperationException)),
                    
                    //online absolute, with relative base directory, with online restriction
                    new(OnlineAbsUri,
                        UUriKind.Online,
                        RelativeUri,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            OnlineAbsUri, RelativeUri, UUriKind.OnlineAbsolute), Times.Once)),
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
                        UUriKind.Relative,
                        UUriKind.All,
                        expectedExceptionType: typeof(InvalidOperationException)), //throws exception because is ambiguous
        
                    //limit constructor, all method
                    new(RelativeUri,
                        UUriKind.LocalRelative,
                        UUriKind.All,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            RelativeUri, null, UUriKind.LocalRelative), Times.Once)),
        
                    //all constructor, limit method
                    new(RelativeUri,
                        UUriKind.Relative,
                        UUriKind.Local,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            RelativeUri, null, UUriKind.LocalRelative), Times.Once)),
                    
                    //limit constructor, limit method
                    new(RelativeUri,
                        UUriKind.LocalRelative,
                        UUriKind.Online,
                        expectedExceptionType: typeof(InvalidOperationException)), //throws exception because can't find a valid uri kind
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
                    new(RelativeUri,
                        null,
                        null,
                        expectedExceptionType: typeof(InvalidOperationException)),
        
                    //set constructor, null method
                    new(RelativeUri,
                        OnlineAbsUri,
                        null,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            RelativeUri, OnlineAbsUri, UUriKind.OnlineRelative), Times.Once)),
        
                    //null constructor, set method
                    new(RelativeUri,
                        null,
                        OnlineAbsUri,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            RelativeUri, OnlineAbsUri, UUriKind.OnlineRelative), Times.Once)),
                    
                    //set constructor, set method
                    new(RelativeUri,
                        OnlineAbsUri,
                        OnlineAbsUri2,
                        assertUUriMock: mock => mock.Verify(h => h.UriToAbsoluteUri(
                            RelativeUri, OnlineAbsUri2, UUriKind.OnlineRelative), Times.Once)),
                };
        
                return tests.Select(t => new object[] { t });
            }
        }
        
        // Tests.
        [Theory]
        [InlineData(UUriKind.Absolute)]
        [InlineData(UUriKind.Relative)]
        [InlineData(UUriKind.LocalRelative)]
        [InlineData(UUriKind.OnlineAbsolute)]
        public void CanConstruct(UUriKind uriKind)
        {
            var uuriMock = new Mock<UUri>("testUri", uriKind, "defaultDir");
        
            Assert.Equal("testUri", uuriMock.Object.OriginalUri);
            Assert.Equal(uriKind, uuriMock.Object.UriKind);
            Assert.Equal("defaultDir", uuriMock.Object.DefaultBaseDirectory);
        }
        
        [Theory]
        [InlineData(null)]
        [InlineData("   ")]
        public void EmptyUriThrowsException(string? uri)
        {
            Assert.Throws<ArgumentException>(
                () =>
                {
                    var uuidMock = new Mock<UUri>(uri!, UUriKind.All, null!);
                    try { _ = uuidMock.Object; }
                    catch(TargetInvocationException e) //unwrap from Moq
                    {
                        throw e.InnerException!;
                    }
                });
        }
        
        [Theory, MemberData(nameof(ToAbsoluteUriTests))]
        public void ToAbsoluteUri(ToAbsoluteUriTestElement test)
        {
            if (test.ExpectedExceptionType is null)
            {
                test.UUri.ToAbsoluteUri(
                    test.UuriKind,
                    test.BaseDirectory);
        
                test.Assert();
            }
            else
            {
                Assert.Throws(test.ExpectedExceptionType,
                    () => test.UUri.ToAbsoluteUri(
                        test.UuriKind,
                        test.BaseDirectory));
            }
        }
        
        [Theory, MemberData(nameof(ToAbsoluteUriUsesAllowedUriKindsTests))]
        public void ToAbsoluteUriUsesAllowedUriKinds(ToAbsoluteUriUsesAllowedUriKindsTestElement test)
        {
            if (test.ExpectedExceptionType is null)
            {
                test.UUri.ToAbsoluteUri(
                    test.ArgAllowedUriKinds);
        
                test.Assert();
            }
            else
            {
                Assert.Throws(test.ExpectedExceptionType!,
                    () => test.UUri.ToAbsoluteUri(
                        test.ArgAllowedUriKinds));
            }
        }
        
        [Theory, MemberData(nameof(ToAbsoluteUriUsesBaseDirectoryTests))]
        public void ToAbsoluteUriUsesBaseDirectory(ToAbsoluteUriUsesBaseDirectoryTestElement test)
        {
            if (test.ExpectedExceptionType is null)
            {
                test.UUri.ToAbsoluteUri(
                    baseDirectory: test.ArgBaseDirectory);
        
                test.Assert();
            }
            else
            {
                Assert.Throws(test.ExpectedExceptionType!,
                    () => test.UUri.ToAbsoluteUri(
                        baseDirectory: test.ArgBaseDirectory));
            }
        }
        
        [Fact]
        public void UriKindNoneThrowsException()
        {
            Assert.Throws<ArgumentException>(
                () =>
                {
                    var uuidMock = new Mock<UUri>("myUri", UUriKind.None, null!);
                    try { _ = uuidMock.Object; }
                    catch(TargetInvocationException e) //unwrap from Moq
                    {
                        throw e.InnerException!;
                    }
                });
        }
        
        // Helpers.
        private static UUriKind UUriToUUriKind(string uri) =>
            uri switch
            {
                LocalAbsOrOnlineRelUri => UUriKind.LocalAbsolute | UUriKind.OnlineRelative,
                LocalAbsUri => UUriKind.LocalAbsolute,
                OnlineAbsUri => UUriKind.OnlineAbsolute,
                OnlineAbsUri2 => UUriKind.OnlineAbsolute,
                RelativeUri => UUriKind.Relative,
                _ => throw new ArgumentException(nameof(uri))
            };
    }
}

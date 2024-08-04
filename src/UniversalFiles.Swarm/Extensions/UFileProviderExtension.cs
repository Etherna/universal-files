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

using Etherna.BeeNet;
using System;

namespace Etherna.UniversalFiles.Extensions
{
    public static class UFileProviderExtension
    {
        public static SwarmUFile BuildNewUFile(
            this IUFileProvider fileProvider,
            SwarmUUri uuri)
        {
            ArgumentNullException.ThrowIfNull(fileProvider, nameof(fileProvider));
            return (SwarmUFile)fileProvider.BuildNewUFile(uuri);
        }

        public static UFileProvider UseSwarmUFiles(
            this UFileProvider fileProvider,
            IBeeClient beeClient)
        {
            ArgumentNullException.ThrowIfNull(fileProvider, nameof(fileProvider));
            
            fileProvider.RegisterUUriType<SwarmUUri>(uuri => new SwarmUFile(beeClient, uuri));
            return fileProvider;
        }
    }
}
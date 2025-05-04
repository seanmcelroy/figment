/*
Figment
Copyright (C) 2025  Sean McElroy

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Figment.Common.Data;

namespace Figment.Data.Memory;

public class MemoryStorageProvider() : IStorageProvider
{
    /// <inheritdoc/>
    public ISchemaStorageProvider? GetSchemaStorageProvider() => new MemorySchemaStorageProvider();

    /// <inheritdoc/>
    public IThingStorageProvider? GetThingStorageProvider() => new MemoryThingStorageProvider();

    /// <inheritdoc/>
    public Task<bool> InitializeAsync(CancellationToken cancellationToken) => Task.FromResult(true);
}
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

namespace Figment.Common.Data;

/// <summary>
/// An interface for storage providers that can create, read, update, and delete entities in a data store.
/// </summary>
public interface IStorageProvider
{
    /// <summary>
    /// Request the storage provider prepare for requests.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token for asynchronous methods.</param>
    /// <returns>A value indicating whether the storage provider successfully initialized and is ready to take requests.</returns>
    public Task<bool> InitializeAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns an <see cref="ISchemaStorageProvider"/> that can create, read, update, and delete <see cref="Schema"/> entities.
    /// </summary>
    /// <returns>An <see cref="ISchemaStorageProvider"/> that can create, read, update, and delete <see cref="Schema"/> entities.</returns>
    public ISchemaStorageProvider? GetSchemaStorageProvider();

    /// <summary>
    /// Returns an <see cref="IThingStorageProvider"/> that can create, read, update, and delete <see cref="Thing"/> entities.
    /// </summary>
    /// <returns>An <see cref="IThingStorageProvider"/> that can create, read, update, and delete <see cref="Thing"/> entities.</returns>
    public IThingStorageProvider? GetThingStorageProvider();
}
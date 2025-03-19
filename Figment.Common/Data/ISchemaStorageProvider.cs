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

public interface ISchemaStorageProvider
{
    /// <summary>
    /// Creates a new <see cref="Schema"/>.
    /// </summary>
    /// <param name="schemaName">The name of the <see cref="Schema"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True, if the operation was successful.  Otherwise, false.</returns>
    public Task<CreateSchemaResult> CreateAsync(
        string schemaName,
        CancellationToken cancellationToken);

    public Task<bool> DeleteAsync(string schemaGuid, CancellationToken cancellationToken);

    public Task<Reference> FindByNameAsync(string schemaName, CancellationToken cancellationToken);

    public IAsyncEnumerable<Reference> FindByPartialNameAsync(string schemaNamePart, CancellationToken cancellationToken);

    public IAsyncEnumerable<Reference> FindByPluralNameAsync(string plural, CancellationToken cancellationToken);

    public Task<bool> GuidExists(string schemaGuid, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all schemas
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to abort the enumerator</param>
    /// <returns>Each schema</returns>
    /// <remarks>This may be a very expensive operation</remarks>
    public IAsyncEnumerable<(Reference reference, string? name)> GetAll(CancellationToken cancellationToken);

    public Task<Schema?> LoadAsync(string schemaGuid, CancellationToken cancellationToken);

    public Task<bool> RebuildIndexes(CancellationToken cancellationToken);

    public Task<bool> SaveAsync(Schema schema, CancellationToken cancellationToken);
}
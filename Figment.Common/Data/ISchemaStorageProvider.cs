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

    /// <summary>
    /// Deletes a <see cref="Schema"/> from the underlying data store.
    /// </summary>
    /// <param name="schemaGuid">The unique identiifer of the <see cref="Schema"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True, if the operation was successful.  Otherwise, false.</returns>
    public Task<bool> DeleteAsync(string schemaGuid, CancellationToken cancellationToken);

    /// <summary>
    /// Obtains a reference to a schema if it is located by its <paramref name="schemaName"/>.
    /// </summary>
    /// <param name="schemaName">The name of the schema to locate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The schema, if it was found.  If the schema was not located, <see cref="Reference.EMPTY"/> is returned.</returns>
    public Task<Reference> FindByNameAsync(string schemaName, CancellationToken cancellationToken);

    public IAsyncEnumerable<Reference> FindByPartialNameAsync(string schemaNamePart, CancellationToken cancellationToken);

    public IAsyncEnumerable<Reference> FindByPluralNameAsync(string plural, CancellationToken cancellationToken);

    public Task<bool> GuidExists(string schemaGuid, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all schemas.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to abort the enumerator.</param>
    /// <returns>Each schema's reference and name.</returns>
    /// <remarks>This may be a very expensive operation, as it must load each <see cref="Schema"/> to obtain its name.</remarks>
    public IAsyncEnumerable<PossibleNameMatch> GetAll(CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to load a <see cref="Schema"/> from the underlying data store.
    /// </summary>
    /// <param name="schemaGuid">Unique identifier of the <see cref="Schema"/> to load.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value indicating whether or not the load attempt was successful.</returns>
    public Task<Schema?> LoadAsync(string schemaGuid, CancellationToken cancellationToken);

    public Task<bool> RebuildIndexes(CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to save a <see cref="Schema"/> to the underlying data store.
    /// </summary>
    /// <param name="schema">The <see cref="Schema"/> to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value indicating whether or not the save attempt was successful.</returns>
    public Task<bool> SaveAsync(Schema schema, CancellationToken cancellationToken);
}
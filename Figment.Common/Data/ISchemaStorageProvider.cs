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
/// An interface for storage providers that can create, read, update, and delete schemas in a data store.
/// </summary>
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

    /// <summary>
    /// Attempts to find <see cref="Schema"/> entities by a partial name match.
    /// </summary>
    /// <param name="schemaNamePart">The partial <see cref="Schema.Name"/> text that must match.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous enumerator for each <see cref="PossibleNameMatch"/> of each matching schema.</returns>
    public IAsyncEnumerable<PossibleNameMatch> FindByPartialNameAsync(string schemaNamePart, CancellationToken cancellationToken);

    /// <summary>
    /// Finds schemas based on the plural names assigned to them.
    /// </summary>
    /// <param name="plural">The plural keyword on the schemas to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous enumerator for each <see cref="Reference"/> of each matching schema.</returns>
    public IAsyncEnumerable<Reference> FindByPluralNameAsync(string plural, CancellationToken cancellationToken);

    /// <summary>
    /// Determines whether the schema with the given <paramref name="schemaGuid"/> exists in the underlying store.
    /// </summary>
    /// <param name="schemaGuid">Unique identifier of the <see cref="Schema"/> to check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value indicating whether the schema with the specified GUID exists.</returns>
    public Task<bool> GuidExists(string schemaGuid, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves an enumeration of metadata for every <see cref="Schema"/> in the underlying data store.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asychronous enumerator with metadata for each <see cref="Schema"/> in the underlying data store.</returns>
    public IAsyncEnumerable<PossibleNameMatch> GetAll(CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves an enumeration of every <see cref="Schema"/> in the underlying data store.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to abort the enumerator.</param>
    /// <returns>An asychronous enumerator for each <see cref="Schema"/> in the underlying data store.</returns>
    /// <remarks>This may be a very expensive operation.</remarks>
    public IAsyncEnumerable<Schema> LoadAll(CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to load a <see cref="Schema"/> from the underlying data store.
    /// </summary>
    /// <param name="schemaGuid">Unique identifier of the <see cref="Schema"/> to load.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value indicating whether or not the load attempt was successful.</returns>
    public Task<Schema?> LoadAsync(string schemaGuid, CancellationToken cancellationToken);

    /// <summary>
    /// Loads schema from a serialized Json string.
    /// </summary>
    /// <param name="content">Json string to deserialize into a <see cref="Schema"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserliazed schema, or null if there was a serialization error.</returns>
    public Task<Schema?> LoadJsonContentAsync(string content, CancellationToken cancellationToken);

    /// <summary>
    /// Rebuilds the indexes in the underlying data store.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value indicating whether or not the index rebuild was successful.</returns>
    public Task<bool> RebuildIndexes(CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to save a <see cref="Schema"/> to the underlying data store.
    /// </summary>
    /// <param name="schema">The <see cref="Schema"/> to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value indicating whether or not the save attempt was successful.</returns>
    public Task<(bool success, string? message)> SaveAsync(Schema schema, CancellationToken cancellationToken);
}
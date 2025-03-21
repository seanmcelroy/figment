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
/// An interface for storage providers that can create, read, update, and delete things in a data store.
/// </summary>
public interface IThingStorageProvider
{
    /// <summary>
    /// Associates a <see cref="Thing"/> with a <see cref="Schema"/>.
    /// </summary>
    /// <param name="thingGuid">Unique identifier of the <see cref="Thing"/> to associate with the <see cref="Schema"/> specified by <paramref name="schemaGuid"/>.</param>
    /// <param name="schemaGuid">Unique identiifer of the <see cref="Schema"/> to which the <see cref="Thing"/> specified by <paramref name="thingGuid"/> shall be associated.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task returning a <see cref="bool"/> indicating whether the operation was successful and an updated <see cref="Thing"/> loaded from the data store after the modification was made, if successful.</returns>
    public Task<(bool, Thing?)> AssociateWithSchemaAsync(string thingGuid, string schemaGuid, CancellationToken cancellationToken);

    public Task<Thing?> CreateAsync(string? schemaGuid, string thingName, CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to delete this <see cref="Thing"/> from its underlying data store.
    /// </summary>
    /// <param name="thingGuid">Unique identifier of the <see cref="Thing"/> to attempt to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value indicating whether or not the delete attempt was successful.</returns>
    public Task<bool> DeleteAsync(string thingGuid, CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to dissociate a <see cref="Schema"/> from a <see cref="Thing"/>.
    /// </summary>
    /// <param name="thingGuid">Unique identifier of the thing to dissociate.</param>
    /// <param name="schemaGuid">Unique identiifer of the schema that will be dissociated. </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value indicating whether the operation was successful, and an updated <see cref="Thing"/> if it was successful.</returns>
    public Task<(bool, Thing?)> DissociateFromSchemaAsync(string thingGuid, string schemaGuid, CancellationToken cancellationToken);

    public IAsyncEnumerable<Reference> GetBySchemaAsync(string schemaGuid, CancellationToken cancellationToken);

    public Task<Reference> FindByNameAsync(string exactName, CancellationToken cancellationToken, StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase);

    /// <summary>
    /// Attempts to find <see cref="Thing"/> entities by a partial name match.
    /// </summary>
    /// <param name="schemaGuid">The <see cref="Schema"/> to which matches must adhere.</param>
    /// <param name="thingNamePart">The partial <see cref="Thing.Name"/> text that must match.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous enumerator for each <see cref="PossibleNameMatch"/> of each matching thing.</returns>
    public IAsyncEnumerable<PossibleNameMatch> FindByPartialNameAsync(string schemaGuid, string thingNamePart, CancellationToken cancellationToken);

    public IAsyncEnumerable<(Reference reference, string? name)> GetAll(CancellationToken cancellationToken);

    public Task<bool> GuidExists(string thingGuid, CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to load a thing from the underlying data store.
    /// </summary>
    /// <param name="thingGuid">Unique identifier of the <see cref="Thing"/> to load.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value indicating whether or not the load attempt was successful.</returns>
    public Task<Thing?> LoadAsync(string thingGuid, CancellationToken cancellationToken);

    public Task<bool> RebuildIndexes(CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to save the thing to the underlying data store.
    /// </summary>
    /// <param name="thing">The <see cref="Thing"/> to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value indicating whether or not the save attempt was successful.</returns>
    public Task<bool> SaveAsync(Thing thing, CancellationToken cancellationToken);
}
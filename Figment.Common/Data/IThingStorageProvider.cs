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

using System.Collections;

namespace Figment.Common.Data;

/// <summary>
/// An interface for storage providers that can create, read, update, and delete things in a data store.
/// </summary>
public interface IThingStorageProvider
{
    /// <summary>
    /// Associates a <see cref="Thing"/> with a <see cref="Schema"/>.
    /// </summary>
    /// <param name="thingGuid">Unique identifier of the <see cref="Thing"/> to associate with the <paramref name="schema"/>.</param>
    /// <param name="schema">The <see cref="Schema"/> to which the <see cref="Thing"/> specified by <paramref name="thingGuid"/> shall be associated.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task returning a <see cref="bool"/> indicating whether the operation was successful and an updated <see cref="Thing"/> loaded from the data store after the modification was made, if successful.</returns>
    public Task<(bool success, Thing? thing)> AssociateWithSchemaAsync(string thingGuid, Schema schema, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new <see cref="Thing"/> in its underlying data store.
    /// </summary>
    /// <param name="schema">The identifier of a <see cref="Schema"/> to which this thing belongs.</param>
    /// <param name="thingName">The name of the thing.</param>
    /// <param name="properties">Initial properties to set on the thing.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The newly created thing if the operation was successful; otherwise, <c>null</c>.</returns>
    public Task<CreateThingResult> CreateAsync(Schema? schema, string thingName, Dictionary<string, object?> properties, CancellationToken cancellationToken);

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

    /// <summary>
    /// Retrieves an enumeration of references to every <see cref="Thing"/> that adheres to the specified <paramref name="schemaGuid"/>.
    /// </summary>
    /// <param name="schemaGuid">Unique identiifer of the schema for which references to things are retrieved.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An enumeration of references to every <see cref="Thing"/> that adheres to the specified <paramref name="schemaGuid"/>.</returns>
    public IAsyncEnumerable<Reference> GetBySchemaAsync(string schemaGuid, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves an enumeration of references to every <see cref="Thing"/> that adheres to the specified <paramref name="schemaGuid"/>.
    /// </summary>
    /// <param name="schemaGuid">Unique identifier of the schema selected objects must implement.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous enumeration of matching <see cref="Thing"/>s.</returns>
    /// <remarks>This is simlar to <see cref="GetBySchemaAsync"/>, except it returns loaded objects.</remarks>
    public IAsyncEnumerable<Thing> LoadAllForSchema(
        string schemaGuid,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves an enumeration of references to every <see cref="Thing"/> that adheres to the specified <paramref name="schemaGuid"/>
    /// and also has a specific property value.
    /// </summary>
    /// <param name="schemaGuid">Unique identifier of the schema selected objects must implement.</param>
    /// <param name="propName">Name of the property on the thing for which to check the property value.</param>
    /// <param name="propValue">Value of the property on the thing to compare.</param>
    /// <param name="comparer">The comparer to use when comparing the value in the potential thing's <paramref name="propName"/> value with the comparison value specified in <paramref name="propValue"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous enumeration of matching <see cref="Thing"/>s.</returns>
    public IAsyncEnumerable<Thing> FindBySchemaAndPropertyValue(
        string schemaGuid,
        string propName,
        object? propValue,
        IComparer comparer,
        CancellationToken cancellationToken);

    /// <summary>
    /// Obtains a reference to a thing if it is located by its <paramref name="exactName"/>.
    /// </summary>
    /// <param name="exactName">The name of the thing to locate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="comparisonType">The type of string comparison to use when finding things.</param>
    /// <returns>The thing, if it was found.  If the thing was not located, <see cref="Reference.EMPTY"/> is returned.</returns>
    public Task<Reference> FindByNameAsync(string exactName, CancellationToken cancellationToken, StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase);

    /// <summary>
    /// Attempts to find <see cref="Thing"/> entities by a partial name match in the data store.
    /// </summary>
    /// <param name="schemaGuid">The <see cref="Schema"/> to which matches must adhere.</param>
    /// <param name="thingNamePart">The partial <see cref="Thing.Name"/> text that must match.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous enumerator for each <see cref="PossibleNameMatch"/> of each matching thing.</returns>
    public IAsyncEnumerable<PossibleNameMatch> FindByPartialNameAsync(string schemaGuid, string thingNamePart, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves an enumeration of metadata for every <see cref="Thing"/> in the underlying data store.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asychronous enumerator with metadata for each <see cref="Thing"/> in the underlying data store.</returns>
    public IAsyncEnumerable<(Reference reference, string? name)> GetAll(CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves an enumeration of every <see cref="Thing"/> in the underlying data store.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to abort the enumerator.</param>
    /// <returns>An asychronous enumerator for each <see cref="Thing"/> in the underlying data store.</returns>
    /// <remarks>This may be a very expensive operation.</remarks>
    public IAsyncEnumerable<Thing> LoadAll(CancellationToken cancellationToken);

    /// <summary>
    /// Determines whether a <see cref="Thing"/> with the specified unique identifier exists in the data store.
    /// </summary>
    /// <param name="thingGuid">The unique identiifer of the <see cref="Thing"/> to find in the data store.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value indicating whether a <see cref="Thing"/> with the matching <paramref name="thingGuid"/> exists.</returns>
    public Task<bool> GuidExists(string thingGuid, CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to load a thing from the underlying data store.
    /// </summary>
    /// <param name="thingGuid">Unique identifier of the <see cref="Thing"/> to load.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value indicating whether or not the load attempt was successful.</returns>
    public Task<Thing?> LoadAsync(string thingGuid, CancellationToken cancellationToken);

    /// <summary>
    /// Rebuilds the indexes in the underlying data store.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value indicating whether or not the index rebuild was successful.</returns>
    public Task<bool> RebuildIndexes(CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to update one or more things by their guid with one or more property values.
    /// </summary>
    /// <param name="changes">A <see cref="Dictionary{Reference, List}"/> keyed by
    /// <see cref="Reference"/> for <see cref="Thing.Guid"/>s to update and a <see cref="List{Tuple}"/>
    /// which contains one or more <see cref="Tuple"/> containing a property name to update and the new value,
    /// similar to the signature and use of <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey, TValue}.TryUpdate(TKey, TValue, TValue)"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tuple containing a value whether the operations was successful at all, and individual results for each of the <see cref="Reference"/> keys passed in to <paramref name="changes"/>.</returns>
    public Task<(bool, Dictionary<Reference, (bool success, string message)> results)> TryBulkUpdate(Dictionary<Reference, Dictionary<string, object?>> changes, CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to save the thing to the underlying data store.
    /// </summary>
    /// <param name="thing">The <see cref="Thing"/> to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value indicating whether or not the save was successful.</returns>
    public Task<(bool success, string? message)> SaveAsync(Thing thing, CancellationToken cancellationToken);

    /// <summary>
    /// Updates every thing belonging to the specified schema and renumbers the increment field, if one exists
    /// for that schema.  This resets those fields to increment serially starting at one, closing any
    /// gaps created by deleted things.
    /// </summary>
    /// <param name="schemaGuid">The schema for which to update all items with updated increment values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value indicating whether or not the renumber was successful.</returns>
    public Task<bool> RenumberIncrementField(string schemaGuid, CancellationToken cancellationToken);
}
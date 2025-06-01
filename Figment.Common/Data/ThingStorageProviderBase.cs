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
/// An optional base provider that provides some common methods for <see cref="IThingStorageProvider"/> implementations.
/// </summary>
public abstract class ThingStorageProviderBase : IThingStorageProvider
{
    /// <inheritdoc/>
    public abstract Task<(bool, Thing?)> AssociateWithSchemaAsync(string thingGuid, Schema schema, CancellationToken cancellationToken);

    /// <inheritdoc/>
    public abstract Task<Thing?> CreateAsync(Schema? schema, string thingName, CancellationToken cancellationToken);

    /// <inheritdoc/>
    public abstract Task<bool> DeleteAsync(string thingGuid, CancellationToken cancellationToken);

    /// <inheritdoc/>
    public abstract Task<(bool, Thing?)> DissociateFromSchemaAsync(string thingGuid, string schemaGuid, CancellationToken cancellationToken);

    /// <inheritdoc/>
    public abstract Task<Reference> FindByNameAsync(string exactName, CancellationToken cancellationToken, StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase);

    /// <inheritdoc/>
    public abstract IAsyncEnumerable<PossibleNameMatch> FindByPartialNameAsync(string schemaGuid, string thingNamePart, CancellationToken cancellationToken);

    /// <inheritdoc/>
    public abstract IAsyncEnumerable<(Reference reference, string? name)> GetAll(CancellationToken cancellationToken);

    /// <inheritdoc/>
    public abstract IAsyncEnumerable<Reference> GetBySchemaAsync(string schemaGuid, CancellationToken cancellationToken);

    /// <inheritdoc/>
    public abstract IAsyncEnumerable<Thing> FindBySchemaAndPropertyValue(
       string schemaGuid,
       string propName,
       object? propValue,
       IComparer comparer,
       CancellationToken cancellationToken);

    /// <inheritdoc/>
    public abstract Task<bool> GuidExists(string thingGuid, CancellationToken cancellationToken);

    /// <inheritdoc/>
    public abstract IAsyncEnumerable<Thing> LoadAll(CancellationToken cancellationToken);

    /// <inheritdoc/>
    public abstract Task<Thing?> LoadAsync(string thingGuid, CancellationToken cancellationToken);

    /// <inheritdoc/>
    public abstract Task<bool> RebuildIndexes(CancellationToken cancellationToken);

    /// <inheritdoc/>
    public abstract Task<(bool success, string? message)> SaveAsync(Thing thing, CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to update one or more things by their guid with one or more property values.
    /// </summary>
    /// <param name="changes">A <see cref="Dictionary{Reference, List}"/> keyed by
    /// <see cref="Reference"/> for <see cref="Thing.Guid"/>s to update and a <see cref="List{Tuple}"/>
    /// which contains one or more <see cref="Tuple"/> containing a property name to update and the new value,
    /// similar to the signature and use of <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey, TValue}.TryUpdate(TKey, TValue, TValue)"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tuple containing a value whether the operations was successful at all, and individual results for each of the <see cref="Reference"/> keys passed in to <paramref name="changes"/>.</returns>
    public async Task<(bool, Dictionary<Reference, (bool success, string message)> results)> TryBulkUpdate(Dictionary<Reference, List<(string propertyName, object newValue)>> changes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(changes);

        var results = new Dictionary<Reference, (bool success, string message)>();
        if (changes.Count == 0)
        {
            return (true, results);
        }

        foreach (var change in changes)
        {
            var thing = await LoadAsync(change.Key.Guid, cancellationToken);
            if (thing == null)
            {
                results.TryAdd(change.Key, (false, "Unable to load from underlying data store."));
                continue;
            }

            var anyError = false;
            foreach (var (propertyName, newValue) in change.Value)
            {
                var tsr = await thing.Set(propertyName, newValue, cancellationToken);
                if (!tsr.Success)
                {
                    results.TryAdd(change.Key, (false, tsr.Message ?? $"Unable to set property '{propertyName}' to '{newValue}'"));
                    anyError = true;
                    break;
                }
            }

            if (anyError)
            {
                continue;
            }

            var (saveSuccess, saveMessage) = await thing.SaveAsync(cancellationToken);
            if (!saveSuccess)
            {
                results.TryAdd(change.Key, (false, saveMessage ?? "Unable to save thing to underlying data store."));
                continue;
            }

            results.TryAdd(change.Key, (true, "Update successful"));
        }

        return (true, results);
    }
}
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
using System.Runtime.CompilerServices;
using Figment.Common.Errors;

namespace Figment.Common.Data;

/// <summary>
/// An optional base provider that provides some common methods for <see cref="IThingStorageProvider"/> implementations.
/// </summary>
public abstract class ThingStorageProviderBase : IThingStorageProvider
{
    /// <inheritdoc/>
    public abstract Task<(bool success, Thing? thing)> AssociateWithSchemaAsync(string thingGuid, Schema schema, CancellationToken cancellationToken);

    /// <inheritdoc/>
    public abstract Task<CreateThingResult> CreateAsync(Schema? schema, string thingName, Dictionary<string, object?> properties, CancellationToken cancellationToken);

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
    public virtual async IAsyncEnumerable<Thing> LoadAllForSchema(
        string schemaGuid,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // This may not be an efficient implementation for a database that can fully hydrate objects on an enumeration.
        // It happens to be efficient for CSV+Index (Local) and in-memory caches.
        await foreach (var reference in GetBySchemaAsync(schemaGuid, cancellationToken))
        {
            var thing = await LoadAsync(reference.Guid, cancellationToken);
            if (thing != null)
            {
                yield return thing;
            }
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<Thing> FindBySchemaAndPropertyValue(
        string schemaGuid,
        string propName,
        object? propValue,
        IComparer comparer,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // This may not be an efficient implementation for a database that can fully hydrate objects on an enumeration.
        // It happens to be efficient for CSV+Index (Local) and in-memory caches.

        // This might look a lot like GetBySchemaAsync, but it is a special version
        // that holds onto the loaded thing object for property testing.
        // It also cannot use quite the same index-shortcut in GetBySchemaAsync because
        // it returns a fully loaded Thing, not just a reference.
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaGuid);
        ArgumentException.ThrowIfNullOrWhiteSpace(propName);
        ArgumentNullException.ThrowIfNull(comparer);

        async Task<bool> ThingMatches(Thing thing)
        {
            var prop = await thing.GetPropertyByTrueNameAsync(propName, cancellationToken);
            if (prop == null)
            {
                return propValue == null; // Property is missing from thing (but maybe we wanted nulls).
            }

            if (!prop.HasValue && propValue == null)
            {
                return true; // We want nulls, and this is null.
            }

            if (!prop.HasValue)
            {
                return false; // We do not want nulls, and this is null.
            }

            return comparer.Compare(prop.Value.Value, propValue) == 0;
        }

        await foreach (var thing in LoadAllForSchema(schemaGuid, cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            if (thing != null && await ThingMatches(thing))
            {
                yield return thing;
            }
        }
    }

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
    public async Task<(bool, Dictionary<Reference, (bool success, string message)> results)> TryBulkUpdate(Dictionary<Reference, Dictionary<string, object?>> changes, CancellationToken cancellationToken)
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

            var tsr = await thing.Set(change.Value, cancellationToken);
            if (!tsr.Success)
            {
                results.TryAdd(change.Key, (false, tsr.Messages == null || tsr.Messages.Length == 0 ? "No error message provided." : string.Join("; ", tsr.Messages)));
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

    /// <summary>
    /// Attempts to update multiple properties on a thing.
    /// </summary>
    /// <param name="thingGuid">Unique identifier of the <see cref="Thing"/> to update.</param>
    /// <param name="changes">The properties and values to update on the thing.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tuple containing a value whether the operations was successful.</returns>
    public async Task<(bool success, string message)> TryUpdate(string thingGuid, Dictionary<string, object?> changes, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(thingGuid);
        ArgumentNullException.ThrowIfNull(changes);

        var results = new Dictionary<Reference, (bool success, string message)>();
        if (changes.Count == 0)
        {
            return (true, "No changes provided.");
        }

        var thing = await LoadAsync(thingGuid, cancellationToken);
        if (thing == null)
        {
            return (false, "Unable to load from underlying data store.");
        }

        var tsr = await thing.Set(changes, cancellationToken);
        if (!tsr.Success)
        {
            return (false, tsr.Messages == null || tsr.Messages.Length == 0 ? "No error message provided." : string.Join("; ", tsr.Messages));
        }

        var (saveSuccess, saveMessage) = await thing.SaveAsync(cancellationToken);
        if (!saveSuccess)
        {
            return (false, saveMessage ?? "Unable to save thing to underlying data store.");
        }

        return (true, "Update successful");
    }

    /// <inheritdoc/>
    public abstract Task<bool> RenumberIncrementField(string schemaGuid, CancellationToken cancellationToken);

    /// <summary>
    /// If this schema has an increment field, set its value.
    /// </summary>
    /// <param name="schema">The schema used to create the thing.</param>
    /// <param name="thing">The thing.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Results of the attempt.</returns>
    protected async Task<(bool success, string? message)> CreateAsyncIncrementFieldInternal(Schema? schema, Thing thing, CancellationToken cancellationToken)
    {
        // If this schema has an increment field, set its value.
        if (schema != null)
        {
            var increment = schema.GetIncrementField();
            if (increment != null)
            {
                var next = increment.NextValue;
                var tsrIncrement = await thing.Set(increment.Name, next, cancellationToken);
                if (!tsrIncrement.Success)
                {
                    AmbientErrorContext.Provider.LogWarning($"Unable to update increment field {increment.Name}: {((tsrIncrement.Messages == null || tsrIncrement.Messages.Length == 0) ? "No error message provided." : string.Join("; ", tsrIncrement.Messages))}");
                    return (false, $"Unable to update increment field {increment.Name}: {((tsrIncrement.Messages == null || tsrIncrement.Messages.Length == 0) ? "No error message provided." : string.Join("; ", tsrIncrement.Messages))}");
                }

                var (saveSuccess, saveMessage) = await thing.SaveAsync(cancellationToken);
                if (!saveSuccess)
                {
                    AmbientErrorContext.Provider.LogWarning($"Unable to save increment field {increment.Name} update: {saveMessage}");
                    return (false, $"Unable to save increment field {increment.Name} update: {saveMessage}");
                }

                // Update schema so next = next + 1.
                var ssp = AmbientStorageContext.StorageProvider?.GetSchemaStorageProvider();
                if (ssp == null)
                {
                    AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_SCHEMA_STORAGE_PROVIDER);
                    return (false, AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_SCHEMA_STORAGE_PROVIDER);
                }

                increment.NextValue += 1;
                var (schemaSavedSuccess, schemaSavedMessage) = await ssp.SaveAsync(schema, cancellationToken);
                if (!schemaSavedSuccess)
                {
                    AmbientErrorContext.Provider.LogError($"Unable to save schema '{schema.Name}' ({schema.Guid}): {schemaSavedMessage}");
                }
            }
        }

        return (true, null);
    }
}
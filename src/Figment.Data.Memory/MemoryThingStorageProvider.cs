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

using System.Runtime.CompilerServices;
using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;

namespace Figment.Data.Memory;

/// <summary>
/// A <see cref="Thing"/> storage provider implementation that stores objects in memory.
/// </summary>
public class MemoryThingStorageProvider : ThingStorageProviderBase, IThingStorageProvider
{
    private static readonly Dictionary<string, Thing> ThingCache = [];

    /// <inheritdoc/>
    public override async Task<Reference> FindByNameAsync(string exactName, CancellationToken cancellationToken, StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase)
    {
        await foreach (var reference in FindByNameAsync(
            new Func<string, bool>(x => string.Equals(x, exactName, comparisonType)), cancellationToken))
        {
            return reference.reference; // Returns the first match
        }

        return Reference.EMPTY;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private static async IAsyncEnumerable<(Reference reference, string name)> FindByNameAsync(Func<string, bool> selector, [EnumeratorCancellation] CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        ArgumentNullException.ThrowIfNull(selector);

        foreach (var thing in ThingCache.Values.Where(t => selector(t.Name)))
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            if (thing != null)
                yield return (new Reference
                {
                    Guid = thing.Guid,
                    Type = Reference.ReferenceType.Thing
                }, thing.Name);
        }

        yield break;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    /// <inheritdoc/>
    public override async IAsyncEnumerable<PossibleNameMatch> FindByPartialNameAsync(string schemaGuid, string thingNamePart, [EnumeratorCancellation] CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaGuid);
        ArgumentException.ThrowIfNullOrWhiteSpace(thingNamePart);

        foreach (var thing in ThingCache.Values.Where(e => e.Name.StartsWith(thingNamePart, StringComparison.CurrentCultureIgnoreCase)))
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            yield return new PossibleNameMatch
            {
                Reference = new()
                {
                    Type = Reference.ReferenceType.Thing,
                    Guid = thing.Guid
                },
                Name = thing.Name
            };
        }
    }

    /// <inheritdoc/>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public override async IAsyncEnumerable<(Reference reference, string? name)> GetAll([EnumeratorCancellation] CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        foreach (var thing in ThingCache.Values)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            yield return (new Reference
            {
                Guid = thing.Guid,
                Type = Reference.ReferenceType.Thing
            }, thing?.Name);
        }
    }

    /// <inheritdoc/>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public override async IAsyncEnumerable<Thing> LoadAll([EnumeratorCancellation] CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        foreach (var thing in ThingCache.Values)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            yield return thing;
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<Reference> GetBySchemaAsync(string schemaGuid, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaGuid);
        await foreach (var (reference, name) in GetAll(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            var thing = await LoadAsync(reference.Guid, cancellationToken);
            if (thing != null && thing.SchemaGuids.Any(s => string.Equals(s, schemaGuid, StringComparison.Ordinal)))
                yield return reference;
        }
    }

    /// <inheritdoc/>
    public override Task<bool> GuidExists(string thingGuid, CancellationToken _)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(thingGuid);
        return Task.FromResult(ThingCache.ContainsKey(thingGuid));
    }

    /// <inheritdoc/>
    public override Task<Thing?> LoadAsync(string thingGuid, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(thingGuid);

        _ = ThingCache.TryGetValue(thingGuid, out Thing? thing);
        return Task.FromResult(thing);
    }

    /// <inheritdoc/>
    public override Task<(bool success, string? message)> SaveAsync(Thing thing, CancellationToken cancellationToken)
    {
        ThingCache[thing.Guid] = thing;
        return Task.FromResult<(bool, string?)>((true, $"Thing {thing.Name} saved."));
    }

    /// <inheritdoc/>
    public override async Task<CreateThingResult> CreateAsync(Schema? schema, string thingName, Dictionary<string, object?> properties, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(thingName);
        ArgumentNullException.ThrowIfNull(properties);

        var thingGuid = Guid.NewGuid().ToString();
        var thing = new Thing(thingGuid, thingName)
        {
            SchemaGuids = schema == null ? [] : [schema.Guid],
            CreatedOn = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            LastAccessed = DateTime.UtcNow,
        };
        ThingCache.Add(thingGuid, thing);

        if (schema != null)
            await AssociateWithSchemaInternal(thing, schema, cancellationToken);

        // If this schema has an increment field, set its value.
        var caifi = await CreateAsyncIncrementFieldInternal(schema, thing, cancellationToken);
        if (!caifi.success)
        {
            return new CreateThingResult { Success = caifi.success, Message = caifi.message };
        }

        var tsr = await thing.Set(properties, cancellationToken);
        if (!tsr.Success)
        {
            return new CreateThingResult { Success = false, Message = tsr.Messages == null || tsr.Messages.Length == 0 ? "No error message provided." : string.Join("; ", tsr.Messages) };
        }

        var (success, message) = await SaveAsync(thing, cancellationToken);
        if (!success)
        {
            return new CreateThingResult { Success = false, Message = message };
        }

        // Load fresh to handle any schema defaults/calculated fields
        thing = await LoadAsync(thingGuid, cancellationToken);

        return new CreateThingResult { Success = true, NewThing = thing };
    }

    /// <inheritdoc/>
    public override async Task<(bool success, Thing? thing)> AssociateWithSchemaAsync(string thingGuid, Schema schema, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(thingGuid);
        ArgumentNullException.ThrowIfNull(schema);

        var thing = await LoadAsync(thingGuid, cancellationToken);
        if (thing == null)
            return (false, null);

        return await AssociateWithSchemaInternal(thing, schema, cancellationToken);
    }

    private static async Task<(bool success, Thing? thing)> AssociateWithSchemaInternal(Thing thing, Schema schema, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(thing);
        ArgumentNullException.ThrowIfNull(schema);

        if (!thing.SchemaGuids.Contains(schema.Guid))
        {
            thing.SchemaGuids.Add(schema.Guid);
            var (saved, _) = await thing.SaveAsync(cancellationToken);
            if (!saved)
                return (false, null);
        }

        return (true, thing);
    }

    /// <inheritdoc/>
    public override async Task<(bool, Thing?)> DissociateFromSchemaAsync(string thingGuid, string schemaGuid, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(thingGuid);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaGuid);

        var thing = await LoadAsync(thingGuid, cancellationToken);
        if (thing == null)
            return (false, null);

        return await DissociateFromSchemaInternal(thing, schemaGuid, cancellationToken);
    }

    private static async Task<(bool, Thing?)> DissociateFromSchemaInternal(Thing thing, string schemaGuid, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(thing);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaGuid);

        if (thing.SchemaGuids.Contains(schemaGuid))
        {
            thing.SchemaGuids.RemoveAll(new Predicate<string>(s => string.Equals(schemaGuid, s, StringComparison.InvariantCultureIgnoreCase)));
            var (saved, saveMessage) = await thing.SaveAsync(cancellationToken);
            if (!saved)
                return (false, null);
        }

        return (true, thing);
    }

    /// <inheritdoc/>
    public override Task<bool> RebuildIndexes(CancellationToken cancellationToken) => Task.FromResult(true);

    /// <inheritdoc/>
    public override Task<bool> DeleteAsync(string thingGuid, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(thingGuid);
        return Task.FromResult(ThingCache.Remove(thingGuid));
    }

    /// <inheritdoc/>
    public override async Task<bool> RenumberIncrementField(string schemaGuid, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaGuid);

        var ssp = AmbientStorageContext.StorageProvider?.GetSchemaStorageProvider();
        if (ssp == null)
        {
            AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_SCHEMA_STORAGE_PROVIDER);
            return false;
        }

        var schema = await ssp.LoadAsync(schemaGuid, cancellationToken);
        if (schema == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load schema '{schemaGuid}'.");
            return false;
        }

        // Does this schema have an increment field?  If so, choose the first, ordered by the key.
        var incrementProperty = schema.GetIncrementField();
        if (incrementProperty == default)
        {
            return false;
        }

        Dictionary<Reference, (long existingId, DateTime createdOn)>? metadata = [];

        await foreach (var thing in LoadAllForSchema(schemaGuid, cancellationToken))
        {
            long existingId = 0;
            if (thing.Properties.TryGetValue(incrementProperty.Name, out object? eid)
                && long.TryParse(eid.ToString() ?? string.Empty, out long eidLong))
            {
                existingId = eidLong;
            }

            metadata.Add(thing, (existingId, thing.CreatedOn));
        }

        // Write INCREMENT index
        var reorderedBase = metadata
            .OrderBy(x => x.Value.existingId)
            .ThenBy(x => x.Value.createdOn)
            .Select((x, i) => new { reference = x.Key, index = (ulong)i + 1 })
            .ToArray();

        var reorderedBulk = reorderedBase
            .ToDictionary(k => k.reference, v => new Dictionary<string, object?> { { incrementProperty.Name, v.index } });

        // Update things with updated values
        var (bulkSuccess, _) = await TryBulkUpdate(reorderedBulk, cancellationToken);
        if (!bulkSuccess)
        {
            AmbientErrorContext.Provider.LogWarning($"Bulk update of increment values on {schemaGuid} failed.");
        }

        // Save new maximum to the increment field definition on the schema.
        incrementProperty.NextValue = reorderedBase.Max(x => x.index) + 1;
        var (schemaSaveSuccess, schemaSaveMessage) = await schema.SaveAsync(cancellationToken);
        if (!schemaSaveSuccess)
        {
            AmbientErrorContext.Provider.LogError($"Unable to update increment field {incrementProperty.Name} on {schema.Name}: {schemaSaveMessage}");
            return false;
        }

        AmbientErrorContext.Provider.LogInfo($"Renumbered increments on schema: {schema.Name}");
        return true;
    }
}

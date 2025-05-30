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
    private async IAsyncEnumerable<(Reference reference, string name)> FindByNameAsync(Func<string, bool> selector, [EnumeratorCancellation] CancellationToken cancellationToken)
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
    public override async Task<Thing?> CreateAsync(string? schemaGuid, string thingName, CancellationToken cancellationToken)
    {
        var thingGuid = Guid.NewGuid().ToString();
        var thing = new Thing(thingGuid, thingName)
        {
            SchemaGuids = [schemaGuid],
            CreatedOn = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            LastAccessed = DateTime.UtcNow,
        };
        ThingCache.Add(thingGuid, thing);

        if (!string.IsNullOrWhiteSpace(schemaGuid))
            await AssociateWithSchemaInternal(thing, schemaGuid, cancellationToken);

        // Load fresh to handle any schema defaults/calculated fields
        return await LoadAsync(thingGuid, cancellationToken);
    }

    /// <inheritdoc/>
    public override async Task<(bool, Thing?)> AssociateWithSchemaAsync(string thingGuid, string schemaGuid, CancellationToken cancellationToken)
    {
        var thing = await LoadAsync(thingGuid, cancellationToken);
        if (thing == null)
            return (false, null);

        return await AssociateWithSchemaInternal(thing, schemaGuid, cancellationToken);
    }

    private static async Task<(bool, Thing?)> AssociateWithSchemaInternal(Thing thing, string schemaGuid, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(thing);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaGuid);

        if (!thing.SchemaGuids.Contains(schemaGuid))
        {
            thing.SchemaGuids.Add(schemaGuid);
            var (saved, _) = await thing.SaveAsync(cancellationToken);
            if (!saved)
                return (false, null);
        }

        return (true, thing);
    }

    /// <inheritdoc/>
    public override async Task<(bool, Thing?)> DissociateFromSchemaAsync(string thingGuid, string schemaGuid, CancellationToken cancellationToken)
    {
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
}

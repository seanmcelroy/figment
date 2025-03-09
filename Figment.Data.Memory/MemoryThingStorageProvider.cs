using System.Runtime.CompilerServices;
using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;

namespace Figment.Data.Memory;

public class MemoryThingStorageProvider : IThingStorageProvider
{
    private static readonly Dictionary<string, Thing> ThingCache = [];

    public async Task<Reference> FindByNameAsync(string exactName, CancellationToken cancellationToken, StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase)
    {
        await foreach (var reference in FindByNameAsync(
            new Func<string, bool>(x => string.Compare(x, exactName, comparisonType) == 0), cancellationToken))
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

    public async IAsyncEnumerable<(Reference reference, string name)> FindByPartialNameAsync(string schemaGuid, string thingNamePart, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaGuid);
        ArgumentException.ThrowIfNullOrWhiteSpace(thingNamePart);

        foreach (var thing in ThingCache.Values.Where(e => e.Name.StartsWith(thingNamePart, StringComparison.CurrentCultureIgnoreCase)))
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            yield return (new Reference
            {
                Type = Reference.ReferenceType.Thing,
                Guid = thing.Guid
            }, thing.Name);
        }
    }

    /// <summary>
    /// Gets all things
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to abort the enumerator</param>
    /// <returns>Each thing</returns>
    /// <remarks>This may be a very expensive operation</remarks>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async IAsyncEnumerable<(Reference reference, string? name)> GetAll([EnumeratorCancellation] CancellationToken cancellationToken)
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

    public async IAsyncEnumerable<Reference> GetBySchemaAsync(string schemaGuid, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaGuid);
        await foreach (var (reference, name) in GetAll(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            var thing = await LoadAsync(reference.Guid, cancellationToken);
            if (thing != null && thing.SchemaGuids.Any(s => string.CompareOrdinal(s, schemaGuid) == 0))
                yield return reference;
        }
    }

    public Task<bool> GuidExists(string thingGuid, CancellationToken _)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(thingGuid);
        return Task.FromResult(ThingCache.ContainsKey(thingGuid));
    }

    public Task<Thing?> LoadAsync(string thingGuid, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(thingGuid);

        _ = ThingCache.TryGetValue(thingGuid, out Thing? thing);
        return Task.FromResult(thing);
    }

    public Task<bool> SaveAsync(Thing thing, CancellationToken cancellationToken)
    {
        ThingCache[thing.Guid] = thing;
        return Task.FromResult(true);
    }

    public async Task<Thing?> CreateAsync(string? schemaGuid, string thingName, CancellationToken cancellationToken)
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

    public async Task<(bool, Thing?)> AssociateWithSchemaAsync(string thingGuid, string schemaGuid, CancellationToken cancellationToken)
    {
        var thing = await LoadAsync(thingGuid, cancellationToken);
        if (thing == null)
            return (false, null);

        return await AssociateWithSchemaInternal(thing, schemaGuid, cancellationToken);
    }

    private async Task<(bool, Thing?)> AssociateWithSchemaInternal(Thing thing, string schemaGuid, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(thing);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaGuid);

        if (!thing.SchemaGuids.Contains(schemaGuid))
        {
            thing.SchemaGuids.Add(schemaGuid);
            var saved = await thing.SaveAsync(cancellationToken);
            if (!saved)
                return (false, null);
        }

        return (true, thing);
    }

    public async Task<(bool, Thing?)> DissociateFromSchemaAsync(string thingGuid, string schemaGuid, CancellationToken cancellationToken)
    {
        var thing = await LoadAsync(thingGuid, cancellationToken);
        if (thing == null)
            return (false, null);

        return await DissociateFromSchemaInternal(thing, schemaGuid, cancellationToken);
    }

    private async Task<(bool, Thing?)> DissociateFromSchemaInternal(Thing thing, string schemaGuid, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(thing);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaGuid);

        if (thing.SchemaGuids.Contains(schemaGuid))
        {
            thing.SchemaGuids.RemoveAll(new Predicate<string>(s => string.Compare(schemaGuid, s, StringComparison.InvariantCultureIgnoreCase) == 0));
            var saved = await thing.SaveAsync(cancellationToken);
            if (!saved)
                return (false, null);
        }

        return (true, thing);
    }

    public Task<bool> RebuildIndexes(CancellationToken cancellationToken) => Task.FromResult(true);

    public Task<bool> DeleteAsync(string thingGuid, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(thingGuid);
        return Task.FromResult(ThingCache.Remove(thingGuid));
    }
}

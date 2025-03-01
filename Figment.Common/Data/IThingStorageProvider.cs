namespace Figment.Common.Data;

public interface IThingStorageProvider
{
    public Task<Thing?> CreateAsync(string? schemaGuid, string thingName, CancellationToken cancellationToken);

    public Task<bool> DeleteAsync(string schemaGuid, CancellationToken cancellationToken);

    public IAsyncEnumerable<Reference> GetBySchemaAsync(string schemaGuid, CancellationToken cancellationToken);

    public Task<Reference> FindByNameAsync(string exactName, CancellationToken cancellationToken, StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase);

    public IAsyncEnumerable<(Reference reference, string name)> FindByPartialNameAsync(string schemaGuid, string thingNamePart, CancellationToken cancellationToken);

    public IAsyncEnumerable<(Reference reference, string? name)> GetAll(CancellationToken cancellationToken);

    public Task<bool> GuidExists(string thingGuid, CancellationToken cancellationToken);

    public Task<Thing?> LoadAsync(string thingGuid, CancellationToken cancellationToken);

    public Task<bool> RebuildIndexes(CancellationToken cancellationToken);

    public Task<bool> SaveAsync(Thing thing, CancellationToken cancellationToken);
}
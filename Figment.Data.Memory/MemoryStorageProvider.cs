
using Figment.Common.Data;

namespace Figment.Data.Memory;

public class MemoryStorageProvider() : IStorageProvider
{
    public ISchemaStorageProvider? GetSchemaStorageProvider() => new MemorySchemaStorageProvider();

    public IThingStorageProvider? GetThingStorageProvider() => new MemoryThingStorageProvider();

    public Task<bool> InitializeAsync(CancellationToken cancellationToken) => Task.FromResult(true);
}
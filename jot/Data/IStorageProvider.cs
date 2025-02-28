namespace Figment.Data;

public interface IStorageProvider
{
    /// <summary>
    /// Request the storage provider prepare for requests
    /// </summary>
    /// <param name="cancellationToken">A cancellation token for asynchronous methods</param>
    /// <returns>A value indicating whether the storage provider successfully initialized and is ready to take requests</returns>
    public Task<bool> InitializeAsync(CancellationToken cancellationToken);

    public ISchemaStorageProvider? GetSchemaStorageProvider();

    public IThingStorageProvider? GetThingStorageProvider();
}
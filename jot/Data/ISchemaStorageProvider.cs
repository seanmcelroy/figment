namespace Figment.Data;

public interface ISchemaStorageProvider
{
    /// <summary>
    /// Creates a new schema
    /// </summary>
    /// <param name="schemaName">The name of the schema</param>
    /// <param name="cancellationToken">A cancellation token for asynchronous methods</param>
    /// <returns>True, if the operation was successful.  Otherwise, false.</returns>
    public Task<CreateSchemaResult> CreateAsync(
        string schemaName,
        CancellationToken cancellationToken);

    public Task<Reference> FindByNameAsync(string schemaName, CancellationToken cancellationToken);

    public IAsyncEnumerable<Reference> FindByPartialNameAsync(string thingNamePart, CancellationToken cancellationToken);

    public IAsyncEnumerable<Reference> FindByPluralNameAsync(string plural, CancellationToken cancellationToken);

    public Task<bool> GuidExists(string schemaGuid, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all schemas
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to abort the enumerator</param>
    /// <returns>Each schema</returns>
    /// <remarks>This may be a very expensive operation</remarks>
    public IAsyncEnumerable<(Reference reference, string? name)> GetAll(CancellationToken cancellationToken);

    public Task<Schema?> LoadAsync(string schemaGuid, CancellationToken cancellationToken);

    public Task<bool> RebuildIndexes(CancellationToken cancellationToken);

    public Task<bool> SaveAsync(Schema schema, CancellationToken cancellationToken);
}
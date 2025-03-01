namespace Figment.Common.Data;

public readonly record struct CreateSchemaResult
{
    /// <summary>
    /// True if the operation was successful, otherwise false
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// If the operation was succesful, this is the Guid of the new schema
    /// </summary>
    public string? NewGuid { get; init; }
}
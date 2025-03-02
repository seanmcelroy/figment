using Figment.Common.Data;

namespace Figment.Common;

public class SchemaSchemaField(string Name) : SchemaTextField(Name)
{
    public override Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken) => Task.FromResult("schema");

    public override async Task<bool> IsValidAsync(object? value, CancellationToken cancellationToken)
    {
        if (!await base.IsValidAsync(value, cancellationToken))
            return false;

        var str = value as string;
        if (string.IsNullOrWhiteSpace(str))
            return false;

        var ssp = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        if (ssp == null)
            return true; // Assume.

        var exists = await ssp.GuidExists(str, cancellationToken);
        return exists;
    }
}
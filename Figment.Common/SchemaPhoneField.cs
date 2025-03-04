namespace Figment.Common;

public class SchemaPhoneField(string Name) : SchemaTextField(Name)
{
    public const string SCHEMA_FIELD_TYPE = "phone";

    public override Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken) => Task.FromResult(SCHEMA_FIELD_TYPE);
}
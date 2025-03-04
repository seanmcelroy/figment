using System.Text.Json.Serialization;

namespace Figment.Common;

public class SchemaIntegerField(string Name) : SchemaFieldBase(Name)
{
    public const string SCHEMA_FIELD_TYPE = "integer";

    [JsonPropertyName("type")]
    public override string Type { get; } = SCHEMA_FIELD_TYPE;

    public override Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken) => Task.FromResult(SCHEMA_FIELD_TYPE);

    public override Task<bool> IsValidAsync(object? value, CancellationToken _)
    {
        if (!Required && value == null)
            return Task.FromResult(true);
        if (Required && value == null)
            return Task.FromResult(false);

        return Task.FromResult(long.TryParse(value!.ToString(), out long _));
    }
}
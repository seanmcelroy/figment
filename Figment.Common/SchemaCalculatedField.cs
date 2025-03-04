using System.Text.Json.Serialization;

namespace Figment.Common;

public class SchemaCalculatedField(string Name) : SchemaFieldBase(Name)
{
    public const string SCHEMA_FIELD_TYPE = "calculated";

    [JsonPropertyName("type")]
    public override string Type { get; } = SCHEMA_FIELD_TYPE;

    [JsonPropertyName("formula")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Formula { get; set; }

    public override Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken) => Task.FromResult(SCHEMA_FIELD_TYPE);

    public override async Task<bool> IsValidAsync(object? value, CancellationToken cancellationToken)
    {
        return true;
    }
}
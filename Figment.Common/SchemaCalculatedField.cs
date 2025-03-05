using System.Text.Json.Serialization;
using Figment.Common.Calculations;

namespace Figment.Common;

public class SchemaCalculatedField(string Name) : SchemaFieldBase(Name)
{
    public const string SCHEMA_FIELD_TYPE = "calculated";

    [JsonPropertyName("type")]
    public override string Type { get; } = SCHEMA_FIELD_TYPE;

    [JsonPropertyName("formula")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Formula { get; set; }

    public override Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken) => Task.FromResult($"calculated: {Formula}");

    public override Task<bool> IsValidAsync(object? value, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Formula))
            return Task.FromResult(false);

        var (success, _, _) = Parser.ParseFormula(Formula);
        return Task.FromResult(success);
    }
}
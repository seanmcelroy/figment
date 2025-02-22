using System.Text.Json.Serialization;

namespace Figment;

public class SchemaNumberField(string Name) : SchemaFieldBase(Name)
{
    [JsonPropertyName("type")]
    public override string Type { get; } = "number";

    public override Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken) => Task.FromResult("number");

    public override Task<bool> IsValidAsync(object? value, CancellationToken _)
    {
        if (!Required && value == null)
            return Task.FromResult(true);
        if (Required && value == null)
            return Task.FromResult(false);

        return Task.FromResult(double.TryParse(value!.ToString(), out double _));
    }
}
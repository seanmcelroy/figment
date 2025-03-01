using System.Text.Json.Serialization;

namespace Figment.Common;

public class SchemaIntegerField(string Name) : SchemaFieldBase(Name)
{
    [JsonPropertyName("type")]
    public override string Type { get; } = "integer";

    public override Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken) => Task.FromResult("integer");

    public override Task<bool> IsValidAsync(object? value, CancellationToken _)
    {
        if (!Required && value == null)
            return Task.FromResult(true);
        if (Required && value == null)
            return Task.FromResult(false);

        return Task.FromResult(long.TryParse(value!.ToString(), out long _));
    }
}
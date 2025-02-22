using System.Text.Json.Serialization;

namespace Figment;

public class SchemaUriField(string Name) : SchemaFieldBase(Name)
{
    [JsonPropertyName("type")]
    public override string Type { get; } = "text";

    [JsonPropertyName("format")]
    public string Format { get; } = "uri";

    [JsonPropertyName("pattern")]
    public string Pattern { get; init; } = @"[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)";

    public override Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken) => Task.FromResult("uri");

    public override Task<bool> IsValidAsync(object? value, CancellationToken _)
    {
        if (!Required && value == null)
            return Task.FromResult(true);
        if (Required && value == null)
            return Task.FromResult(false);

        return Task.FromResult(Uri.TryCreate(value as string, UriKind.RelativeOrAbsolute, out Uri? _));
    }
}
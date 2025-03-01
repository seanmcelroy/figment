using System.Text.Json.Serialization;

namespace Figment.Common;

public class SchemaUriField(string Name) : SchemaTextField(Name)
{
    [JsonPropertyName("type")]
    public override string Type { get; } = "string";

    [JsonPropertyName("format")]
    public string Format { get; } = "uri";

    [JsonPropertyName("pattern")]
    public override string? Pattern { get; set; } = @"[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)";

    public override Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken) => Task.FromResult("uri");

    public override async Task<bool> IsValidAsync(object? value, CancellationToken cancellationToken)
    {
        if (!await base.IsValidAsync(value, cancellationToken))
            return false;

        var str = value as string;
        return Uri.TryCreate(str, UriKind.RelativeOrAbsolute, out Uri? _);
    }
}
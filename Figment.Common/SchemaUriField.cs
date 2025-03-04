using System.Text.Json.Serialization;

namespace Figment.Common;

public class SchemaUriField(string Name) : SchemaTextField(Name)
{
    public const string SCHEMA_FIELD_TYPE = "uri";

    [JsonPropertyName("type")]
    public override string Type { get; } = "string"; // SCHEMA_FIELD_TYPE does not match JSON schema

    [JsonPropertyName("format")]
    public string Format { get; } = "uri"; // SCHEMA_FIELD_TYPE does not match JSON schema

    [JsonPropertyName("pattern")]
    public override string? Pattern { get; set; } = @"[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)";

    public override Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken) => Task.FromResult(SCHEMA_FIELD_TYPE);

    public override async Task<bool> IsValidAsync(object? value, CancellationToken cancellationToken)
    {
        if (!await base.IsValidAsync(value, cancellationToken))
            return false;

        var str = value as string;
        return Uri.TryCreate(str, UriKind.RelativeOrAbsolute, out Uri? _);
    }
}
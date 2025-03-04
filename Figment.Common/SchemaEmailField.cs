using System.Net.Mail;
using System.Text.Json.Serialization;

namespace Figment.Common;

public class SchemaEmailField(string Name) : SchemaFieldBase(Name)
{
    public const string SCHEMA_FIELD_TYPE = "email";

    [JsonPropertyName("type")]
    public override string Type { get; } = "string"; // SCHEMA_FIELD_TYPE does not match JSON schema

    [JsonPropertyName("format")]
    public string Format { get; } = "email"; // SCHEMA_FIELD_TYPE does not match JSON schema

    public override Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken) => Task.FromResult(SCHEMA_FIELD_TYPE);

    public override Task<bool> IsValidAsync(object? value, CancellationToken _)
    {
        if (!Required && value == null)
            return Task.FromResult(true);
        if (Required && value == null)
            return Task.FromResult(false);

        return Task.FromResult(MailAddress.TryCreate(value as string, out MailAddress? _));
    }
}
using System.Net.Mail;
using System.Text.Json.Serialization;

namespace Figment.Common;

public class SchemaEmailField(string Name) : SchemaFieldBase(Name)
{
    [JsonPropertyName("type")]
    public override string Type { get; } = "string";

    [JsonPropertyName("format")]
    public string Format { get; } = "email";

    public override Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken) => Task.FromResult("email");

    public override Task<bool> IsValidAsync(object? value, CancellationToken _)
    {
        if (!Required && value == null)
            return Task.FromResult(true);
        if (Required && value == null)
            return Task.FromResult(false);

        return Task.FromResult(MailAddress.TryCreate(value as string, out MailAddress? _));
    }
}
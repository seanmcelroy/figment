using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Figment;

public class SchemaTextField(string Name) : SchemaFieldBase(Name)
{
    [JsonPropertyName("type")]
    public override string Type { get; } = "string";

    [JsonPropertyName("minLength")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ushort? MinLength { get; set; }

    [JsonPropertyName("maxLength")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ushort? MaxLength { get; set; }

    [JsonPropertyName("pattern")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual string? Pattern { get; set; }

    public override Task<bool> IsValidAsync(object? value, CancellationToken _)
    {
        if (!Required && value == null)
            return Task.FromResult(true);
        if (Required && value == null)
            return Task.FromResult(false);

        var str = value as string;

        if (MinLength.HasValue && (str == null || str.Length < MinLength.Value))
            return Task.FromResult(false);
        if (MaxLength.HasValue && str?.Length > MaxLength.Value)
            return Task.FromResult(false);
        if (!string.IsNullOrWhiteSpace(Pattern) && (str == null || !Regex.IsMatch(str, Pattern)))
            return Task.FromResult(false);
        if (Required && value == null)
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

    public static SchemaTextField FromSchemaDefinitionProperty(string name, JsonElement prop, bool required)
    {

        var subs = prop.EnumerateObject().ToDictionary(k => k.Name, v => v.Value);
        ushort? minLength = null, maxLength = null;
        string? minLengthString = null, maxLengthString = null, pattern = null;
        if (subs.TryGetValue("minLength", out JsonElement typeMinLength))
        {
            minLengthString = typeMinLength.ToString();
            if (ushort.TryParse(minLengthString, out ushort ml))
            {
                minLength = ml;
            }
        }
        if (subs.TryGetValue("maxLength", out JsonElement typeMaxLength))
        {
            maxLengthString = typeMinLength.ToString();
            if (ushort.TryParse(maxLengthString, out ushort ml))
            {
                maxLength = ml;
            }
        }
        if (subs.TryGetValue("pattern", out JsonElement typePattern))
        {
            pattern = typePattern.ToString();
        }

        var f = new SchemaTextField(name)
        {
            MinLength = minLength,
            MaxLength = maxLength,
            Pattern = pattern,
            Required = required
        };
        return f;
    }

    public override Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken) => Task.FromResult("text");
}
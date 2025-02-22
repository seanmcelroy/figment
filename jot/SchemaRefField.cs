using System.Text.Json.Serialization;

namespace Figment;

public class SchemaRefField(string Name, string SchemaGuid) : SchemaFieldBase(Name)
{
    [JsonIgnore] // Only for enums.
    public override string Type { get; } = "ref";

    [JsonIgnore]
    public string SchemaGuid { get; set; } = SchemaGuid;

    [JsonPropertyName("ref")]
    public string Id
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SchemaGuid))
                return "???";
            return $"https://figment.seanmcelroy.com/{SchemaGuid}.schema.json";
        }
        set
        {
            if (value.StartsWith("https://figment.seanmcelroy.com/", StringComparison.OrdinalIgnoreCase)
            && value.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase))
            {
                SchemaGuid = value[32..^12];
            }
        }
    }

    public override async Task<bool> IsValidAsync(object? value, CancellationToken cancellationToken)
    {
        if (!Required && value == null)
            return true;
        if (Required && value == null)
            return false;

        if (!await Schema.GuidExists(SchemaGuid, cancellationToken))
            return false;

        if (value is string s && !await Thing.GuidExists(s, cancellationToken))
            return false;

        return true;
    }

    public override async Task<string?> GetMarkedUpFieldValue(object? value, CancellationToken cancellationToken)
    {
        if (value == null)
            return default;

        if (value is not string str)
            return default;

        var thingGuid = str[(str.IndexOf('.') + 1)..];
        var thing = await Thing.LoadAsync(thingGuid, cancellationToken);

        if (thing == null)
            return str;

        return thing.Name;
    }

    public override async Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken)
    {
        if (!await Schema.GuidExists(SchemaGuid, cancellationToken))
            return "???";

        var schemaLoaded = await Schema.LoadAsync(SchemaGuid, cancellationToken);
        if (schemaLoaded == null)
            return "???";

        return $"{schemaLoaded.Name} ({schemaLoaded.Guid})";
    }
}
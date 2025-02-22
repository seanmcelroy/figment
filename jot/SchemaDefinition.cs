using System.Text.Json.Serialization;

namespace Figment;

[method: JsonConstructor]
public record SchemaDefinition(string Guid, string Name, string? Description, string? Plural)

{
    [JsonPropertyName("$schema")]
    public string Schema { get; init; } = "https://json-schema.org/draft/2020-12/schema";

    [JsonPropertyName("$id")]
    public string Id
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Guid))
                return "???";
            return $"https://figment.seanmcelroy.com/{Guid}.schema.json";
        }
        set
        {
            if (value.StartsWith("https://figment.seanmcelroy.com/", StringComparison.OrdinalIgnoreCase)
            && value.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase))
            {
                Guid = value[32..^12];
            }
        }
    }

    [JsonPropertyName("title")]
    public string Name { get; set; } = Name;

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; } = Description;

    [JsonPropertyName("$plural")]
    public string? Plural { get; set; } = Plural;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";

    [JsonPropertyName("required")]
    public string[]? RequiredProperties { get; set; }

    [JsonPropertyName("properties")]
    public Dictionary<string, SchemaFieldBase> Properties { get; set; } = [];

    [JsonIgnore]
    public string Guid { get; set; } = Guid;

    public SchemaDefinition(Schema schema) : this(schema.Guid, schema.Name, schema.Description, schema.Plural)
    {
        Description = schema.Description;
        Plural = schema.Plural;
        RequiredProperties = schema.Properties.Where(sp => sp.Value.Required).Select(sp => sp.Key).ToArray();

        foreach (var prop in schema.Properties)
        {
            Properties.Add(prop.Key, prop.Value);
        }
    }
}
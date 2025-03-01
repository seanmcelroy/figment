using System.Text.Json.Serialization;

namespace Figment.Data.Local;

/// <summary>
/// This is the definition of a <see cref="Figment.Schema"/> when it is persisted
/// to local storage, such as via <see cref="LocalDirectoryStorageProvider"/>
/// </summary>
/// <param name="Guid">The unique identiifer of the schema</param>
/// <param name="Name">The name of the schema</param>
/// <param name="Description">A description for the schema</param>
/// <param name="Plural">The plural word for the schema, which should be the plural form of the value provided in <paramref name="Name"/></param>
[method: JsonConstructor]
public record JsonSchemaDefinition(string Guid, string Name, string? Description, string? Plural)

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

    public JsonSchemaDefinition(Schema schema) : this(schema.Guid, schema.Name, schema.Description, schema.Plural)
    {
        Description = schema.Description;
        Plural = schema.Plural;
        RequiredProperties = schema.Properties.Where(sp => sp.Value.Required).Select(sp => sp.Key).ToArray();

        foreach (var prop in schema.Properties)
        {
            Properties.Add(prop.Key, prop.Value);
        }
    }

    public Schema ToSchema()
    {
        var schema = new Schema(Guid, Name)
        {
            // Optional built-ins
            Description = Description,
            Plural = Plural
        };

        foreach (var prop in Properties)
        {
            var required =
                RequiredProperties != null &&
                RequiredProperties.Any(sdr => string.CompareOrdinal(sdr, prop.Key) == 0);

            prop.Value.Required = required;

            schema.Properties.Add(prop.Key, prop.Value);
        }

        return schema;
    }
}
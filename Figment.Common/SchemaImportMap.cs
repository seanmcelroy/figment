using System.Text.Json.Serialization;

namespace Figment.Common;

public class SchemaImportMap(string Name, string Format) {
    /// <summary>
    /// The name of the import map
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = Name;

    /// <summary>
    /// The format to which this import map appleis
    /// </summary>
    [JsonPropertyName("format")]
    public string Format { get; init; } = Format;

}
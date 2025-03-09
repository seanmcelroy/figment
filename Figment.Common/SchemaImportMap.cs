using System.Text.Json.Serialization;

namespace Figment.Common;

/// <summary>
/// A schema import map is a named configuration that dictates what
/// fields from an import source map to what fields in a schema
/// </summary>
/// <param name="Name">Name of the import map</param>
/// <param name="Format">Format to which this import map applies</param>
public class SchemaImportMap(string Name, string Format) {
    /// <summary>
    /// Name of the import map
    /// </summary>
    /// <example>Google Contacts CSV file export</example>
    [JsonPropertyName("name")]
    public string Name { get; init; } = Name;

    /// <summary>
    /// Format to which this import map applies
    /// </summary>
    /// <example>csv</example>
    [JsonPropertyName("format")]
    public string Format { get; init; } = Format;

    /// <summary>
    /// Fields defined for import
    /// </summary>
    [JsonPropertyName("fields")]
    public List<SchemaImportField> FieldConfiguration = [];
}
using System.Text.Json.Serialization;

namespace Figment.Common;

/// <summary>
/// A schema import map is a named configuration that dictates what
/// fields from an import source map to what fields in a schema.
/// </summary>
/// <param name="Name">Name of the import map.</param>
/// <param name="Format">Format to which this import map applies.</param>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public class SchemaImportMap(string Name, string Format)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    /// <summary>
    /// Gets the name of the import map.
    /// </summary>
    /// <example>Google Contacts CSV file export.</example>
    [JsonPropertyName("name")]
    public string Name { get; init; } = Name;

    /// <summary>
    /// Gets the format to which this import map applies.
    /// </summary>
    /// <example>Such as 'csv'.</example>
    [JsonPropertyName("format")]
    public string Format { get; init; } = Format;

    /// <summary>
    /// Gets the fields defined for import.
    /// </summary>
    [JsonPropertyName("fields")]
    public List<SchemaImportField> FieldConfiguration { get; init; } = [];
}
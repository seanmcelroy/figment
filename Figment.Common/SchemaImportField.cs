using System.Text.Json.Serialization;

namespace Figment.Common;

/// <summary>
/// Field configuration for one schema property that accepts a value from
/// a field on an import source
/// </summary>
/// <param name="SchemaPropertyName">Schema property name into which data is imported</param>
/// <param name="ImportFieldName">Field name from the import source from where data is retrieved</param>
public class SchemaImportField(string SchemaPropertyName, string ImportFieldName)
{
    /// <summary>
    /// Schema property name into which data is imported
    /// </summary>
    /// <remarks>
    /// This is the schema's property name, not any human-readable version.
    /// Because these are bound to one schema, these do not include a
    /// schema GUID prefix, as would be specified on a 'thing'
    /// </remarks>
    /// <example>c9882fca-62ed-4456-8dbb-231ae518a410.[Work Phone]</example>
    [JsonPropertyName("schemaProperty")]
    public string SchemaPropertyName { get; init; } = SchemaPropertyName;

    /// <summary>
    /// Field name from the import source from where data is retrieved
    /// </summary>
    /// <example>Phone 1 - Value</example>
    [JsonPropertyName("importField")]
    public string ImportFieldName { get; init; } = ImportFieldName;

    /// <summary>
    /// If true, if this field is not provided in the import source, the
    /// whole record will be skipped
    /// </summary>
    [JsonPropertyName("skipRecordIfMissing")]
    public bool SkipRecordIfMissing { get; set; }

    /// <summary>
    /// If true, and if a <see cref="ValidationFormula"/> is provided
    /// and the result of that formula is not true, then this record 
    /// will be skipped.
    /// </summary>
    [JsonPropertyName("skipRecordIfInvalid")]
    public bool SkipRecordIfInvalid { get; set; }

    /// <summary>
    /// If specified, the value of this field will be injected as the
    /// field [Value] into a formula provided here.  This allows for
    /// import validation logic to determine whether the value from
    /// the import source is acceptable for import.  This formula
    /// must evaluate to either True or False.
    /// </summary>
    /// <example>
    /// =LEN([Value])>3
    /// </example>
    [JsonPropertyName("validation")]
    public string? ValidationFormula { get; set; }

    /// <summary>
    /// If specified, the value of this field will be injected as the
    /// field [Value] into a formula provided here.  The output of
    /// the formula will be used as the value stored in the schema
    /// property.  This allows for values to be transformed when
    /// imported, such as making fields all uppercase.  This formula
    /// must evaluate to a data type that is the same as the schema
    /// property.
    /// </summary>
    [JsonPropertyName("transform")]
    public string? TransformFormula { get; set; }
}
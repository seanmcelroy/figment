using System.Text.Json.Serialization;

namespace Figment.Common;

/// <summary>
/// Field configuration for one schema property that accepts a value from
/// a field on an import source.
/// </summary>
/// <param name="SchemaPropertyName">Schema property name into which data is imported.</param>
/// <param name="ImportFieldName">Field name from the import source from where data is retrieved.</param>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public class SchemaImportField(string? SchemaPropertyName, string? ImportFieldName)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    /// <summary>
    /// Gets or sets the schema property name into which data is imported.
    /// </summary>
    /// <remarks>
    /// This is the schema's property name, not any human-readable version.
    /// Because these are bound to one schema, these do not include a
    /// schema GUID prefix, as would be specified on a 'thing'.
    /// </remarks>
    /// <example>c9882fca-62ed-4456-8dbb-231ae518a410.[Work Phone].</example>
    [JsonPropertyName("schemaProperty")]
    public string? SchemaPropertyName { get; set; } = SchemaPropertyName;

    /// <summary>
    /// Gets the field name from the import source from where data is retrieved.
    /// </summary>
    /// <example>Phone 1 - Value.</example>
    /// <remarks>This should only ever be null for uninitialized schema metadata.</remarks>
    [JsonPropertyName("importField")]
    public string? ImportFieldName { get; init; } = ImportFieldName;

    /// <summary>
    /// Gets or sets a value indicating whether to skip the whole record
    /// if this field is not provided in the import source.
    /// </summary>
    [JsonPropertyName("skipRecordIfMissing")]
    public bool SkipRecordIfMissing { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to skip the whole record
    /// if this field does not pass its <see cref="ValidationFormula"/>.
    /// </summary>
    [JsonPropertyName("skipRecordIfInvalid")]
    public bool SkipRecordIfInvalid { get; set; }

    /// <summary>
    /// Gets or sets the value of this field will be injected as the
    /// field [Value] into an example formula provided here.  This allows for
    /// import validation logic to determine whether the value from
    /// the import source is acceptable for import.  This formula
    /// must evaluate to either True or False.
    /// </summary>
    /// <example>
    /// =LEN([Value])>3.
    /// </example>
    [JsonPropertyName("validation")]
    public string? ValidationFormula { get; set; }

    /// <summary>
    /// Gets or sets the value of this field will be injected as the
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
using System.ComponentModel;
using Figment.Common;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

/// <summary>
/// The settings supplied to the <see cref="SetSchemaPropertyTypeCommand"/>.
/// </summary>
public class SetSchemaPropertyTypeCommandSettings : SchemaPropertyCommandSettings
{
    /// <summary>
    /// Gets the field type to set, or to delete if blank.
    /// </summary>
    [Description("Field type to set, or to delete if blank")]
    [CommandArgument(0, "[FIELD_TYPE]")]
    public string? FieldType { get; init; }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(PropertyName))
        {
            return ValidationResult.Error("Property name must be set");
        }

        List<string> validFieldTypes = [
            SchemaArrayField.SCHEMA_FIELD_TYPE,
            SchemaBooleanField.SCHEMA_FIELD_TYPE,
            SchemaCalculatedField.SCHEMA_FIELD_TYPE,
            SchemaDateField.SCHEMA_FIELD_TYPE,
            SchemaEmailField.SCHEMA_FIELD_TYPE,
            SchemaIntegerField.SCHEMA_FIELD_TYPE,
            SchemaMonthDayField.SCHEMA_FIELD_TYPE,
            SchemaNumberField.SCHEMA_FIELD_TYPE,
            SchemaPhoneField.SCHEMA_FIELD_TYPE,
            SchemaSchemaField.SCHEMA_FIELD_TYPE,
            "text",
            SchemaUriField.SCHEMA_FIELD_TYPE
        ];
        if (!string.IsNullOrWhiteSpace(FieldType)
            && !validFieldTypes.Contains(FieldType, StringComparer.InvariantCultureIgnoreCase)
            && !FieldType.StartsWith('['))
        {
            return ValidationResult.Error($"If the field type is set, it must be set to a valid value.  Other than enums, which are specified as a comma separate list of value enclosed in brackets, the valid field types are: {validFieldTypes.Aggregate((c, n) => $"{c}, {n}")}.");
        }

        return ValidationResult.Success();
    }
}
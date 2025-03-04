using System.ComponentModel;
using Figment.Common;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class SetSchemaPropertyCommandSettings : SchemaCommandSettings
{
    public const int ARG_POSITION_PROPERTY_NAME = 0;
    public const int ARG_POSITION_FIELD_TYPE = 1;
    public const int ARG_POSITION_FORMULA = 2;

    [Description("The name of the property to change")]
    [CommandArgument(ARG_POSITION_PROPERTY_NAME, "<PROPERTY>")]
    public string? PropertyName { get; init; }

    [Description("The field type to set, or to delete if blank")]
    [CommandArgument(ARG_POSITION_FIELD_TYPE, "[FIELD_TYPE]")]
    public string? Value { get; init; }

    [Description("If the field type is 'calculated', this is the formula to use.")]
    [CommandArgument(ARG_POSITION_FORMULA, "[FORMULA]")]
    public string? Formula { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(PropertyName))
            return ValidationResult.Error("Property name must be set");

        List<string> validFieldTypes = [
            SchemaArrayField.SCHEMA_FIELD_TYPE,
            SchemaBooleanField.SCHEMA_FIELD_TYPE,
            SchemaCalculatedField.SCHEMA_FIELD_TYPE,
            SchemaDateField.SCHEMA_FIELD_TYPE,
            SchemaEmailField.SCHEMA_FIELD_TYPE,
            SchemaIntegerField.SCHEMA_FIELD_TYPE,
            SchemaNumberField.SCHEMA_FIELD_TYPE,
            SchemaPhoneField.SCHEMA_FIELD_TYPE,
            SchemaSchemaField.SCHEMA_FIELD_TYPE,
            "text",
            SchemaUriField.SCHEMA_FIELD_TYPE
        ];
        if (!string.IsNullOrWhiteSpace(Value)
            && !validFieldTypes.Contains(Value, StringComparer.InvariantCultureIgnoreCase)
            && !Value.StartsWith('['))
            return ValidationResult.Error($"If the field type is set, it must be set to a valid value.  Other than enums, which are specified as a comma separate list of value enclosed in brackets, the valid field types are: {validFieldTypes.Aggregate((c,n) => $"{c}, {n}")}.");

        if (string.CompareOrdinal(Value, SchemaCalculatedField.SCHEMA_FIELD_TYPE) == 0
            && string.IsNullOrWhiteSpace(Formula))      
            return ValidationResult.Error($"If the field type is calculated, the FORMULA must be specified.");
    
        return ValidationResult.Success();
    }
}
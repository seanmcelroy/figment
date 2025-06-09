/*
Figment
Copyright (C) 2025  Sean McElroy

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

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
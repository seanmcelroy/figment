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

using Figment.Common;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

/// <summary>
/// Sets the data type of a property.
/// </summary>
public class SetSchemaPropertyTypeCommand : SchemaCancellableAsyncCommand<SetSchemaPropertyTypeCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, SetSchemaPropertyTypeCommandSettings settings, CancellationToken cancellationToken)
    {
        var (tgs, schema, ssp) = await TryGetSchema(settings, cancellationToken);
        if (tgs != Globals.GLOBAL_ERROR_CODES.SUCCESS)
        {
            return (int)tgs;
        }

        // set work phone=+1 (212) 555-5555
        // auto-selects text
        var propName = settings.PropertyName;
        if (string.IsNullOrWhiteSpace(propName))
        {
            AmbientErrorContext.Provider.LogError("To change a property on a schema, specify the property's name.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }
        else if (!settings.OverrideValidation && !ThingProperty.IsPropertyNameValid(propName, out string? message))
        {
            AmbientErrorContext.Provider.LogError($"Property name '{propName}' is invalid: {message}");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        // Handle built-ins
        if (string.IsNullOrWhiteSpace(settings.FieldType))
        {
            // Deletes a property.
            var propToDelete = schema!.Properties.FirstOrDefault(p => string.Equals(p.Key, propName, StringComparison.OrdinalIgnoreCase));
            if (propToDelete.Equals(default(KeyValuePair<string, SchemaFieldBase>)))
            {
                AmbientErrorContext.Provider.LogError($"No property named '{propName}' found on schema '{schema.Name}'");
                return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
            }

            schema.Properties.Remove(propToDelete.Key);
            AmbientErrorContext.Provider.LogWarning($"Deleted property name '{propName}'.");
        }
        else if (string.Equals(settings.FieldType, SchemaArrayField.SCHEMA_FIELD_TYPE, StringComparison.Ordinal))
        {
            // Array
            var saf = new SchemaArrayField(propName)
            {
                Items = new SchemaArrayField.SchemaArrayFieldItems
                {
                    Type = "string",
                },
            };
            schema!.Properties[propName] = saf;
        }
        else if (string.Equals(settings.FieldType, SchemaBooleanField.SCHEMA_FIELD_TYPE, StringComparison.Ordinal))
        {
            // Boolean
            var sbf = new SchemaBooleanField(propName);
            schema!.Properties[propName] = sbf;
        }
        else if (string.Equals(settings.FieldType, SchemaCalculatedField.SCHEMA_FIELD_TYPE, StringComparison.Ordinal))
        {
            // Calculated
            var scf = new SchemaCalculatedField(propName);
            schema!.Properties[propName] = scf;

            // Formula is null at this point.
        }
        else if (string.Equals(settings.FieldType, SchemaDateField.SCHEMA_FIELD_TYPE, StringComparison.Ordinal))
        {
            // Date
            var sdf = new SchemaDateField(propName);
            schema!.Properties[propName] = sdf;
        }
        else if (string.Equals(settings.FieldType, SchemaEmailField.SCHEMA_FIELD_TYPE, StringComparison.Ordinal))
        {
            // Email
            var sef = new SchemaEmailField(propName);
            schema!.Properties[propName] = sef;
        }
        else if (string.Equals(settings.FieldType, SchemaIntegerField.SCHEMA_FIELD_TYPE, StringComparison.Ordinal))
        {
            // Number (integer)
            var sif = new SchemaIntegerField(propName);
            schema!.Properties[propName] = sif;
        }
        else if (string.Equals(settings.FieldType, SchemaMonthDayField.SCHEMA_FIELD_TYPE, StringComparison.Ordinal))
        {
            // Month+day
            var ssf = new SchemaMonthDayField(propName);
            schema!.Properties[propName] = ssf;
        }
        else if (string.Equals(settings.FieldType, SchemaNumberField.SCHEMA_FIELD_TYPE, StringComparison.Ordinal))
        {
            // Number (double)
            var snf = new SchemaNumberField(propName);
            schema!.Properties[propName] = snf;
        }
        else if (string.Equals(settings.FieldType, SchemaPhoneField.SCHEMA_FIELD_TYPE, StringComparison.Ordinal))
        {
            // Phone
            var spf = new SchemaPhoneField(propName);
            schema!.Properties[propName] = spf;
        }
        else if (string.Equals(settings.FieldType, SchemaSchemaField.SCHEMA_FIELD_TYPE, StringComparison.Ordinal))
        {
            // Schema
            var ssf = new SchemaSchemaField(propName);
            schema!.Properties[propName] = ssf;
        }
        else if (string.Equals(settings.FieldType, "text", StringComparison.Ordinal))
        {
            // Text
            var stf = new SchemaTextField(propName);
            schema!.Properties[propName] = stf;
        }
        else if (string.Equals(settings.FieldType, SchemaUriField.SCHEMA_FIELD_TYPE, StringComparison.Ordinal))
        {
            // Uri
            var suf = new SchemaUriField(propName);
            schema!.Properties[propName] = suf;
        }
        else if (settings.FieldType != null
            && settings.FieldType.StartsWith('[')
            && settings.FieldType.EndsWith(']')
            && settings.FieldType.Length >= 5
            && settings.FieldType.Contains(','))
        {
            // Enum
            var enumValues = settings.FieldType[1..^1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var sef = new SchemaEnumField(propName, enumValues);
            schema!.Properties[propName] = sef;
        }
        else
        {
            // Maybe this is the name of a schema.
            var refSchema = await ssp!.FindByNameAsync(settings.FieldType!, cancellationToken);
            if (refSchema == Reference.EMPTY)
            {
                AmbientErrorContext.Provider.LogError($"I do not understand that type of field ({settings.FieldType}).");
                return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
            }

            var srf = new SchemaRefField(propName, refSchema.Guid);
            schema!.Properties[propName] = srf;
        }

        var (saved, saveMessage) = await schema!.SaveAsync(cancellationToken);
        if (!saved)
        {
            if (settings.Verbose ?? false)
            {
                AmbientErrorContext.Provider.LogError($"Unable to save schema '{schema.Name}' ({schema.Guid}): {saveMessage}");
            }
            else
            {
                AmbientErrorContext.Provider.LogError($"Unable to save schema '{schema.Name}': {saveMessage}");
            }

            return (int)Globals.GLOBAL_ERROR_CODES.SCHEMA_SAVE_ERROR;
        }

        AmbientErrorContext.Provider.LogDone($"{schema.Name} saved.");
        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}
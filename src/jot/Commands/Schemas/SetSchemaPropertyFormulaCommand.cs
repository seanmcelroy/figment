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
/// Sets the <see cref="SchemaCalculatedField.Formula"/> expression of a calculated property.
/// </summary>
public class SetSchemaPropertyFormulaCommand : SchemaCancellableAsyncCommand<SetSchemaPropertyFormulaCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, SetSchemaPropertyFormulaCommandSettings settings, CancellationToken cancellationToken)
    {
        var (tgs, schema, ssp) = await TryGetSchema(settings, cancellationToken);
        if (tgs != Globals.GLOBAL_ERROR_CODES.SUCCESS)
        {
            return (int)tgs;
        }

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

        var possibleProperties = schema!.Properties
            .Where(p => string.Equals(p.Key, settings.PropertyName, StringComparison.CurrentCultureIgnoreCase))
            .ToList();

        SchemaFieldBase? selectedProperty;

        switch (possibleProperties.Count)
        {
            case 0:
                AmbientErrorContext.Provider.LogError($"No property found with name '{settings.PropertyName}'.");
                return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
            case 1:
                selectedProperty = possibleProperties[0].Value;
                break;
            default:
                AmbientErrorContext.Provider.LogError($"Ambiguous match; more than one property matches the name '{settings.PropertyName}'.");
                return (int)Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH;
        }

        if (selectedProperty is not SchemaCalculatedField scf)
        {
            AmbientErrorContext.Provider.LogError($"Cannot set formula on property '{settings.PropertyName}' as it is not a 'calculated' field type.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        scf.Formula = settings.Formula;
        schema.Properties[propName] = scf;

        var (saved, saveMessage) = await schema.SaveAsync(cancellationToken);
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

        AmbientErrorContext.Provider.LogDone($"{schema.Name} saved.  Formula for '{settings.PropertyName}' updated.");
        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}
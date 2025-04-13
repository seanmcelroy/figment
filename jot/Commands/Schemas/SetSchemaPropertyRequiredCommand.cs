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
/// Sets the property as a required field on the containing schema.
/// </summary>
public class SetSchemaPropertyRequiredCommand : SchemaCancellableAsyncCommand<SetSchemaPropertyRequiredCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, SetSchemaPropertyRequiredCommandSettings settings, CancellationToken cancellationToken)
    {
        var (tgs, schema, ssp) = await TryGetSchema(settings, cancellationToken);
        if (tgs != Globals.GLOBAL_ERROR_CODES.SUCCESS)
        {
            return (int)tgs;
        }

        var propName = settings.PropertyName;
        var required = settings.Required;

        var sp = schema!.Properties.FirstOrDefault(p => string.Compare(p.Key, propName, StringComparison.CurrentCultureIgnoreCase) == 0);
        if (sp.Equals(default(KeyValuePair<string, SchemaFieldBase>)))
        {
            AmbientErrorContext.Provider.LogError($"No schema field named '{propName}' was found.");
            return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
        }

        var oldRequired = sp.Value.Required;

        sp.Value.Required = required;

        if (oldRequired == sp.Value.Required)
        {
            AmbientErrorContext.Provider.LogWarning($"Required for {propName} is already '{required}'. Nothing to do.");
            return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
        }

        var saved = await schema.SaveAsync(cancellationToken);
        if (!saved)
        {
            if (settings.Verbose ?? false)
            {
                AmbientErrorContext.Provider.LogError($"Unable to save schema '{schema.Name}' ({schema.Guid}).");
            }
            else
            {
                AmbientErrorContext.Provider.LogError($"Unable to save schema '{schema.Name}'.");
            }

            return (int)Globals.GLOBAL_ERROR_CODES.SCHEMA_SAVE_ERROR;
        }

        AmbientErrorContext.Provider.LogDone($"{schema.Name} saved.  Required was '{oldRequired}' but is now '{required}' for '{propName}'.");
        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}
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
using jot.Commands.Schemas;
using jot.Commands.Things;
using Spectre.Console.Cli;

namespace jot.Commands;

/// <summary>
/// Renders the properties of a selected entity.
/// </summary>
public class PrintSelectedCommand : CancellableAsyncCommand<PrintSelectedCommandSettings>, ICommand
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, PrintSelectedCommandSettings settings, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY))
        {
            AmbientErrorContext.Provider.LogError("To view properties on an entity, you must first 'select' one.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        bool? extraVerbose = null;
        if (context.Arguments.Count > 0
            && string.CompareOrdinal(context.Arguments[0], "??") == 0)
        {
            extraVerbose = true;
        }

        switch (selected.Type)
        {
            case Reference.ReferenceType.Schema:
                {
                    var cmd = new PrintSchemaCommand();
                    return await cmd.ExecuteAsync(context, new SchemaCommandSettings
                    {
                        SchemaName = selected.Guid,
                        Verbose = extraVerbose ?? settings.Verbose ?? Program.Verbose,
                    }, cancellationToken);
                }

            case Reference.ReferenceType.Thing:
                {
                    var cmd = new PrintThingCommand();
                    return await cmd.ExecuteAsync(context, new PrintThingCommandSettings
                    {
                        ThingName = selected.Guid,
                        NoPrettyDisplayNames = settings.NoPrettyDisplayNames,
                        Verbose = extraVerbose ?? settings.Verbose ?? Program.Verbose,
                    }, cancellationToken);
                }

            default:
                {
                    AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(selected.Type)}'.");
                    return (int)Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE;
                }
        }
    }
}
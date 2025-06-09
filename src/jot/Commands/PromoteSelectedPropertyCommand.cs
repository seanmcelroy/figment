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
using jot.Commands.Things;
using Spectre.Console.Cli;

namespace jot.Commands;

/// <summary>
/// Promotes the selected property.
/// </summary>
/// <seealso cref="PromoteThingPropertyCommand"/>
public class PromoteSelectedPropertyCommand : CancellableAsyncCommand<PromoteSelectedPropertyCommandSettings>, ICommand
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, PromoteSelectedPropertyCommandSettings settings, CancellationToken cancellationToken)
    {
        if (Program.SelectedEntity.Equals(Reference.EMPTY))
        {
            AmbientErrorContext.Provider.LogError("To set properties on an entity, you must first 'select' it.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        if (string.IsNullOrWhiteSpace(settings.PropertyName))
        {
            AmbientErrorContext.Provider.LogError("To promote a property on a new thing, specify the name of the property.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        switch (Program.SelectedEntity.Type)
        {
            case Reference.ReferenceType.Thing:
                {
                    var cmd = new PromoteThingPropertyCommand();
                    return await cmd.ExecuteAsync(
                        context,
                        new PromoteThingPropertyCommandSettings
                        {
                            ThingName = Program.SelectedEntity.Guid,
                            PropertyName = settings.PropertyName,
                            Verbose = settings.Verbose,
                        },
                        cancellationToken);
                }

            default:
                AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(Program.SelectedEntity.Type)}'.");
                return (int)Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE;
        }
    }
}
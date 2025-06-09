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
/// Renames the selected entity.
/// </summary>
public class RenameSelectedCommand : CancellableAsyncCommand<RenameSelectedCommandSettings>, ICommand
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, RenameSelectedCommandSettings settings, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY))
        {
            AmbientErrorContext.Provider.LogError("To rename an entity, you must first 'select' one.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        switch (selected.Type)
        {
            case Reference.ReferenceType.Schema:
                return await new SchemaRenameCommand().ExecuteAsync(context, new SchemaRenameCommandSettings
                {
                    SchemaName = selected.Guid,
                    NewName = settings.NewName,
                    Verbose = settings.Verbose,
                }, cancellationToken);
            case Reference.ReferenceType.Thing:
                return await new ThingRenameCommand().ExecuteAsync(context, new ThingRenameCommandSettings
                {
                    ThingName = selected.Guid,
                    NewName = settings.NewName,
                    Verbose = settings.Verbose,
                }, cancellationToken);
            default:
                AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(selected.Type)}'.");
                return (int)Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE;
        }
    }
}
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
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Tasks;

/// <summary>
/// Permanently deletes a task.
/// </summary>
public class DeleteTaskCommand : CancellableAsyncCommand<DeleteTaskCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, DeleteTaskCommandSettings settings, CancellationToken cancellationToken)
    {
        var tsp = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (tsp == null)
        {
            AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_THING_STORAGE_PROVIDER);
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var anyFound = false;

        await foreach (var thing in tsp.FindBySchemaAndPropertyValue(
            Figment.Common.Tasks.Task.WellKnownSchemaGuid,
            Figment.Common.Tasks.Task.TrueNameId,
            settings.TaskNumber,
            UnsignedNumberComparer.Default,
            cancellationToken))
        {
            anyFound = true;

            var deleted = await tsp.DeleteAsync(thing.Guid, cancellationToken);
            if (deleted)
            {
                AmbientErrorContext.Provider.LogDone($"Task #{settings.TaskNumber} deleted.");
                if (Program.SelectedEntity == thing)
                {
                    Program.SelectedEntity = Reference.EMPTY;
                    Program.SelectedEntityName = string.Empty;
                }

                break; // Only one can match.
            }
            else
            {
                AmbientErrorContext.Provider.LogError($"Unable to delete Task #{settings.TaskNumber}");
                return (int)Globals.GLOBAL_ERROR_CODES.THING_DELETE_ERROR;
            }
        }

        if (!anyFound)
        {
            AmbientErrorContext.Provider.LogError($"Unable to find Task #{settings.TaskNumber}");
            return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}
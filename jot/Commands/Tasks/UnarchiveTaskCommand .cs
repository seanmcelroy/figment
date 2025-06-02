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
/// Unarchives a task.  This is the reverse of <see cref="ArchiveTaskCommand"/>.
/// </summary>
public partial class UnarchiveTaskCommand : CancellableAsyncCommand<UnarchiveTaskCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, UnarchiveTaskCommandSettings settings, CancellationToken cancellationToken)
    {
        var tsp = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (tsp == null)
        {
            AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_THING_STORAGE_PROVIDER);
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var anyFound = false;

        await foreach (var thing in tsp.FindBySchemaAndPropertyValue(
            WellKnownSchemas.Task.Guid,
            ListTasksCommand.TrueNameId,
            settings.TaskNumber,
            new UnsignedNumberComparer(),
            cancellationToken))
        {
            anyFound = true;

            var tsr = await thing.Set("archived", false, cancellationToken);
            if (tsr.Success)
            {
                var id = await thing.GetPropertyByTrueNameAsync(ListTasksCommand.TrueNameId, cancellationToken);
                var (saveSuccess, saveMessage) = await thing.SaveAsync(cancellationToken);
                if (saveSuccess)
                {
                    AmbientErrorContext.Provider.LogDone($"Task #{id.Value.Value} unarchived.");
                }
                else
                {
                    AmbientErrorContext.Provider.LogError($"Unable to save changes to Task #{id.Value.Value}: {saveMessage}");
                    return (int)Globals.GLOBAL_ERROR_CODES.THING_SAVE_ERROR;
                }
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
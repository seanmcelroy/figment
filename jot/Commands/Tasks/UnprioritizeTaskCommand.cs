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
/// Unprioritizes a task.  This is the reverse of <see cref="PrioritizeTaskCommand"/>.
/// </summary>
public class UnprioritizeTaskCommand : CancellableAsyncCommand<UnprioritizeTaskCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, UnprioritizeTaskCommandSettings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.TaskNumber))
        {
            AmbientErrorContext.Provider.LogError("Task number not provided.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var tsp = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (tsp == null)
        {
            AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_THING_STORAGE_PROVIDER);
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var foundCount = 0;
        var isNumber = false;

        if (ulong.TryParse(settings.TaskNumber, out ulong taskNumber))
        {
            // settings.TaskNumber is actually a number.
            isNumber = true;
            await foreach (var thing in tsp.FindBySchemaAndPropertyValue(
                WellKnownSchemas.Task.Guid,
                ListTasksCommand.TrueNameId,
                settings.TaskNumber,
                new UnsignedNumberComparer(),
                cancellationToken))
            {
                if (!(await thing.GetPropertyByTrueNameAsync(ListTasksCommand.TrueNamePriority, cancellationToken))?.AsBoolean() ?? false)
                {
                    foundCount++;
                    AmbientErrorContext.Provider.LogDone($"Task #{taskNumber} is already unprioritized.");
                    break; // Only one can match.
                }

                var tsr = await thing.Set("priority", false, cancellationToken);
                if (tsr.Success)
                {
                    var id = await thing.GetPropertyByTrueNameAsync(ListTasksCommand.TrueNameId, cancellationToken);
                    var (saveSuccess, saveMessage) = await thing.SaveAsync(cancellationToken);
                    if (saveSuccess)
                    {
                        foundCount++;
                        AmbientErrorContext.Provider.LogDone($"Task #{id.Value.Value} unprioritized.");
                        break; // Only one can match.
                    }
                    else
                    {
                        AmbientErrorContext.Provider.LogError($"Unable to save changes to Task #{id.Value.Value}: {saveMessage}");
                        return (int)Globals.GLOBAL_ERROR_CODES.THING_SAVE_ERROR;
                    }
                }
            }
        }
        else if (settings.TaskNumber.Equals("*", StringComparison.CurrentCultureIgnoreCase))
        {
            // Mark ALL tasks as unprioritized.
            await foreach (var thing in tsp.LoadAllForSchema(
                    WellKnownSchemas.Task.Guid,
                    cancellationToken))
            {
                if (!(await thing.GetPropertyByTrueNameAsync(ListTasksCommand.TrueNamePriority, cancellationToken))?.AsBoolean() ?? false)
                {
                    continue;
                }

                var tsr = await thing.Set("priority", false, cancellationToken);
                if (tsr.Success)
                {
                    var id = await thing.GetPropertyByTrueNameAsync(ListTasksCommand.TrueNameId, cancellationToken);
                    var (saveSuccess, saveMessage) = await thing.SaveAsync(cancellationToken);
                    if (saveSuccess)
                    {
                        foundCount++;
                    }
                    else
                    {
                        AmbientErrorContext.Provider.LogError($"Unable to save changes to Task #{id.Value.Value}: {saveMessage}");
                        return (int)Globals.GLOBAL_ERROR_CODES.THING_SAVE_ERROR;
                    }
                }
            }

            AmbientErrorContext.Provider.LogDone($"Unprioritized {foundCount} tasks.");
        }

        if (foundCount == 0 && isNumber)
        {
            AmbientErrorContext.Provider.LogError($"Unable to find Task #{settings.TaskNumber}");
            return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}
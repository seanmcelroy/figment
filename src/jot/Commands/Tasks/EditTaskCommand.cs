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
/// Edits the detalis of an existing task.
/// </summary>
internal class EditTaskCommand : TaskCommandBase<EditTaskCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, EditTaskCommandSettings settings, CancellationToken cancellationToken)
    {
        var tsp = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (tsp == null)
        {
            AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_THING_STORAGE_PROVIDER);
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var (archived, completed, priority, status, due, taskName) = ParseBodyForValues(string.Join(' ', settings.Segments).Trim(), settings);

        var propertiesToUpdate = new Dictionary<string, object?>();

        if (archived.Specified)
        {
            propertiesToUpdate.Add("archived", archived.Value);
        }

        if (completed.Specified)
        {
            propertiesToUpdate.Add("completed", completed.Value);
        }

        if (priority.Specified)
        {
            propertiesToUpdate.Add("priority", priority.Value);
        }

        if (status.Specified)
        {
            propertiesToUpdate.Add("status", status.Value);
        }

        if (due.Specified)
        {
            if (due.Value == DateTimeOffset.MinValue)
            {
                propertiesToUpdate.Add("due", null);
            }
            else
            {
                propertiesToUpdate.Add("due", due.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(taskName))
        {
            propertiesToUpdate.Add("name", taskName);
        }

        var anyFound = false;

        await foreach (var task in tsp.FindBySchemaAndPropertyValue(
            Figment.Common.Tasks.Task.WellKnownSchemaGuid,
            Figment.Common.Tasks.Task.TrueNameId,
            settings.TaskNumber,
            UnsignedNumberComparer.Default,
            cancellationToken))
        {
            anyFound = true;

            var tsr = await task.Set(propertiesToUpdate, cancellationToken);
            if (!tsr.Success)
            {
                var errorMessage = tsr.Messages == null || tsr.Messages.Length == 0 ? "No error message provided." : string.Join("; ", tsr.Messages);
                AmbientErrorContext.Provider.LogError($"Unable to edit Task #{settings.TaskNumber}: {errorMessage}");
                return (int)Globals.GLOBAL_ERROR_CODES.THING_SAVE_ERROR;
            }

            var (success, message) = await task.SaveAsync(cancellationToken);
            if (!success)
            {
                AmbientErrorContext.Provider.LogError($"Unable to edit Task #{settings.TaskNumber}: {message}");
                return (int)Globals.GLOBAL_ERROR_CODES.THING_SAVE_ERROR;
            }

            AmbientErrorContext.Provider.LogDone($"Task #{settings.TaskNumber} edited.");
            break; // Only one can match.
        }

        if (!anyFound)
        {
            AmbientErrorContext.Provider.LogError($"Unable to find Task #{settings.TaskNumber}");
            return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}
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

using System.Text.RegularExpressions;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Tasks;

/// <summary>
/// Lists all the things in the database.
/// </summary>
public partial class AddTaskCommand : CancellableAsyncCommand<AddTaskCommandSettings>
{
    private enum ERROR_CODES : int
    {
        THING_CREATE_ERROR = -2002,
    }

    [GeneratedRegex(@"(?<=\b)due:(?:.+)$")]
    private static partial Regex DueRegex();

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, AddTaskCommandSettings settings, CancellationToken cancellationToken)
    {
        var schemaProvider = AmbientStorageContext.StorageProvider?.GetSchemaStorageProvider();
        if (schemaProvider == null)
        {
            AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_SCHEMA_STORAGE_PROVIDER);
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var taskSchema = await schemaProvider.LoadAsync(WellKnownSchemas.Task.Guid, cancellationToken);
        if (taskSchema == null)
        {
            AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_BUILT_IN_SCHEMA);
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var thingProvider = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (thingProvider == null)
        {
            AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_THING_STORAGE_PROVIDER);
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var taskName = string.Join(' ', settings.Segments).Trim();

        var task = await thingProvider.CreateAsync(taskSchema, taskName, cancellationToken);
        if (task == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to create task.");
            return (int)ERROR_CODES.THING_CREATE_ERROR;
        }

        if (settings.Priority ?? false)
        {
            var tsr = await task.Set("priority", settings.Priority ?? false, cancellationToken);
            if (!tsr.Success)
            {
                // Not fatal, but warn.
                AmbientErrorContext.Provider.LogWarning($"Unable to set priority '{settings.Priority ?? false}' on task.");
            }
        }

        if (!string.IsNullOrWhiteSpace(settings.Status))
        {
            var tsr = await task.Set("status", settings.Status, cancellationToken);
            if (!tsr.Success)
            {
                // Not fatal, but warn.
                AmbientErrorContext.Provider.LogWarning($"Unable to set status '{settings.Status}' on task.");
            }
        }

        var dueMatch = DueRegex().Match(taskName, 1); // Don't allow the name to ONLY be a due date.
        if (dueMatch.Success)
        {
            var (dueDate, _) = ListTasksCommand.ParseFlagDateValue(dueMatch.Value[4..]);

            // The -1 is okay because the regex asserts a \b preceding.
#pragma warning disable SA1008 // Opening parenthesis should be spaced correctly
            task.Name = task.Name[..(dueMatch.Index - 1)];
#pragma warning restore SA1008 // Opening parenthesis should be spaced correctly
            var tsr = await task.Set("due", dueDate, cancellationToken);
            if (!tsr.Success)
            {
                // Not fatal, but warn.
                AmbientErrorContext.Provider.LogWarning($"Unable to set due date '{dueMatch.Value}' on task.");
            }
        }
        else if (!string.IsNullOrWhiteSpace(settings.DueDate))
        {
            // If we could not match a due in the text (and adjust it accordingly, THEN we will respect the command option.)
            var (dueDate, _) = ListTasksCommand.ParseFlagDateValue(settings.DueDate);

            var tsr = await task.Set("due", dueDate, cancellationToken);
            if (!tsr.Success)
            {
                // Not fatal, but warn.
                AmbientErrorContext.Provider.LogWarning($"Unable to set due date '{settings.DueDate}' on task.");
            }
        }

        if (task.IsDirty)
        {
            await task.SaveAsync(cancellationToken);
        }

        var taskIdPropValue = await task.GetPropertyByTrueNameAsync(ListTasksCommand.TrueNameId, cancellationToken);
        var taskId = taskIdPropValue?.AsUInt64();

        AmbientErrorContext.Provider.LogDone($"Task #{taskId} created.");
        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}
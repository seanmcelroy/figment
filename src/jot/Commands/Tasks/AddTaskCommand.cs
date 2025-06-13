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

using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Tasks;

/// <summary>
/// Adds a new task.
/// </summary>
internal partial class AddTaskCommand : TaskCommandBase<AddTaskCommandSettings>
{
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

        var (_, _, priority, status, due, taskName) = ParseBodyForValues(string.Join(' ', settings.Segments).Trim(), settings);

        if (string.IsNullOrWhiteSpace(taskName))
        {
            // Don't allow the task to be ONLY a status:value or due:value entry.
            AmbientErrorContext.Provider.LogError($"Task name was not provided.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var propertiesToAdd = new Dictionary<string, object?>();
        if (priority.Specified)
        {
            propertiesToAdd.Add("priority", priority.Value);
        }

        if (status.Specified)
        {
            propertiesToAdd.Add("status", status.Value);
        }

        if (due.Specified)
        {
            if (due.Value == DateTimeOffset.MinValue)
            {
                propertiesToAdd.Add("due", null);
            }
            else
            {
                propertiesToAdd.Add("due", due.Value);
            }
        }

        var tcr = await thingProvider.CreateAsync(
            taskSchema,
            taskName,
            propertiesToAdd,
            cancellationToken);

        if (!tcr.Success || tcr.NewThing == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to create task: {tcr.Message}");
            return (int)Globals.GLOBAL_ERROR_CODES.THING_CREATE_ERROR;
        }

        var taskIdPropValue = await tcr.NewThing.GetPropertyByTrueNameAsync(ListTasksCommand.TrueNameId, cancellationToken);
        var taskId = taskIdPropValue?.AsUInt64();

        AmbientErrorContext.Provider.LogDone($"Task #{taskId} created.");

        Program.SelectedEntity = tcr.NewThing;
        Program.SelectedEntityName = tcr.NewThing.Name;

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}
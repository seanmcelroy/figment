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
/// Lists all the things in the database.
/// </summary>
public partial class AddTaskCommand : CancellableAsyncCommand<AddTaskCommandSettings>
{
    private enum ERROR_CODES : int
    {
        THING_CREATE_ERROR = -2002,
    }

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

        var taskName = string.Join(' ', settings.Segments);

        var task = await thingProvider.CreateAsync(taskSchema, taskName, cancellationToken);
        if (task == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to create task.");
            return (int)ERROR_CODES.THING_CREATE_ERROR;
        }

        var taskIdPropValue = await task.GetPropertyByTrueNameAsync(ListTasksCommand.TrueNameId, cancellationToken);
        var taskId = taskIdPropValue?.AsUInt64();

        AmbientErrorContext.Provider.LogDone($"Task #{taskId} created.");
        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}
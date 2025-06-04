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
using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Tasks;

/// <summary>
/// Adds a new task.
/// </summary>
public partial class AddTaskCommand : CancellableAsyncCommand<AddTaskCommandSettings>
{
    private enum ERROR_CODES : int
    {
        THING_CREATE_ERROR = -2002,
    }

    [GeneratedRegex(@"\b(?:due:[^\s\b]+)")]
    internal static partial Regex DueRegex();

    [GeneratedRegex(@"(?<=^|\b)priority:(?:$|[^\s\b]+)")]
    internal static partial Regex PriorityRegex();

    [GeneratedRegex(@"(?<=^|\b)status:(?:$|[^\s\b]+)")]
    internal static partial Regex StatusRegex();

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
        bool? priority = null;
        string? status = null;
        DateTimeOffset? due = null;

        // Is due:value specified?
        var dueMatch = DueRegex().Match(taskName);
        if (dueMatch.Success)
        {
            var (dueDate, _) = ListTasksCommand.ParseFlagDateValue(dueMatch.Value[4..]);

#pragma warning disable SA1008 // Opening parenthesis should be spaced correctly
            taskName = $"{taskName[..dueMatch.Index]}{taskName[(dueMatch.Index + dueMatch.Value.Length)..]}".Trim();
#pragma warning restore SA1008 // Opening parenthesis should be spaced correctly
            due = dueDate;
        }
        else if (!string.IsNullOrWhiteSpace(settings.DueDate))
        {
            // If we could not match a due in the text (and adjust it accordingly, THEN we will respect the command option.)
            var (dueDate, _) = ListTasksCommand.ParseFlagDateValue(settings.DueDate);
            due = dueDate;
        }

        // Is priority:value specified?
        var priorityMatch = PriorityRegex().Match(taskName);
        if (priorityMatch.Success && SchemaBooleanField.TryParseBoolean(priorityMatch.Value[9..], out bool p))
        {
#pragma warning disable SA1008 // Opening parenthesis should be spaced correctly
            taskName = $"{taskName[..priorityMatch.Index]}{taskName[(priorityMatch.Index + priorityMatch.Value.Length)..]}".Trim();
#pragma warning restore SA1008 // Opening parenthesis should be spaced correctly
            priority = p;
        }
        else if (settings.Priority.HasValue)
        {
            priority = settings.Priority.Value;
        }

        // Is status:value specified?
        var statusMatch = StatusRegex().Match(taskName);
        if (statusMatch.Success)
        {
#pragma warning disable SA1008 // Opening parenthesis should be spaced correctly
            taskName = $"{taskName[..statusMatch.Index]}{taskName[(statusMatch.Index + statusMatch.Value.Length)..]}".Trim();
#pragma warning restore SA1008 // Opening parenthesis should be spaced correctly
            status = statusMatch.Value[7..];
        }
        else if (!string.IsNullOrWhiteSpace(settings.Status))
        {
            status = settings.Status;
        }

        if (string.IsNullOrWhiteSpace(taskName))
        {
            // Don't allow the task to be ONLY a status:value or due:value entry.
            AmbientErrorContext.Provider.LogError($"Task name was not provided.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var tcr = await thingProvider.CreateAsync(
            taskSchema,
            taskName,
            new Dictionary<string, object?>()
            {
                { "priority", priority },
                { "status", status },
                { "due", due },
            },
            cancellationToken);

        if (!tcr.Success || tcr.NewThing == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to create task: {tcr.Message}");
            return (int)ERROR_CODES.THING_CREATE_ERROR;
        }

        var taskIdPropValue = await tcr.NewThing.GetPropertyByTrueNameAsync(ListTasksCommand.TrueNameId, cancellationToken);
        var taskId = taskIdPropValue?.AsUInt64();

        AmbientErrorContext.Provider.LogDone($"Task #{taskId} created.");

        Program.SelectedEntity = tcr.NewThing;
        Program.SelectedEntityName = tcr.NewThing.Name;

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}
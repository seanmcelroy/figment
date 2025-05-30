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
using Figment.Common.Calculations.Functions;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Tasks;

/// <summary>
/// Lists all the things in the database.
/// </summary>
public class ListTasksCommand : CancellableAsyncCommand<ListTasksCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, ListTasksCommandSettings settings, CancellationToken cancellationToken)
    {
        var thingProvider = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (thingProvider == null)
        {
            AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_THING_STORAGE_PROVIDER);
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        Table t = new();
        t = t.HideHeaders()
            .HideRowSeparators()
            .Border(TableBorder.None)
            .AddColumn("ID", c => c.Padding(5, 0))
            .AddColumn("Done", c => c.Padding(5, 0))
            .AddColumn("Due", c => c.Padding(5, 0))
            .AddColumn("Name");

        // Collect all task references first
        var taskReferences = new List<Reference>();
        await foreach (var reference in thingProvider.GetBySchemaAsync(WellKnownSchemas.TaskGuid, cancellationToken))
        {
            taskReferences.Add(reference);
        }

        // Batch load all tasks in parallel to avoid N+1 query problem
        var taskTasks = taskReferences.Select(reference => thingProvider.LoadAsync(reference.Guid, cancellationToken));
        var taskResults = await Task.WhenAll(taskTasks);

        // Filter out null results
        var tasks = taskResults.Where(task => task != null).Cast<Thing>().ToList();

        const string trueNameId = $"{WellKnownSchemas.TaskGuid}.id";
        const string trueNameComplete = $"{WellKnownSchemas.TaskGuid}.complete";
        const string trueNameDue = $"{WellKnownSchemas.TaskGuid}.due";

        // Extract properties once and cache them with tasks to avoid duplicate lookups
        var tasksWithProps = new List<(Thing Task, Dictionary<string, ThingProperty?> Props)>();
        foreach (var task in tasks)
        {
            var props = await task.GetPropertiesByTrueNameAsync([trueNameId, trueNameComplete, trueNameDue], cancellationToken);
            tasksWithProps.Add((task, props));
        }

        // Sort using cached properties instead of calling GetPropertyByTrueName during comparison
        var nowDate = DateTime.Now.Date;
        foreach (var item in tasksWithProps.OrderBy(x => x.Props.TryGetValue(trueNameId, out var idProp) ? idProp?.AsUInt64() : null))
        {
            var task = item.Task;
            var props = item.Props;

            var id = props[trueNameId]?.AsUInt64();
            var idValue = id != null
                ? $"[yellow]{id}[/]"
                : "[red]<ID MISSING>[/]";
            var completeValue = props[trueNameComplete]?.Value is bool b ? (b ? "[[[green]x[/]]]" : "[[ ]]") : "[[ ]]";
            string? dueValue = null;
            {
                DateTime? dueDate = null;
                if (!props.TryGetValue(trueNameDue, out ThingProperty? dueProp))
                {
                    dueValue = string.Empty; // No due date
                }
                else
                {
                    if (dueProp == default)
                    {
                        dueValue = string.Empty; // No due date
                    }
                    else if (dueProp!.Value.Value is DateTimeOffset dto)
                    {
                        dueDate = dto.DateTime;
                    }
                    else if (dueProp.Value.Value is DateTime dt)
                    {
                        dueDate = dt;
                    }
                    else if (dueProp.Value.Value is string sdt && SchemaDateField.TryParseDate(sdt, out DateTimeOffset dto2))
                    {
                        dueDate = dto2.Date;
                    }
                    else
                    {
                        dueValue = $"[red]{dueProp.Value.Value}[/]"; // Unparsable due date
                    }
                }

                if (dueValue == null && dueDate != null)
                {
                    var dueValueInner = DateUtility.GetShortRelativeFutureDateDescription(dueDate.Value);
                    if (dueDate < nowDate)
                    {
                        dueValue = $"[red]{dueValueInner}[/]";
                    }
                    else if (dueDate == nowDate)
                    {
                        dueValue = $"[yellow]{dueValueInner}[/]";
                    }
                    else
                    {
                        dueValue = $"[blue]{dueValueInner}[/]";
                    }
                }
            }

            var name = task.Name ?? $"[red]NAME MISSING ({task.Guid})[/]";

            t.AddRow(
                idValue,
                completeValue,
                dueValue ?? string.Empty,
                name);
        }

        AnsiConsole.Write(t);

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}
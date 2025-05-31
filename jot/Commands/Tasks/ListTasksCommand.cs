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

using System.Text;
using System.Text.RegularExpressions;
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
public partial class ListTasksCommand : CancellableAsyncCommand<ListTasksCommandSettings>
{
    private delegate Task<bool> FilterDelegate(Thing task);
    private delegate Task<Dictionary<string, HashSet<Thing>>> GroupingDelegate(string? fieldName, HashSet<Thing> tasks, CancellationToken cancellationToken);

    [GeneratedRegex(@"(?<!\b)\@[\w\d]+\b")]
    private static partial Regex ContextRegex();

    [GeneratedRegex(@"(?<!\b)\+[\w\d]+\b")]
    private static partial Regex ProjectRegex();

    /// <summary>
    /// Converts a flag value representing a date into an inclusive range of beginning and ending dates.
    /// </summary>
    /// <param name="flagValue">The string value to parse, in the ultralist.io format.</param>
    /// <returns>An inclusive range of dates between which tasks must have a date.</returns>
    private static (DateTimeOffset, DateTimeOffset) ParseFlagDateValue(string flagValue)
    {
        // Ultralist filtering by date
        // due:(tod|today|tom|tomorrow|thisweek|nextweek|lastweek|mon|tue|wed|thu|fri|sat|sun|none|<specific date>)
        var nowDT = DateTime.Now.Date;
        var nowDO = DateOnly.FromDateTime(nowDT);
        switch (flagValue.ToLowerInvariant())
        {
            case "tod":
            case "today":
                return (nowDO.ToDateTime(TimeOnly.MinValue), nowDO.ToDateTime(TimeOnly.MaxValue));
            case "tom":
            case "tomorrow":
                return (nowDO.AddDays(1).ToDateTime(TimeOnly.MinValue), nowDO.AddDays(1).ToDateTime(TimeOnly.MaxValue));
            case "thisweek":
                {
                    int diff = (7 + (nowDO.DayOfWeek - DayOfWeek.Sunday)) % 7;
                    var startOfWeek = nowDT.AddDays(-1 * diff).Date;
                    var endOfWeek = DateOnly.FromDateTime(startOfWeek.AddDays(6)).ToDateTime(TimeOnly.MaxValue);
                    return (startOfWeek, endOfWeek);
                }

            case "nextweek":
                {
                    int diff = (7 + (nowDO.DayOfWeek - DayOfWeek.Sunday)) % 7;
                    var startOfWeek = nowDT.AddDays(7).AddDays(-1 * diff).Date;
                    var endOfWeek = DateOnly.FromDateTime(startOfWeek.AddDays(6)).ToDateTime(TimeOnly.MaxValue);
                    return (startOfWeek, endOfWeek);
                }

            case "lastweek":
                {
                    int diff = (7 + (nowDO.DayOfWeek - DayOfWeek.Sunday)) % 7;
                    var startOfWeek = nowDT.AddDays(-7).AddDays(-1 * diff).Date;
                    var endOfWeek = DateOnly.FromDateTime(startOfWeek.AddDays(6)).ToDateTime(TimeOnly.MaxValue);
                    return (startOfWeek, endOfWeek);
                }

            case "sun":
                var sundaySOD = nowDT.AddDays(((int)DayOfWeek.Sunday - (int)nowDO.DayOfWeek + 7) % 7);
                return (sundaySOD, DateOnly.FromDateTime(sundaySOD).ToDateTime(TimeOnly.MaxValue));

            case "mon":
                var mondaySOD = nowDT.AddDays(((int)DayOfWeek.Monday - (int)nowDO.DayOfWeek + 7) % 7);
                return (mondaySOD, DateOnly.FromDateTime(mondaySOD).ToDateTime(TimeOnly.MaxValue));

            case "tue":
                var tuesdaySOD = nowDT.AddDays(((int)DayOfWeek.Tuesday - (int)nowDO.DayOfWeek + 7) % 7);
                return (tuesdaySOD, DateOnly.FromDateTime(tuesdaySOD).ToDateTime(TimeOnly.MaxValue));

            case "wed":
                var wednesdaySOD = nowDT.AddDays(((int)DayOfWeek.Wednesday - (int)nowDO.DayOfWeek + 7) % 7);
                return (wednesdaySOD, DateOnly.FromDateTime(wednesdaySOD).ToDateTime(TimeOnly.MaxValue));

            case "thu":
            case "thur":
            case "thurs":
            case "thr":
                var thursdaySOD = nowDT.AddDays(((int)DayOfWeek.Thursday - (int)nowDO.DayOfWeek + 7) % 7);
                return (thursdaySOD, DateOnly.FromDateTime(thursdaySOD).ToDateTime(TimeOnly.MaxValue));

            case "fri":
                var fridaySOD = nowDT.AddDays(((int)DayOfWeek.Friday - (int)nowDO.DayOfWeek + 7) % 7);
                return (fridaySOD, DateOnly.FromDateTime(fridaySOD).ToDateTime(TimeOnly.MaxValue));

            case "sat":
                var saturdaySOD = nowDT.AddDays(((int)DayOfWeek.Saturday - (int)nowDO.DayOfWeek + 7) % 7);
                return (saturdaySOD, DateOnly.FromDateTime(saturdaySOD).ToDateTime(TimeOnly.MaxValue));

            default:
                if (SchemaDateField.TryParseDate(flagValue, out DateTimeOffset dto))
                {
                    var sod = dto.Date;
                    return (sod, DateOnly.FromDateTime(sod).ToDateTime(TimeOnly.MaxValue));
                }

                return (DateTimeOffset.MinValue, DateTimeOffset.MaxValue);
        }
    }

    private static Task<Dictionary<string, HashSet<Thing>>> BucketTasksByContext(string? fieldName, HashSet<Thing> tasks, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tasks);

        var groupedBuckets = new Dictionary<string, HashSet<Thing>>();
        foreach (var task in tasks)
        {
            var contexts = ContextRegex().Matches(task.Name)
                .Where(m => m.Success)
                .Select(m => m.Value)
                .ToArray();

            if (contexts.Length == 0)
            {
                if (!groupedBuckets.TryAdd("no context", [task]))
                {
                    if (groupedBuckets.TryGetValue("no context", out HashSet<Thing>? bucket))
                    {
                        bucket.Add(task);
                    }
                }
            }
            else
            {
                foreach (var context in contexts)
                {
                    if (!groupedBuckets.TryAdd(context, [task]))
                    {
                        if (groupedBuckets.TryGetValue(context, out HashSet<Thing>? bucket))
                        {
                            bucket.Add(task);
                        }
                    }
                }
            }
        }

        return Task.FromResult(groupedBuckets);
    }

    private static Task<Dictionary<string, HashSet<Thing>>> BucketTasksByProject(string? fieldName, HashSet<Thing> tasks, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tasks);

        var groupedBuckets = new Dictionary<string, HashSet<Thing>>();
        foreach (var task in tasks)
        {
            var projects = ProjectRegex().Matches(task.Name)
                .Where(m => m.Success)
                .Select(m => m.Value)
                .ToArray();

            if (projects.Length == 0)
            {
                if (!groupedBuckets.TryAdd("no project", [task]))
                {
                    if (groupedBuckets.TryGetValue("no project", out HashSet<Thing>? bucket))
                    {
                        bucket.Add(task);
                    }
                }
            }
            else
            {
                foreach (var project in projects)
                {
                    if (!groupedBuckets.TryAdd(project, [task]))
                    {
                        if (groupedBuckets.TryGetValue(project, out HashSet<Thing>? bucket))
                        {
                            bucket.Add(task);
                        }
                    }
                }
            }
        }

        return Task.FromResult(groupedBuckets);
    }

    private static async Task<Dictionary<string, HashSet<Thing>>> BucketTasksByProperty(string? fieldName, HashSet<Thing> tasks, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
        ArgumentNullException.ThrowIfNull(tasks);

        string trueNameGroupField = $"{WellKnownSchemas.TaskGuid}.{fieldName}";

        var groupedBuckets = new Dictionary<string, HashSet<Thing>>();
        foreach (var task in tasks)
        {
            var groupingProp = await task.GetPropertyByTrueNameAsync(trueNameGroupField, cancellationToken);
            if (!groupingProp.HasValue)
            {
                if (!groupedBuckets.TryAdd($"no {fieldName}", [task]))
                {
                    if (groupedBuckets.TryGetValue($"no {fieldName}", out HashSet<Thing>? bucket))
                    {
                        bucket.Add(task);
                    }
                }
            }
            else if (groupingProp.Value.Value == null || (groupingProp.Value.Value is string s && string.IsNullOrWhiteSpace(s)))
            {
                if (!groupedBuckets.TryAdd($"no {groupingProp.Value.SimpleDisplayName}", [task]))
                {
                    if (groupedBuckets.TryGetValue($"no {groupingProp.Value.SimpleDisplayName}", out HashSet<Thing>? bucket))
                    {
                        bucket.Add(task);
                    }
                }
            }
            else
            {
                var bucketName = groupingProp.Value.Value.ToString() ?? $"no {groupingProp.Value.SimpleDisplayName}";
                if (!groupedBuckets.TryAdd(bucketName, [task]))
                {
                    if (groupedBuckets.TryGetValue(bucketName, out HashSet<Thing>? bucket))
                    {
                        bucket.Add(task);
                    }
                }
            }
        }

        return groupedBuckets;
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, ListTasksCommandSettings settings, CancellationToken cancellationToken)
    {
        var thingProvider = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (thingProvider == null)
        {
            AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_THING_STORAGE_PROVIDER);
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        const string trueNameId = $"{WellKnownSchemas.TaskGuid}.id";
        const string trueNameComplete = $"{WellKnownSchemas.TaskGuid}.complete";
        const string trueNameDue = $"{WellKnownSchemas.TaskGuid}.due";
        const string trueNamePriority = $"{WellKnownSchemas.TaskGuid}.priority";
        const string trueNameArchived = $"{WellKnownSchemas.TaskGuid}.archived";
        const string trueNameStatus = $"{WellKnownSchemas.TaskGuid}.status";
        const string trueNameContexts = $"{WellKnownSchemas.TaskGuid}.contexts";
        const string trueNameProjects = $"{WellKnownSchemas.TaskGuid}.projects";

        // Parse filters
        List<FilterDelegate> filters = [];
        FilterDelegate? dueFilter = null;
        string? groupFieldName = null;
        GroupingDelegate bucketSorter = (fieldName, tasks, ct) =>
        {
            return Task.FromResult(new Dictionary<string, HashSet<Thing>>() { { "all", tasks } });
        };

        foreach (var flagValue in settings.Flags)
        {
            // Flag convenience replacements
            var flag = flagValue;
            if (flag.Equals("done", StringComparison.CurrentCultureIgnoreCase)
                || flag.Equals("closed", StringComparison.CurrentCultureIgnoreCase)
                || flag.Equals("complete", StringComparison.CurrentCultureIgnoreCase))
            {
                flag = "complete:true";
            }
            else if (flag.Equals("undone", StringComparison.CurrentCultureIgnoreCase)
                || flag.Equals("todo", StringComparison.CurrentCultureIgnoreCase)
                || flag.Equals("open", StringComparison.CurrentCultureIgnoreCase))
            {
                flag = "complete:false";
            }
            else if (flag.Equals("archived", StringComparison.CurrentCultureIgnoreCase))
            {
                flag = "archived:true";
            }

            if (!flag.Contains(':'))
            {
                continue;
            }

            var split = flag.Split(':');
            if (split.Length != 2)
            {
                continue;
            }

            switch (split[0].ToLowerInvariant())
            {
                case "due":
                    {
                        var (rangeStart, rangeEnd) = ParseFlagDateValue(split[1]);
                        dueFilter = async (t) =>
                        {
                            var dueProp = await t.GetPropertyByTrueNameAsync(trueNameDue, cancellationToken);
                            var dueValue = dueProp.Value.AsDateTimeOffset();

                            // Only allow if no due date and 'none' specified.  Otherwise no match if no due date.
                            if (split[1].Equals("none", StringComparison.CurrentCultureIgnoreCase))
                            {
                                return dueValue == null;
                            }
                            else if (dueValue == null)
                            {
                                return false;
                            }

                            return rangeStart <= dueValue && dueValue <= rangeEnd;
                        };
                        break;
                    }

                case "before":
                case "duebefore":
                    {
                        var (rangeStart, _) = ParseFlagDateValue(split[1]);
                        dueFilter = async (t) =>
                        {
                            var dueProp = await t.GetPropertyByTrueNameAsync(trueNameDue, cancellationToken);
                            var dueValue = dueProp.Value.AsDateTimeOffset();

                            // Only allow if no due date and 'none' specified.  Otherwise no match if no due date.
                            if (split[1].Equals("none", StringComparison.CurrentCultureIgnoreCase))
                            {
                                return dueValue == null;
                            }
                            else if (dueValue == null)
                            {
                                return false;
                            }

                            return dueValue <= rangeStart;
                        };
                        break;
                    }

                case "after":
                case "dueafter":
                    {
                        var (_, rangeEnd) = ParseFlagDateValue(split[1]);
                        dueFilter = async (t) =>
                        {
                            var dueProp = await t.GetPropertyByTrueNameAsync(trueNameDue, cancellationToken);
                            var dueValue = dueProp.Value.AsDateTimeOffset();

                            // Only allow if no due date and 'none' specified.  Otherwise no match if no due date.
                            if (split[1].Equals("none", StringComparison.CurrentCultureIgnoreCase))
                            {
                                return dueValue == null;
                            }
                            else if (dueValue == null)
                            {
                                return false;
                            }

                            return dueValue >= rangeEnd;
                        };
                        break;
                    }

                case "com":
                case "comp":
                case "complete":
                case "completed":
                case "done":
                    async Task<bool> CompletedFilter(Thing t)
                    {
                        // This can be a boolean or a date filter, like completed:true, completed:false, or completed:thisweek
                        var completeProp = await t.GetPropertyByTrueNameAsync(trueNameComplete, cancellationToken);
                        var completeValue = completeProp?.AsDateTimeOffset();
                        var specValidBoolean = SchemaBooleanField.TryParseBoolean(split[1], out bool specValue);
                        if (!specValidBoolean)
                        {
                            // Treat it like a date.
                            var (rangeStart, rangeEnd) = ParseFlagDateValue(split[1]);
                            var completeValueDate = completeProp?.AsDateTimeOffset();

                            // Only allow if no due date and 'none' specified.  Otherwise no match if no due date.
                            if (split[1].Equals("none", StringComparison.CurrentCultureIgnoreCase))
                            {
                                return completeValueDate == null;
                            }
                            else if (completeValueDate == null)
                            {
                                return false;
                            }

                            return rangeStart <= completeValueDate && completeValueDate <= rangeEnd;
                        }

                        return completeValue.HasValue == specValue;
                    }

                    filters.Add(CompletedFilter);
                    break;

                case "pr":
                case "pri":
                case "prio":
                case "priority":
                case "prioritized":
                    async Task<bool> PriorityFilter(Thing t)
                    {
                        var priorityProp = await t.GetPropertyByTrueNameAsync(trueNamePriority, cancellationToken);
                        var priorityValue = priorityProp?.AsBoolean();
                        var specValid = SchemaBooleanField.TryParseBoolean(split[1], out bool specValue);
                        if (!specValid)
                        {
                            return true; // User provided unparsable value; do not filter.
                        }

                        return (priorityValue ?? false) == specValue;
                    }

                    filters.Add(PriorityFilter);
                    break;

                case "a":
                case "ar":
                case "arc":
                case "arch":
                case "archive":
                case "archived":
                    async Task<bool> ArchivedFilter(Thing t)
                    {
                        var archivedProp = await t.GetPropertyByTrueNameAsync(trueNameArchived, cancellationToken);
                        var archivedValue = archivedProp?.AsBoolean();
                        var specValid = SchemaBooleanField.TryParseBoolean(split[1], out bool specValue);
                        if (!specValid)
                        {
                            return true; // User provided unparsable value; do not filter.
                        }

                        return (archivedValue ?? false) == specValue;
                    }

                    filters.Add(ArchivedFilter);
                    break;

                case "group":
                    {
                        string grpFieldValue = split[1];

                        // Convienences
                        if (grpFieldValue.Equals("c", StringComparison.CurrentCultureIgnoreCase))
                        {
                            // Special handling for context
                            groupFieldName = "context";
                        }
                        else if (grpFieldValue.Equals("p", StringComparison.CurrentCultureIgnoreCase))
                        {
                            // Special handling for project
                            groupFieldName = "project";
                        }
                        else if (grpFieldValue.Equals("s", StringComparison.CurrentCultureIgnoreCase))
                        {
                            groupFieldName = "status";
                        }
                        else
                        {
                            groupFieldName = grpFieldValue;
                        }

                        switch (groupFieldName.ToLowerInvariant())
                        {
                            case "context":
                                bucketSorter = BucketTasksByContext;
                                break;
                            case "project":
                                bucketSorter = BucketTasksByProject;
                                break;
                            case "status":
                            default:
                                bucketSorter = BucketTasksByProperty;
                                break;
                        }

                        break;
                    }

                default:
                    AmbientErrorContext.Provider.LogWarning($"Unsupported flag '{split[0]}'");
                    break;
            }
        }

        if (dueFilter != null)
        {
            filters.Add(dueFilter);
        }

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
        var tasks = taskResults.Where(task => task != null).Cast<Thing>().ToHashSet();

        // Bucket tasks for grouping.
        var groupedBuckets = await bucketSorter(groupFieldName, tasks, cancellationToken);

        // Recalculate context and projects dynamically.

        // Extract properties once and cache them with tasks to avoid duplicate lookups
        var tasksWithProps = new List<(Thing Task, Dictionary<string, ThingProperty?> Props, string[] projects, string[] contexts)>();
        foreach (var task in tasks)
        {
            var props = await task.GetPropertiesByTrueNameAsync([trueNameId, trueNameComplete, trueNameDue, trueNameArchived, trueNamePriority, trueNameStatus], cancellationToken);
            string[] projects = [];
            string[] contexts = [];
            tasksWithProps.Add((task, props, projects, contexts));
        }

        // Pre-evaluate filters to avoid .Result deadlocks
        var filteredTaskGuidsAndProps = new Dictionary<string, Dictionary<string, ThingProperty?>>();
        foreach (var item in tasksWithProps)
        {
            // Evaluate all filters for this task
            bool passesAllFilters = true;
            foreach (var filter in filters)
            {
                if (!await filter(item.Task))
                {
                    passesAllFilters = false;
                    break;
                }
            }

            if (passesAllFilters)
            {
                filteredTaskGuidsAndProps.TryAdd(item.Task.Guid, item.Props);
            }
        }

        var nowDate = DateTime.Now.Date;
        foreach (var (bucketName, todos) in groupedBuckets)
        {
            AnsiConsole.MarkupLineInterpolated($"{Environment.NewLine}[teal]{Markup.Escape(bucketName)}[/]");
            Table t = new();
            t = t.HideHeaders()
                .HideRowSeparators()
                .Border(TableBorder.None)
                .AddColumn("ID", c => c.Padding(2, 0))
                .AddColumn("Done", c => c.Padding(2, 0))
                .AddColumn("Due", c => c.Padding(2, 0))
                .AddColumn("Name");

            // Sort using cached properties instead of calling GetPropertyByTrueName during comparison
            foreach (var item in todos
                .Join(filteredTaskGuidsAndProps, t => t.Guid, f => f.Key, (f, t) => new { Task = f, Props = t.Value })
                .OrderBy(x => x.Props.TryGetValue(trueNameDue, out var dueProp)
                    ? dueProp?.AsDateTimeOffset()
                    : null)
                .ThenBy(x => x.Props.TryGetValue(trueNameId, out var idProp)
                    ? idProp?.AsUInt64()
                    : null))
            {
                var task = item.Task;
                var props = item.Props;

                var id = props[trueNameId]?.AsUInt64();
                var idValue = id != null
                    ? $"[darkgoldenrod]{id}[/]"
                    : "[red]<ID MISSING>[/]";

                bool complete = (props[trueNameComplete]?.AsDateTimeOffset()).HasValue;
                var completeValue = complete
                    ? (AnsiConsole.Profile.Capabilities.Unicode ? "[[[green]:check_mark:[/]]]" : "[[[green]x[/]]]")
                    : "[[ ]]";

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
                        if (complete)
                        {
                            // Don't color the due date if the task is already complete
                            dueValue = dueValueInner;
                        }
                        else if (dueDate < nowDate)
                        {
                            // This task is overdue, so color it red.
                            dueValue = $"[red]{dueValueInner}[/]";
                        }
                        else if (dueDate == nowDate)
                        {
                            // This task is due today, so color it yellow.
                            dueValue = $"[yellow]{dueValueInner}[/]";
                        }
                        else
                        {
                            // This task is not overdue and is not due today, so color it blue.
                            dueValue = $"[blue]{dueValueInner}[/]";
                        }
                    }
                }

                string renderableName;
                if (string.IsNullOrWhiteSpace(task.Name))
                {
                    renderableName = $"[red]NAME MISSING ({task.Guid})[/]";
                }
                else
                {
                    var split = task.Name.Split(" ");
                    var sb = new StringBuilder();
                    foreach (var entry in split)
                    {
                        if (entry.StartsWith('@'))
                        {
                            sb.AppendFormat($"[hotpink]{Markup.Escape(entry)}[/] ");
                        }
                        else if (entry.StartsWith('+'))
                        {
                            sb.AppendFormat($"[lightslateblue]{Markup.Escape(entry)}[/] ");
                        }
                        else
                        {
                            sb.AppendFormat($"{Markup.Escape(entry)} ");
                        }
                    }

                    renderableName = sb.ToString();
                }

                t.AddRow(
                    idValue,
                    completeValue,
                    dueValue ?? string.Empty,
                    renderableName);
            }

            AnsiConsole.Write(t);
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}
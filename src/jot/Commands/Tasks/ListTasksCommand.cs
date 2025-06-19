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
    internal static (DateTimeOffset, DateTimeOffset) ParseFlagDateValue(string flagValue)
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

            case "none":
                return (DateTimeOffset.MinValue, DateTimeOffset.MinValue);

            default:
                if (SchemaDateField.TryParseDate(flagValue, out DateTimeOffset dto))
                {
                    var sod = dto.Date;
                    return (sod, DateOnly.FromDateTime(sod).ToDateTime(TimeOnly.MaxValue));
                }

                return (DateTimeOffset.MinValue, DateTimeOffset.MaxValue);
        }
    }

    private static FilterDelegate CreateArchivedFilter(string flagValue, CancellationToken cancellationToken)
    {
        return async t =>
        {
            var archivedProp = await t.GetPropertyByTrueNameAsync(Figment.Common.Tasks.Task.TrueNameArchived, cancellationToken);
            var archivedValue = archivedProp?.AsBoolean();
            if (!SchemaBooleanField.TryParseBoolean(flagValue, out bool specValue))
            {
                return true; // User provided unparsable value; do not filter.
            }

            return (archivedValue ?? false) == specValue;
        };
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

        string trueNameGroupField = $"{Figment.Common.Tasks.Task.WellKnownSchemaGuid}.{fieldName}";

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

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
    private static Task<bool> FilterByContext(Thing task, string input, CancellationToken _)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
    {
        ArgumentNullException.ThrowIfNull(task);

        if (string.IsNullOrWhiteSpace(input))
        {
            return Task.FromResult(true); // User provided unparsable value; do not filter.
        }

        var taskContexts = ContextRegex().Matches(task.Name)
            .Where(m => m.Success)
            .Select(m => m.Value[1..]) // Ignore preceding '@'
            .ToArray();

        // This could be one context like 'now', or multiple like 'mom,-dad'
        var passing = true;
        foreach (var inputContext in input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string cv;

            // Negation.
            if (inputContext.StartsWith('-'))
            {
                if (inputContext.Length < 2)
                {
                    // If the task has no context or if this is a naked negation operator (minus with no context after it), then ignore any negation.
                    continue;
                }

                cv = inputContext[1..];
                passing &= !taskContexts.Any(tc => tc.Equals(cv, StringComparison.CurrentCultureIgnoreCase));
            }
            else
            {
                if (taskContexts.Length == 0)
                {
                    // Task does NOT have any context values, and we are filtering for any one here.
                    return Task.FromResult(false);
                }

                passing &= taskContexts.Any(tc => tc.Equals(inputContext, StringComparison.CurrentCultureIgnoreCase));
            }
        }

        return Task.FromResult(passing);
    }

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
    private static Task<bool> FilterByProject(Thing task, string input, CancellationToken _)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
    {
        ArgumentNullException.ThrowIfNull(task);

        if (string.IsNullOrWhiteSpace(input))
        {
            return Task.FromResult(true); // User provided unparsable value; do not filter.
        }

        var taskProjects = ProjectRegex().Matches(task.Name)
            .Where(m => m.Success)
            .Select(m => m.Value[1..]) // Ignore preceding '+'
            .ToArray();

        // This could be one project like 'project1', or multiple like 'project1,-project2'
        var passing = true;
        foreach (var inputProject in input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string pv;

            // Negation.
            if (inputProject.StartsWith('-'))
            {
                if (inputProject.Length < 2)
                {
                    // If the task has no project or if this is a naked negation operator (minus with no status after it), then ignore any negation.
                    continue;
                }

                pv = inputProject[1..];
                passing &= !taskProjects.Any(tc => tc.Equals(pv, StringComparison.CurrentCultureIgnoreCase));
            }
            else
            {
                if (taskProjects.Length == 0)
                {
                    // Task does NOT have any project values, and we are filtering for any one here.
                    return Task.FromResult(false);
                }

                passing &= taskProjects.Any(tc => tc.Equals(inputProject, StringComparison.CurrentCultureIgnoreCase));
            }
        }

        return Task.FromResult(passing);
    }

    private static async Task<bool> FilterByProperty(Thing task, string truePropertyName, string input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(truePropertyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(input);

        var statusProp = await task.GetPropertyByTrueNameAsync(truePropertyName, cancellationToken);
        var statusValue = statusProp?.AsString();
        if (string.IsNullOrWhiteSpace(input))
        {
            return true; // User provided unparsable value; do not filter.
        }

        // This could be one status like 'now', or multiple like 'now,-next'
        var passing = true;
        foreach (var status in input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string sv;

            // Negation.
            if (status.StartsWith('-'))
            {
                if (string.IsNullOrWhiteSpace(statusValue) || status.Length < 2)
                {
                    // If the task has no status or if this is a naked negation operator (minus with no status after it), then ignore any negation.
                    continue;
                }

                sv = status[1..];
                passing &= !statusValue.Equals(sv, StringComparison.CurrentCultureIgnoreCase);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(statusValue))
                {
                    // Task does NOT have a status value, and we are filtering for any one here.
                    return false;
                }

                passing &= statusValue.Equals(status, StringComparison.CurrentCultureIgnoreCase);
            }
        }

        return passing;
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

        // Parse filters
        List<FilterDelegate> filters = [];
        FilterDelegate? dueFilter = null;
        FilterDelegate? archiveFilter = null;
        string? groupFieldName = null;
        GroupingDelegate bucketSorter = (fieldName, tasks, ct) =>
        {
            return Task.FromResult(new Dictionary<string, HashSet<Thing>>() { { "all", tasks } });
        };

        var flags = new List<string>(settings.Flags);

        // If standardized arguments were provided, massage them into Ultralist style flags so we process them uniformally.
        if (!string.IsNullOrWhiteSpace(settings.DueDate))
        {
            flags.Add($"due:{settings.DueDate}");
        }

        if (!string.IsNullOrWhiteSpace(settings.DueBefore))
        {
            flags.Add($"duebefore:{settings.DueBefore}");
        }

        if (!string.IsNullOrWhiteSpace(settings.DueAfter))
        {
            flags.Add($"dueafter:{settings.DueAfter}");
        }

        if (settings.Completed != null)
        {
            flags.Add($"completed:{settings.Completed}");
        }

        if (settings.Priority != null)
        {
            flags.Add($"priority:{settings.Priority}");
        }

        if (settings.Archived != null)
        {
            flags.Add($"archived:{settings.Archived}");
        }

        if (settings.Context != null && settings.Context.Length > 0)
        {
            foreach (var ctx in settings.Context)
            {
                flags.Add($"context:{ctx}");
            }
        }

        if (settings.Project != null && settings.Project.Length > 0)
        {
            foreach (var proj in settings.Project)
            {
                flags.Add($"project:{proj}");
            }
        }

        if (settings.Status != null && settings.Status.Length > 0)
        {
            foreach (var stat in settings.Status)
            {
                flags.Add($"status:{stat}");
            }
        }

        if (!string.IsNullOrWhiteSpace(settings.GroupBy))
        {
            flags.Add($"group:{settings.GroupBy}");
        }

        foreach (var flagValue in flags)
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
                            var dueProp = await t.GetPropertyByTrueNameAsync(Figment.Common.Tasks.Task.TrueNameDue, cancellationToken);
                            var dueValue = dueProp?.AsDateTimeOffset();

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
                            var dueProp = await t.GetPropertyByTrueNameAsync(Figment.Common.Tasks.Task.TrueNameDue, cancellationToken);
                            var dueValue = dueProp?.AsDateTimeOffset();

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
                            var dueProp = await t.GetPropertyByTrueNameAsync(Figment.Common.Tasks.Task.TrueNameDue, cancellationToken);
                            var dueValue = dueProp?.AsDateTimeOffset();

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
                        var completeProp = await t.GetPropertyByTrueNameAsync(Figment.Common.Tasks.Task.TrueNameComplete, cancellationToken);
                        var completeValue = completeProp?.AsDateTimeOffset();
                        if (!SchemaBooleanField.TryParseBoolean(split[1], out bool specValue))
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
                        var priorityProp = await t.GetPropertyByTrueNameAsync(Figment.Common.Tasks.Task.TrueNamePriority, cancellationToken);
                        var priorityValue = priorityProp?.AsBoolean();
                        if (!SchemaBooleanField.TryParseBoolean(split[1], out bool specValue))
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
                    archiveFilter = CreateArchivedFilter(split[1], cancellationToken);
                    break;

                case "context":
                    filters.Add((t) => FilterByContext(t, split[1], cancellationToken));
                    break;

                case "project":
                    filters.Add((t) => FilterByProject(t, split[1], cancellationToken));
                    break;

                case "status":
                    filters.Add((t) => FilterByProperty(t, Figment.Common.Tasks.Task.TrueNameStatus, split[1], cancellationToken));
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

                        bucketSorter = groupFieldName.ToLowerInvariant() switch
                        {
                            "context" => BucketTasksByContext,
                            "project" => BucketTasksByProject,
                            _ => BucketTasksByProperty,
                        };
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

        // Implicitly show only archived:false tasks if no override.
        archiveFilter ??= CreateArchivedFilter("false", cancellationToken);
        filters.Add(archiveFilter);

        // Collect all task references first
        var taskReferences = new HashSet<Reference>();
        await foreach (var reference in thingProvider.GetBySchemaAsync(Figment.Common.Tasks.Task.WellKnownSchemaGuid, cancellationToken))
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
            var props = await task.GetPropertiesByTrueNameAsync(
            [
                Figment.Common.Tasks.Task.TrueNameId,
                Figment.Common.Tasks.Task.TrueNameComplete,
                Figment.Common.Tasks.Task.TrueNameDue,
                Figment.Common.Tasks.Task.TrueNameArchived,
                Figment.Common.Tasks.Task.TrueNamePriority,
                Figment.Common.Tasks.Task.TrueNameStatus,
                Figment.Common.Tasks.Task.TrueNameNotes,
            ], cancellationToken);
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
                .OrderBy(x => x.Props.TryGetValue(Figment.Common.Tasks.Task.TrueNamePriority, out var duePrio)
                    ? ((duePrio?.AsBoolean() ?? false) ? 0 : 1)
                    : 1)
                .ThenBy(x => x.Props.TryGetValue(Figment.Common.Tasks.Task.TrueNameDue, out var dueProp)
                    ? dueProp?.AsDateTimeOffset()
                    : null)
                .ThenBy(x => x.Props.TryGetValue(Figment.Common.Tasks.Task.TrueNameId, out var idProp)
                    ? idProp?.AsUInt64()
                    : null))
            {
                var task = item.Task;
                var props = item.Props;

                bool prioritized = props[Figment.Common.Tasks.Task.TrueNamePriority]?.AsBoolean() ?? false;
                var beBold = prioritized ? " bold" : string.Empty;

                var id = props[Figment.Common.Tasks.Task.TrueNameId]?.AsUInt64();
                var idValue = id != null
                    ? $"[darkgoldenrod{beBold}]{id}[/]"
                    : $"[red{beBold}]<ID MISSING>[/]";

                bool complete = (props[Figment.Common.Tasks.Task.TrueNameComplete]?.AsDateTimeOffset()).HasValue;
                var completeValue = complete
                    ? (AnsiConsole.Profile.Capabilities.Unicode ? $"[[[green{beBold}]:check_mark:[/]]]" : "[[[green]x[/]]]")
                    : "[[ ]]";

                string? dueValue = null;
                {
                    DateTime? dueDate = null;
                    if (!props.TryGetValue(Figment.Common.Tasks.Task.TrueNameDue, out ThingProperty? dueProp))
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
                            dueValue = $"[red{beBold}]{dueProp.Value.Value}[/]"; // Unparsable due date
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
                            dueValue = $"[red{beBold}]{dueValueInner}[/]";
                        }
                        else if (dueDate == nowDate)
                        {
                            // This task is due today, so color it yellow.
                            dueValue = $"[yellow{beBold}]{dueValueInner}[/]";
                        }
                        else
                        {
                            // This task is not overdue and is not due today, so color it blue.
                            dueValue = $"[blue{beBold}]{dueValueInner}[/]";
                        }
                    }
                }

                string renderableName;
                if (string.IsNullOrWhiteSpace(task.Name))
                {
                    renderableName = $"[red{beBold}]NAME MISSING ({task.Guid})[/]";
                }
                else
                {
                    var split = task.Name.Split(" ");
                    var sb = new StringBuilder();
                    foreach (var entry in split)
                    {
                        if (entry.StartsWith('@'))
                        {
                            sb.AppendFormat($"[hotpink{beBold}]{Markup.Escape(entry)}[/] ");
                        }
                        else if (entry.StartsWith('+'))
                        {
                            sb.AppendFormat($"[lightslateblue{beBold}]{Markup.Escape(entry)}[/] ");
                        }
                        else if (prioritized)
                        {
                            sb.AppendFormat($"[bold]{Markup.Escape(entry)}[/] ");
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

                // Does this task have notes?
                if (settings.ShowNotes ?? false)
                {
                    var notes = props[Figment.Common.Tasks.Task.TrueNameNotes]?.AsStringArray();
                    if (notes != null)
                    {
                        for (var noteIndex = 0; noteIndex < notes.Length; noteIndex++)
                        {
                            t.AddRow(
                                $"  [teal]{noteIndex}[/]",
                                string.Empty,
                                string.Empty,
                                $"  {notes[noteIndex]}");
                        }
                    }
                }
            }

            // Only print the table header and the table if the table has any rows.
            if (t.Rows.Count > 0)
            {
                AnsiConsole.MarkupLineInterpolated($"{Environment.NewLine}[teal]{Markup.Escape(bucketName)}[/]");
                AnsiConsole.Write(t);
            }
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}
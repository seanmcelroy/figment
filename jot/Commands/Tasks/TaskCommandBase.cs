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
using Spectre.Console.Cli;

namespace jot.Commands.Tasks;

/// <summary>
/// A base class for <see cref="AddTaskCommand"/> and <see cref="EditTaskCommand"/>
/// that provides shared functionality for parsing task bodies.
/// </summary>
/// <typeparam name="TSettings">The settings for the command when executed.</typeparam>
internal abstract partial class TaskCommandBase<TSettings> : CancellableAsyncCommand<TSettings>
    where TSettings : CommandSettings, ITaskCommandSettings
{
    [GeneratedRegex(@"\b(?:archived:[^\s\b]+)")]
    private static partial Regex ArchivedRegex();

    [GeneratedRegex(@"\b(?:completed:[^\s\b]+)")]
    private static partial Regex CompletedRegex();

    [GeneratedRegex(@"\b(?:due:[^\s\b]+)")]
    private static partial Regex DueRegex();

    [GeneratedRegex(@"(?<=^|\b)priority:(?:$|[^\s\b]+)")]
    private static partial Regex PriorityRegex();

    [GeneratedRegex(@"(?<=^|\b)status:(?:$|[^\s\b]+)")]
    private static partial Regex StatusRegex();

    /// <summary>
    /// Parses Ultralist-style tags like due:tom, status:next, or priority:true
    /// from the <paramref name="body"/> (task name), and returns those parsed
    /// values as well as a modified version of the body that does not contain
    /// those tags.
    /// </summary>
    /// <param name="body">Original task name.</param>
    /// <param name="settings">The settings passed into the command which contains
    /// potential values in command arguments.  These will be used if a corresponding
    /// tag is not present in the body.</param>
    /// <returns>The parsed values, if available, from the body.</returns>
    protected static (
        bool? archived,
        bool? completed,
        bool? priority,
        string? status,
        DateTimeOffset? due,
        string revisedBody)
        ParseBodyForValues(string body, TSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(body);

        bool? archived = null;
        bool? completed = null;
        bool? priority = null;
        string? status = null;
        DateTimeOffset? due = null;
        string revisedBody = body;

        // Is archived:value specified?
        var archivedMatch = ArchivedRegex().Match(revisedBody);
        if (archivedMatch.Success && SchemaBooleanField.TryParseBoolean(archivedMatch.Value[9..], out bool a))
        {
#pragma warning disable SA1008 // Opening parenthesis should be spaced correctly
            revisedBody = $"{revisedBody[..archivedMatch.Index]}{revisedBody[(archivedMatch.Index + archivedMatch.Value.Length)..]}".Trim();
#pragma warning restore SA1008 // Opening parenthesis should be spaced correctly
            archived = a;
        }
        else if (settings.Archived.HasValue)
        {
            archived = settings.Archived.Value;
        }

        // Is completed:value specified?
        var completedMatch = CompletedRegex().Match(revisedBody);
        if (completedMatch.Success && SchemaBooleanField.TryParseBoolean(completedMatch.Value[10..], out bool c))
        {
#pragma warning disable SA1008 // Opening parenthesis should be spaced correctly
            revisedBody = $"{revisedBody[..completedMatch.Index]}{revisedBody[(completedMatch.Index + completedMatch.Value.Length)..]}".Trim();
#pragma warning restore SA1008 // Opening parenthesis should be spaced correctly
            completed = c;
        }
        else if (settings.Completed.HasValue)
        {
            completed = settings.Completed.Value;
        }

        // Is due:value specified?
        var dueMatch = DueRegex().Match(revisedBody);
        if (dueMatch.Success)
        {
            var (dueDate, _) = ListTasksCommand.ParseFlagDateValue(dueMatch.Value[4..]);

#pragma warning disable SA1008 // Opening parenthesis should be spaced correctly
            revisedBody = $"{revisedBody[..dueMatch.Index]}{revisedBody[(dueMatch.Index + dueMatch.Value.Length)..]}".Trim();
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
        var priorityMatch = PriorityRegex().Match(revisedBody);
        if (priorityMatch.Success && SchemaBooleanField.TryParseBoolean(priorityMatch.Value[9..], out bool p))
        {
#pragma warning disable SA1008 // Opening parenthesis should be spaced correctly
            revisedBody = $"{revisedBody[..priorityMatch.Index]}{revisedBody[(priorityMatch.Index + priorityMatch.Value.Length)..]}".Trim();
#pragma warning restore SA1008 // Opening parenthesis should be spaced correctly
            priority = p;
        }
        else if (settings.Priority.HasValue)
        {
            priority = settings.Priority.Value;
        }

        // Is status:value specified?
        var statusMatch = StatusRegex().Match(revisedBody);
        if (statusMatch.Success)
        {
#pragma warning disable SA1008 // Opening parenthesis should be spaced correctly
            revisedBody = $"{revisedBody[..statusMatch.Index]}{revisedBody[(statusMatch.Index + statusMatch.Value.Length)..]}".Trim();
#pragma warning restore SA1008 // Opening parenthesis should be spaced correctly
            status = statusMatch.Value[7..];
        }
        else if (!string.IsNullOrWhiteSpace(settings.Status))
        {
            status = settings.Status;
        }

        return (archived, completed, priority, status, due, revisedBody);
    }
}
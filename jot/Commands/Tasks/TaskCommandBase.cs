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
        NullableOrMissing<bool> archived,
        NullableOrMissing<bool> completed,
        NullableOrMissing<bool> priority,
        NullableOrMissing<string> status,
        NullableOrMissing<DateTimeOffset> due,
        string revisedBody)
        ParseBodyForValues(string body, TSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(body);

        NullableOrMissing<bool> archived = default;
        NullableOrMissing<bool> completed = default;
        NullableOrMissing<bool> priority = default;
        NullableOrMissing<string> status = default;
        NullableOrMissing<DateTimeOffset> due = default;
        string revisedBody = body;

        // Is archived:value specified?
        var archivedMatch = ArchivedRegex().Match(revisedBody);
        if (archivedMatch.Success && SchemaBooleanField.TryParseBoolean(archivedMatch.Value[9..], out bool a))
        {
#pragma warning disable SA1008 // Opening parenthesis should be spaced correctly
            revisedBody = $"{revisedBody[..archivedMatch.Index]}{revisedBody[(archivedMatch.Index + archivedMatch.Value.Length)..]}".Trim();
#pragma warning restore SA1008 // Opening parenthesis should be spaced correctly
            archived = NullableOrMissing<bool>.CreateWithStruct(a);
        }
        else if (settings.Archived.HasValue)
        {
            archived = NullableOrMissing<bool>.CreateWithStruct(settings.Archived.Value);
        }

        // Is completed:value specified?
        var completedMatch = CompletedRegex().Match(revisedBody);
        if (completedMatch.Success && SchemaBooleanField.TryParseBoolean(completedMatch.Value[10..], out bool c))
        {
#pragma warning disable SA1008 // Opening parenthesis should be spaced correctly
            revisedBody = $"{revisedBody[..completedMatch.Index]}{revisedBody[(completedMatch.Index + completedMatch.Value.Length)..]}".Trim();
#pragma warning restore SA1008 // Opening parenthesis should be spaced correctly
            completed = NullableOrMissing<bool>.CreateWithStruct(c);
        }
        else if (settings.Completed.HasValue)
        {
            completed = NullableOrMissing<bool>.CreateWithStruct(settings.Completed.Value);
        }

        // Is due:value specified?
        var dueMatch = DueRegex().Match(revisedBody);
        if (dueMatch.Success)
        {
            var (dueDate, _) = ListTasksCommand.ParseFlagDateValue(dueMatch.Value[4..]);

#pragma warning disable SA1008 // Opening parenthesis should be spaced correctly
            revisedBody = $"{revisedBody[..dueMatch.Index]}{revisedBody[(dueMatch.Index + dueMatch.Value.Length)..]}".Trim();
#pragma warning restore SA1008 // Opening parenthesis should be spaced correctly
            due = NullableOrMissing<DateTimeOffset>.CreateWithStruct(dueDate);
        }
        else if (!string.IsNullOrWhiteSpace(settings.DueDate))
        {
            // If we could not match a due in the text (and adjust it accordingly, THEN we will respect the command option.)
            var (dueDate, _) = ListTasksCommand.ParseFlagDateValue(settings.DueDate);
            due = NullableOrMissing<DateTimeOffset>.CreateWithStruct(dueDate);
        }

        // Is priority:value specified?
        var priorityMatch = PriorityRegex().Match(revisedBody);
        if (priorityMatch.Success && SchemaBooleanField.TryParseBoolean(priorityMatch.Value[9..], out bool p))
        {
#pragma warning disable SA1008 // Opening parenthesis should be spaced correctly
            revisedBody = $"{revisedBody[..priorityMatch.Index]}{revisedBody[(priorityMatch.Index + priorityMatch.Value.Length)..]}".Trim();
#pragma warning restore SA1008 // Opening parenthesis should be spaced correctly
            priority = NullableOrMissing<bool>.CreateWithStruct(p);
        }
        else if (settings.Priority.HasValue)
        {
            priority = NullableOrMissing<bool>.CreateWithStruct(settings.Priority.Value);
        }

        // Is status:value specified?
        var statusMatch = StatusRegex().Match(revisedBody);
        if (statusMatch.Success)
        {
#pragma warning disable SA1008 // Opening parenthesis should be spaced correctly
            revisedBody = $"{revisedBody[..statusMatch.Index]}{revisedBody[(statusMatch.Index + statusMatch.Value.Length)..]}".Trim();
#pragma warning restore SA1008 // Opening parenthesis should be spaced correctly
            if ("none".Equals(statusMatch.Value[7..].Trim()))
            {
                status = NullableOrMissing<string>.CreateWithClass(default(string?));
            }
            else
            {
                status = NullableOrMissing<string>.CreateWithClass(statusMatch.Value[7..]);
            }
        }
        else if (!string.IsNullOrWhiteSpace(settings.Status))
        {
            status = NullableOrMissing<string>.CreateWithClass(settings.Status);
        }

        return (archived, completed, priority, status, due, revisedBody);
    }
}
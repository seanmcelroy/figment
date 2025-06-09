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

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Tasks;

/// <summary>
/// The settings supplied to the <see cref="AddTaskCommand"/>.
/// </summary>
internal class AddTaskCommandSettings : CommandSettings, ITaskCommandSettings
{
    /// <summary>
    /// Gets the body (name) of the task.
    /// </summary>
    [CommandArgument(0, "<BODY>")]
    [Description("The name (body text) of the task, using the Ultralist style, which allows a 'due', 'status', and/or 'priority' hints to be provided inline, such as 'due:tomorrow' as last characters.  Projects will be inferred by single words with a '+' prefix, like +projectName.  Contexts will be inferred by single words with a '@' prefix, like @sean.")]
    public string[] Segments { get; init; } = [];

    /// <summary>
    /// Gets a value that only shows tasks with the specified due date.  This can be a specific date or a relative or special term, such as: tod today tom tomorrow thisweek nextweek lastweek mon tue wed thu fri sat sun none.
    /// </summary>
    [CommandOption("-d|--due <DUEDATE>")]
    [Description("Creates the specified due date.  This can be a specific date or a relative or special term, such as: tod today tom tomorrow thisweek nextweek lastweek mon tue wed thu fri sat sun none.  If this option is not specified, a due date can be specified with the 'due:value' in the body text.")]
    public string? DueDate { get; init; } = null;

    /// <summary>
    /// Gets a value that indicates the task is a priority.
    /// </summary>
    [Description("Creates the task as a priority.  If this option is not specified, a priority can be specified with the 'priority:value' in the body text, where value is true or false.")]
    [CommandOption("-p|--priority")]
    public bool? Priority { get; init; } = null;

    /// <summary>
    /// Gets a value that indicates the status of the task.
    /// </summary>
    [Description("Creates the task with the specified status.  If this option is not specified, a status can be specified with the 'status:value' in the body text.")]
    [CommandOption("-s|--status <STATUS>")]
    public string? Status { get; init; } = null;

    /// <summary>
    /// Gets a value that indicates the task is completed.
    /// </summary>
    /// <remarks>
    /// This is marked as a hidden command option, as it only exists here to satisfy <see cref="ITaskCommandSettings"/> for edit support with a shared <see cref="TaskCommandBase{TSettings}"/>.
    /// </remarks>
    [Description("Specifies the task is completed.  If this option is not specified, completed can be specified with the 'completed:value' in the body text, where value is true or false.")]
    [CommandOption("-c|--completed", IsHidden = true)]
    public bool? Completed { get; init; } = null;

    /// <summary>
    /// Gets a value that indicates the task is archived.
    /// </summary>
    /// <remarks>
    /// This is marked as a hidden command option, as it only exists here to satisfy <see cref="ITaskCommandSettings"/> for edit support with a shared <see cref="TaskCommandBase{TSettings}"/>.
    /// </remarks>
    [Description("Creates the task is archived.  If this option is not specified, archived can be specified with the 'archived:value' in the body text, where value is true or false.")]
    [CommandOption("-a|--archived", IsHidden = true)]
    public bool? Archived { get; init; } = null;

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        return Segments == null || Segments.Length == 0
            ? ValidationResult.Error("The task name (body text) must be provided to add it.")
            : ValidationResult.Success();
    }
}
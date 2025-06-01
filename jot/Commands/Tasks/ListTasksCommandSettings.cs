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
using Spectre.Console.Cli;

namespace jot.Commands.Tasks;

/// <summary>
/// The settings supplied to the <see cref="ListTasksCommand"/>.
/// </summary>
public class ListTasksCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets or sets a value that only shows tasks with the specified due date.  This can be a specific date or a relative or special term, such as: tod today tom tomorrow thisweek nextweek lastweek mon tue wed thu fri sat sun none
    /// </summary>
    [CommandOption("-d|--due <DUEDATE>")]
    [Description("Only shows tasks with the specified due date.  This can be a specific date or a relative or special term, such as: tod today tom tomorrow thisweek nextweek lastweek mon tue wed thu fri sat sun none")]
    public string? DueDate { get; set; }

    /// <summary>
    /// Gets or sets a value that only shows tasks with a due date before the specified value.  This can be any keyword that --due accepts.
    /// </summary>
    [CommandOption("-b|--due-before <DUEDATE>")]
    [Description("Only shows tasks with a due date before the specified value.  This can be any keyword that --due accepts.")]
    public string? DueBefore { get; set; }

    /// <summary>
    /// Gets or sets a value that only shows tasks with a due date after the specified value.  This can be any keyword that --due accepts.
    /// </summary>
    [CommandOption("-a|--due-after <DUEDATE>")]
    [Description("Only shows tasks with a due date after the specified value.  This can be any keyword that --due accepts.")]
    public string? DueAfter { get; set; }

    /// <summary>
    /// Gets or sets the grouping property (context, project, or status are valid values).  This is a standard alternative to [FLAGS] values like group:context.
    /// </summary>
    [CommandOption("-g|--group|--group-by <PROPERTY>")]
    [Description("Groups task listing by property (context, project, or status are valid values).  This is a standard alternative to [[FLAGS]] values like group:context.")]
    public string? GroupBy { get; set; }

    /// <summary>
    /// Gets a value that only shows tasks that have a completed attribute as specified with this filter.
    /// </summary>
    [Description("Only shows tasks that have a completed attribute as specified with this filter.")]
    [CommandOption("-c|--completed")]
    public bool? Completed { get; init; } = null;

    /// <summary>
    /// Gets a value that only shows tasks that have are priorities.
    /// </summary>
    [Description("Only shows tasks that have are priorities.")]
    [CommandOption("-p|--priority")]
    public bool? Priority { get; init; } = null;

    /// <summary>
    /// Gets a value that only shows tasks that have are archived.
    /// </summary>
    [Description("Only shows tasks that have are archived.")]
    [CommandOption("--archived")]
    public bool? Archived { get; init; } = null;

    /// <summary>
    /// Gets a value that have a specified context.
    /// </summary>
    [Description("Only shows tasks that have a specified context.")]
    [CommandOption("--context <CONTEXTS>")]
    public string[] Context { get; init; } = [];

    /// <summary>
    /// Gets a value that have a specified project.
    /// </summary>
    [Description("Only shows tasks that have a specified project.")]
    [CommandOption("--project <PROJECTS>")]
    public string[] Project { get; init; } = [];

    /// <summary>
    /// Gets a value that have a specified status.
    /// </summary>
    [Description("Only shows tasks that have a specified status.")]
    [CommandOption("--status <STATUSES>")]
    public string[] Status { get; init; } = [];

    /// <summary>
    /// Gets or sets the options used to filter <see cref="Task"/>s or to group them.
    /// This supports the Ultralist style flags.
    /// </summary>
    [CommandArgument(1, "[FLAGS]")]
    [Description("Options used to filter or group tasks using the Ultralist style flags.  This is an alternative way to configure this command from the standard options.")]
    public string[] Flags { get; set; } = [];

    /// <summary>
    /// Gets a value indicating whether to output the notes on a task in the listing.
    /// </summary>
    [Description("Outputs the notes on a task in the listing")]
    [CommandOption("-n|--notes")]
    public bool? ShowNotes { get; init; } = false;
}
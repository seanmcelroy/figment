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

namespace jot.Commands.Tasks;

/// <summary>
/// The settings supplied to the <see cref="AddTaskCommand"/>.
/// </summary>
internal interface ITaskCommandSettings
{
    /// <summary>
    /// Gets a value that only shows tasks with the specified due date.  This can be a specific date or a relative or special term, such as: tod today tom tomorrow thisweek nextweek lastweek mon tue wed thu fri sat sun none.
    /// </summary>
    public string? DueDate { get; init; }

    /// <summary>
    /// Gets a value that indicates the task is a priority.
    /// </summary>
    public bool? Priority { get; init; }

    /// <summary>
    /// Gets a value that indicates the status of the task.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Gets a value that indicates the task is completed.
    /// </summary>
    public bool? Completed { get; init; }

    /// <summary>
    /// Gets a value that indicates the task is archived.
    /// </summary>
    public bool? Archived { get; init; }
}
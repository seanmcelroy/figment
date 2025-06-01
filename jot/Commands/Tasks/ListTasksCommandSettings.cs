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
    /// Gets or sets the options used to filter <see cref="Task"/>s or to group them.
    /// </summary>
    [CommandArgument(0, "[FLAGS]")]
    [Description("Options used to filter or group tasks")]
    public string[] Flags { get; set; } = [];

    /// <summary>
    /// Gets a value indicating whether to output the notes on a task in the listing.
    /// </summary>
    [Description("Outputs the notes on a task in the listing")]
    [CommandOption("--notes")]
    public bool? ShowNotes { get; init; } = false;
}
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
/// The settings supplied to the <see cref="UnarchiveTaskCommand"/>.
/// </summary>
public class UnarchiveTaskCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets the task number to unarchive.
    /// </summary>
    [CommandArgument(0, "<ID>")]
    [Description("The task number to unarchive.")]
    public int TaskNumber { get; init; }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        return TaskNumber < 1
            ? ValidationResult.Error("The task number must be greater than 1.")
            : ValidationResult.Success();
    }
}
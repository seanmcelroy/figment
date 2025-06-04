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
/// The settings supplied to the <see cref="AddNoteCommand"/>.
/// </summary>
internal class AddNoteCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets the task number to which to add the note.
    /// </summary>
    [CommandArgument(0, "<ID>")]
    [Description("The task number to which to add the note.")]
    public int TaskNumber { get; init; }

    /// <summary>
    /// Gets the text for the note.
    /// </summary>
    [CommandArgument(1, "<CONTENT>")]
    [Description("The text for the note.")]
    public string[] Segments { get; init; } = [];

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        if (TaskNumber < 1)
        {
            return ValidationResult.Error("The task number must be greater than zero.");
        }

        return Segments == null || Segments.Length == 0 || string.IsNullOrWhiteSpace(string.Join(' ', Segments))
            ? ValidationResult.Error("The note text must be provided to add it.")
            : ValidationResult.Success();
    }
}
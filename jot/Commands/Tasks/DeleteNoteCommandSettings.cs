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
/// The settings supplied to the <see cref="DeleteNoteCommand"/>.
/// </summary>
internal class DeleteNoteCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets the task number from which to delete the note.
    /// </summary>
    [CommandArgument(0, "<TASK_ID>")]
    [Description("The task number from which to delete the note.")]
    public int TaskNumber { get; init; }

    /// <summary>
    /// Gets the note number to delete.
    /// </summary>
    [CommandArgument(1, "<NOTE_ID>")]
    [Description("The note number to delete.")]
    public int NoteNumber { get; init; }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        if (TaskNumber < 1)
        {
            return ValidationResult.Error("The task number must be greater than zero.");
        }

        if (NoteNumber < 0)
        {
            return ValidationResult.Error("The note number must be greater than or equal to zero.");
        }

        return ValidationResult.Success();
    }
}
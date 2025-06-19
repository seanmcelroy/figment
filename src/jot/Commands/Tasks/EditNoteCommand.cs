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

using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Tasks;

/// <summary>
/// Edits an existing note on a task.
/// </summary>
internal class EditNoteCommand : NoteCommandBase<EditNoteCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, EditNoteCommandSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var tsp = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (tsp == null)
        {
            AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_THING_STORAGE_PROVIDER);
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var noteText = string.Join(' ', settings.Segments).Trim();

        if (string.IsNullOrWhiteSpace(noteText))
        {
            AmbientErrorContext.Provider.LogError($"Note text missing.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var anyFound = false;

        await foreach (var task in tsp.FindBySchemaAndPropertyValue(
            Figment.Common.Tasks.Task.WellKnownSchemaGuid,
            Figment.Common.Tasks.Task.TrueNameId,
            settings.TaskNumber,
            UnsignedNumberComparer.Default,
            cancellationToken))
        {
            anyFound = true;

            var notesList = await GetNotesAsync(task, cancellationToken);
            if (notesList == null)
            {
                return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
            }

            if (notesList.Count == 0)
            {
                AmbientErrorContext.Provider.LogError($"There are no notes on Task #{settings.TaskNumber}.  Use 'addnote' to add a new note.");
                return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
            }
            else if (settings.NoteNumber >= notesList.Count)
            {
                AmbientErrorContext.Provider.LogError($"There is no Note #{settings.NoteNumber} on Task #{settings.TaskNumber}.");
                return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
            }

            notesList[settings.NoteNumber] = noteText;
            var tsr = await task.Set("notes", notesList.ToArray(), cancellationToken);
            if (!tsr.Success)
            {
                var errorMessage = tsr.Messages == null || tsr.Messages.Length == 0 ? "No error message provided." : string.Join("; ", tsr.Messages);
                AmbientErrorContext.Provider.LogError($"Unable to edit Note #{settings.NoteNumber} on Task #{settings.TaskNumber}: {errorMessage}");
                return (int)Globals.GLOBAL_ERROR_CODES.THING_SAVE_ERROR;
            }

            var (success, message) = await task.SaveAsync(cancellationToken);
            if (!success)
            {
                AmbientErrorContext.Provider.LogError($"Unable to save Task #{settings.TaskNumber}: {message}");
                return (int)Globals.GLOBAL_ERROR_CODES.THING_SAVE_ERROR;
            }

            AmbientErrorContext.Provider.LogDone($"Note edited.");
            break; // Only one can match.
        }

        if (!anyFound)
        {
            AmbientErrorContext.Provider.LogError($"Unable to find Task #{settings.TaskNumber}");
            return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}
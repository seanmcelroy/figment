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
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Tasks;

/// <summary>
/// A base class for <see cref="AddNoteCommand"/> and <see cref="EditNoteCommand"/>
/// that provides shared functionality for parsing task notes.
/// </summary>
/// <typeparam name="TSettings">The settings for the command when executed.</typeparam>
internal abstract class NoteCommandBase<TSettings> : CancellableAsyncCommand<TSettings>
    where TSettings : CommandSettings
{
    /// <summary>
    /// Gets the notes attribute on the task.
    /// </summary>
    /// <param name="task">The task thing from which to retrieve the notes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The notes in a string array, unless notes exist and could not be loaded as a string array.</returns>
    protected async Task<List<string>?> GetNotesAsync(Thing task, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(task);

        var notes = await task.GetPropertyByTrueNameAsync(ListTasksCommand.TrueNameNotes, cancellationToken);
        if (notes == null)
        {
            return [];
        }
        else if (!SchemaArrayField.SCHEMA_FIELD_TYPE.Equals(notes.Value.SchemaFieldType, StringComparison.OrdinalIgnoreCase))
        {
            AmbientErrorContext.Provider.LogError($"Field type on 'notes' property is '{notes.Value.SchemaFieldType}', but expected '{SchemaArrayField.SCHEMA_FIELD_TYPE}'.");
            return null;
        }

        string?[] arr;
        if (notes.Value.Value is string[] sa)
        {
            arr = sa;
        }
        else if (notes.Value.Value is object[] oa)
        {
            arr = [.. oa.Select(o => o?.ToString())];
        }
        else
        {
            AmbientErrorContext.Provider.LogError($"Value of 'notes' property is not an array as expected, but '{notes.Value.Value?.GetType().Name ?? "(NULL)"}.");
            return null;
        }

        return new List<string>([.. arr.Where(a => a != null).Cast<string>()]);
    }
}
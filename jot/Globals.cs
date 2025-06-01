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

using System.Text;

namespace jot;

/// <summary>
/// Global static variables and functions that support jot.
/// </summary>
public static class Globals
{
    /// <summary>
    /// Error codes that can be returned from the entry point.
    /// </summary>
    public enum GLOBAL_ERROR_CODES : int
    {
        /// <summary>
        /// The operation completed successfully or the program is terminating normally.
        /// </summary>
        SUCCESS = 0,

        /// <summary>
        /// One or more arguments were invalid for the specified operation.
        /// </summary>
        ARGUMENT_ERROR = -1,

        /// <summary>
        /// One or more objects could not be located by name or by reference.
        /// </summary>
        NOT_FOUND = -2,

        /// <summary>
        /// More than one objects were located by name or by reference, but only one was expected.
        /// </summary>
        AMBIGUOUS_MATCH = -3,

        /// <summary>
        /// Because the type of the object is not known, the operation cannot proceed.
        /// </summary>
        UNKNOWN_TYPE = -4,

        /// <summary>
        /// General input/output error, such as files or network connections could not be read or written to.
        /// </summary>
        GENERAL_IO_ERROR = -5,

        /// <summary>
        /// Unable to load a <see cref="Figment.Common.Schema"/> from the underlying data store.
        /// </summary>
        SCHEMA_LOAD_ERROR = -1000,

        /// <summary>
        /// Unable to save a <see cref="Figment.Common.Schema"/> to the underlying data store.
        /// </summary>
        SCHEMA_SAVE_ERROR = -1001,

        // SCHEMA_CREATE_ERROR = -1002
        // SCHEMA_DELETE_ERROR = -1003

        /// <summary>
        /// Unable to load a <see cref="Figment.Common.Thing"/> from the underlying data store.
        /// </summary>
        THING_LOAD_ERROR = -2000,

        /// <summary>
        /// Unable to save a <see cref="Figment.Common.Thing"/> to the underlying data store.
        /// </summary>
        THING_SAVE_ERROR = -2001,

        // THING_CREATE_ERROR = -2002
        // THING_DELETE_ERROR = -2003,
    }

    /// <summary>
    /// Splits command arguments in one string into multiple strings.
    /// </summary>
    /// <param name="commandLine">The input command line arguments.</param>
    /// <returns>An enumeration of each command line argument.</returns>
    public static IEnumerable<string> SplitArgs(string? commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            yield break;
        }

        var result = new StringBuilder();

        var quoted = false;
        var escaped = false;
        var started = false;
        var allowcaret = false;
        for (int i = 0; i < commandLine.Length; i++)
        {
            var chr = commandLine[i];

            if (chr == '^' && !quoted)
            {
                if (allowcaret)
                {
                    result.Append(chr);
                    started = true;
                    escaped = false;
                    allowcaret = false;
                }
                else if (i + 1 < commandLine.Length && commandLine[i + 1] == '^')
                {
                    allowcaret = true;
                }
                else if (i + 1 == commandLine.Length)
                {
                    result.Append(chr);
                    started = true;
                    escaped = false;
                }
            }
            else if (escaped)
            {
                result.Append(chr);
                started = true;
                escaped = false;
            }
            else if (chr == '"')
            {
                quoted = !quoted;
                started = true;
            }
            else if (chr == '\\' && i + 1 < commandLine.Length && commandLine[i + 1] == '"')
            {
                escaped = true;
            }
            else if (chr == ' ' && !quoted)
            {
                if (started)
                {
                    yield return result.ToString();
                }

                result.Clear();
                started = false;
            }
            else
            {
                result.Append(chr);
                started = true;
            }
        }

        if (started)
        {
            yield return result.ToString();
        }
    }
}

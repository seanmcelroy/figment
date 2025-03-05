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

public static class Globals
{
    public enum GLOBAL_ERROR_CODES : int
    {
        SUCCESS = 0,
        ARGUMENT_ERROR = -1,
        NOT_FOUND = -2,
        AMBIGUOUS_MATCH = -3,
        UNKNOWN_TYPE = -4,
        GENERAL_IO_ERROR = -5,
        SCHEMA_LOAD_ERROR = -1000,
        SCHEMA_SAVE_ERROR = -1001,
        // SCHEMA_CREATE_ERROR = -1002
        THING_LOAD_ERROR = -2000,
        THING_SAVE_ERROR = -2001,
        // THING_CREATE_ERROR = -2002
        // THING_DELETE_ERROR = -2003
   }

    public static IEnumerable<string> SplitArgs(string? commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
            yield break;

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
                if (started) yield return result.ToString();
                result.Clear();
                started = false;
            }
            else
            {
                result.Append(chr);
                started = true;
            }
        }

        if (started) yield return result.ToString();
    }
}

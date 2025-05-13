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

namespace Figment.Common;

/// <summary>
/// Utility methods for handling files and file paths.
/// </summary>
public static class FileUtility
{
    /// <summary>
    /// Expands a file path that starts with a tilde into a full path.
    /// </summary>
    /// <param name="filePath">Original file path which may or may not begin with a tilde.</param>
    /// <returns>A full file path with the user profle / home directory expanded where a beginning tilde is present, or the same <paramref name="filePath"/> value otherwise.</returns>
    public static string ExpandRelativePaths(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        var prefix = $"~{Path.DirectorySeparatorChar}";
        if (!filePath.StartsWith(prefix))
        {
            return filePath;
        }

        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (profile.EndsWith(Path.DirectorySeparatorChar))
        {
            profile = profile[..^1];
        }

        return Path.Combine(profile, filePath[prefix.Length..]);
    }
}
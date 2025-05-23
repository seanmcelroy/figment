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

using Figment.Common.Calculations.Functions;

namespace Figment.Common;

/// <summary>
/// Utility methods for handling files and file paths.
/// </summary>
public static class FileUtility
{
    /// <summary>
    /// Counts the number of lines in a text stream.
    /// </summary>
    /// <param name="stream">Stream from which to read for line deliminters.</param>
    /// <returns>The number of lines detected in the stream.</returns>
    /// <remarks>See https://github.com/NimaAra/Easy.Common, which is MIT licensed.</remarks>
    public static long CountLines(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        const char NULL = '\0';
        const char CR = '\r';
        const char LF = '\n';

        long lineCount = 0;

        byte[] byteBuffer = new byte[1024 * 1024]; // 1 MB
        char detectedEOL = NULL;
        char currentChar = NULL;

        int bytesRead;
        while ((bytesRead = stream.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
        {
            for (int i = 0; i < bytesRead; i++)
            {
                currentChar = (char)byteBuffer[i];

                if (detectedEOL != NULL)
                {
                    if (currentChar == detectedEOL)
                    {
                        lineCount++;
                    }
                }
                else if (currentChar == LF || currentChar == CR)
                {
                    detectedEOL = currentChar;
                    lineCount++;
                }
            }
        }

        // We had a NON-EOL character at the end without a new line
        if (currentChar != LF && currentChar != CR && currentChar != NULL)
        {
            lineCount++;
        }

        return lineCount;
    }

    /// <summary>
    /// Expands a file path that starts with a tilde into a full path.
    /// </summary>
    /// <param name="filePath">Original file path which may or may not begin with a tilde.</param>
    /// <returns>A full file path with the user profle / home directory expanded where a beginning tilde is present, or the same <paramref name="filePath"/> value otherwise.</returns>
    public static string ExpandRelativePaths(string filePath)
    {
        return ExpandRelativePaths(filePath, "~/", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    }

    /// <summary>
    /// Expands a file path that starts with a tilde into a full path.
    /// </summary>
    /// <param name="filePath">Original file path which may or may not begin with a tilde.</param>
    /// <param name="token">The value to search for at the beginning of the <paramref name="filePath"/>.</param>
    /// <param name="replacement">The value to replace the <paramref name="token"/> when found.</param>
    /// <returns>A full file path with the user profle / home directory expanded where a beginning tilde is present, or the same <paramref name="filePath"/> value otherwise.</returns>
    public static string ExpandRelativePaths(this string filePath, string token, string replacement)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (!filePath.StartsWith(token))
        {
            return filePath;
        }

        if (replacement.EndsWith(Path.DirectorySeparatorChar))
        {
            replacement = replacement[..^1];
        }

        return Path.Combine(replacement, filePath[token.Length..]);
    }
}
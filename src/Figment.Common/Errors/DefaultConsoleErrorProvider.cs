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

namespace Figment.Common.Errors;

/// <summary>
/// A simple implementation of <see cref="IErrorProvider"/> that logs to <see cref="Console.Error"/>.
/// </summary>
public class DefaultConsoleErrorProvider : IErrorProvider
{
    /// <inheritdoc/>
    public void LogDone(FormattableString formattableString) => Console.Error.WriteLine(formattableString);

    /// <inheritdoc/>
    public void LogDone(string message) => Console.Error.WriteLine(message);

    /// <inheritdoc/>
    public void LogError(FormattableString formattableString) => Console.Error.WriteLine(formattableString);

    /// <inheritdoc/>
    public void LogError(string message) => Console.Error.WriteLine(message);

    /// <inheritdoc/>
    public void LogException(Exception ex, FormattableString formattableString)
    {
        Console.Error.WriteLine(formattableString);
        Console.Error.WriteLine(ex);
    }

    /// <inheritdoc/>
    public void LogException(Exception ex, string message)
    {
        Console.Error.WriteLine(message);
        Console.Error.WriteLine(ex);
    }

    /// <inheritdoc/>
    public void LogInfo(FormattableString formattableString) => Console.Error.WriteLine(formattableString);

    /// <inheritdoc/>
    public void LogInfo(string message) => Console.Error.WriteLine(message);

    /// <inheritdoc/>
    public void LogWarning(FormattableString formattableString) => Console.Error.WriteLine(formattableString);

    /// <inheritdoc/>
    public void LogWarning(string message) => Console.Error.WriteLine(message);

    /// <inheritdoc/>
    public void LogProgress(FormattableString formattableString) => Console.Error.WriteLine(formattableString);

    /// <inheritdoc/>
    public void LogProgress(string message) => Console.Error.WriteLine(message);

    /// <inheritdoc/>
    public void LogDebug(FormattableString formattableString) => Console.Error.WriteLine(formattableString);

    /// <inheritdoc/>
    public void LogDebug(string message) => Console.Error.WriteLine(message);
}
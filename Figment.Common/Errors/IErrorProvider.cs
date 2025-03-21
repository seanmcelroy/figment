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
/// A provider that handles error messages propogated from core components.
/// </summary>
public interface IErrorProvider
{
    /// <summary>
    /// Logs an exception.
    /// </summary>
    /// <param name="ex">The exception that was observed.</param>
    /// <param name="formattableString">A formattable string representing a human-readable message or context for the exception.</param>
    public void LogException(Exception ex, FormattableString formattableString);

    /// <summary>
    /// Logs an error.
    /// </summary>
    /// <param name="formattableString">A formattable string representing a human-readable message or context for the error.</param>
    public void LogError(FormattableString formattableString);

    /// <summary>
    /// Logs a warning.
    /// </summary>
    /// <param name="formattableString">A formattable string representing a human-readable message or context for the warning.</param>
    public void LogWarning(FormattableString formattableString);

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="formattableString">A formattable string representing a human-readable message.</param>
    public void LogInfo(FormattableString formattableString);

    /// <summary>
    /// Logs a message indicating an operation has completed.
    /// </summary>
    /// <param name="formattableString">A formattable string representing a human-readable message.</param>
    public void LogDone(FormattableString formattableString);

    /// <summary>
    /// Logs a message indicating an operation has begun or a status update if it is in progress.
    /// </summary>
    /// <param name="formattableString">A formattable string representing a human-readable message.</param>
    public void LogProgress(FormattableString formattableString);

    /// <summary>
    /// Logs a debugging message providing diagnostic information of interest to technical users or programmers.
    /// </summary>
    /// <param name="formattableString">A formattable string representing a human-readable message.</param>
    public void LogDebug(FormattableString formattableString);

    /// <summary>
    /// Logs an exception.
    /// </summary>
    /// <param name="ex">The exception that was observed.</param>
    /// <param name="message">A human-readable message or context for the exception.</param>
    public void LogException(Exception ex, string message);

    /// <summary>
    /// Logs an error.
    /// </summary>
    /// <param name="message">A human-readable message or context for the error.</param>
    public void LogError(string message);

    /// <summary>
    /// Logs a warning.
    /// </summary>
    /// <param name="message">A human-readable message or context for the warning.</param>
    public void LogWarning(string message);

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">A human-readable message.</param>
    public void LogInfo(string message);

    /// <summary>
    /// Logs a message indicating an operation has completed.
    /// </summary>
    /// <param name="message">A human-readable message.</param>
    public void LogDone(string message);

    /// <summary>
    /// Logs a message indicating an operation has begun or a status update if it is in progress.
    /// </summary>
    /// <param name="message">A human-readable message.</param>
    public void LogProgress(string message);

    /// <summary>
    /// Logs a debugging message providing diagnostic information of interest to technical users or programmers.
    /// </summary>
    /// <param name="message">A human-readable message.</param>
    public void LogDebug(string message);
}
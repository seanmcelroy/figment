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

using Figment.Common.Errors;

namespace jot.Test.TestUtilities;

public class TestErrorProvider : IErrorProvider
{
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    public List<string> InfoMessages { get; } = new();
    public List<string> DebugMessages { get; } = new();
    public List<string> DoneMessages { get; } = new();
    public List<string> ProgressMessages { get; } = new();
    public List<string> Exceptions { get; } = new();

    public void LogException(Exception ex, FormattableString formattableString)
    {
        var message = $"{formattableString}: {ex.Message}";
        Exceptions.Add(message);
        Console.WriteLine($"EXCEPTION: {message}");
    }

    public void LogError(FormattableString formattableString)
    {
        var message = formattableString.ToString();
        Errors.Add(message);
        Console.WriteLine($"ERROR: {message}");
    }

    public void LogWarning(FormattableString formattableString)
    {
        var message = formattableString.ToString();
        Warnings.Add(message);
        Console.WriteLine($"WARNING: {message}");
    }

    public void LogInfo(FormattableString formattableString)
    {
        var message = formattableString.ToString();
        InfoMessages.Add(message);
        Console.WriteLine($"INFO: {message}");
    }

    public void LogDone(FormattableString formattableString)
    {
        var message = formattableString.ToString();
        DoneMessages.Add(message);
        Console.WriteLine($"DONE: {message}");
    }

    public void LogProgress(FormattableString formattableString)
    {
        var message = formattableString.ToString();
        ProgressMessages.Add(message);
        Console.WriteLine($"PROGRESS: {message}");
    }

    public void LogDebug(FormattableString formattableString)
    {
        var message = formattableString.ToString();
        DebugMessages.Add(message);
        Console.WriteLine($"DEBUG: {message}");
    }

    public void LogException(Exception ex, string message)
    {
        var fullMessage = $"{message}: {ex.Message}";
        Exceptions.Add(fullMessage);
        Console.WriteLine($"EXCEPTION: {fullMessage}");
    }

    public void LogError(string message)
    {
        Errors.Add(message);
        Console.WriteLine($"ERROR: {message}");
    }

    public void LogWarning(string message)
    {
        Warnings.Add(message);
        Console.WriteLine($"WARNING: {message}");
    }

    public void LogInfo(string message)
    {
        InfoMessages.Add(message);
        Console.WriteLine($"INFO: {message}");
    }

    public void LogDone(string message)
    {
        DoneMessages.Add(message);
        Console.WriteLine($"DONE: {message}");
    }

    public void LogProgress(string message)
    {
        ProgressMessages.Add(message);
        Console.WriteLine($"PROGRESS: {message}");
    }

    public void LogDebug(string message)
    {
        DebugMessages.Add(message);
        Console.WriteLine($"DEBUG: {message}");
    }

    public void Clear()
    {
        Errors.Clear();
        Warnings.Clear();
        InfoMessages.Clear();
        DebugMessages.Clear();
        DoneMessages.Clear();
        ProgressMessages.Clear();
        Exceptions.Clear();
    }
}
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

public class DefaultConsoleErrorProvider : IErrorProvider
{
    public void LogDone(FormattableString formattableString) => Console.Out.WriteLine(formattableString);

    public void LogDone(string message) => Console.Out.WriteLine(message);

    public void LogError(FormattableString formattableString) => Console.Error.WriteLine(formattableString);

    public void LogError(string message) => Console.Error.WriteLine(message);

    public void LogException(Exception ex, FormattableString formattableString)
    {
        Console.Error.WriteLine(formattableString);
        Console.Error.WriteLine(ex);
    }

    public void LogException(Exception ex, string message)
    {
        Console.Error.WriteLine(message);
        Console.Error.WriteLine(ex);
    }

    public void LogInfo(FormattableString formattableString) => Console.Error.WriteLine(formattableString);

    public void LogInfo(string message) => Console.Error.WriteLine(message);

    public void LogWarning(FormattableString formattableString) => Console.Error.WriteLine(formattableString);

    public void LogWarning(string message) => Console.Error.WriteLine(message);

    public void LogProgress(FormattableString formattableString) => Console.Error.WriteLine(formattableString);

    public void LogProgress(string message) => Console.Error.WriteLine(message);

    public void LogDebug(FormattableString formattableString)=> Console.Error.WriteLine(formattableString);

    public void LogDebug(string message) => Console.Error.WriteLine(message);
}
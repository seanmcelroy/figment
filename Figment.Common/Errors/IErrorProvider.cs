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

public interface IErrorProvider
{
    public void LogException(Exception ex, FormattableString formattableString);
    public void LogError(FormattableString formattableString);
    public void LogWarning(FormattableString formattableString);
    public void LogInfo(FormattableString formattableString);
    public void LogDone(FormattableString formattableString);

    public void LogException(Exception ex, string message);
    public void LogError(string message);
    public void LogWarning(string message);
    public void LogInfo(string message);
    public void LogDone(string message);
}
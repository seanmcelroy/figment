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

using System.Diagnostics.CodeAnalysis;

namespace Figment.Common.Calculations.Functions;

/// <summary>
/// Utility methods for handling dates.
/// </summary>
public static class DateUtility
{
    /// <summary>
    /// The <see cref="DateTime"/> for January 1, 1900.
    /// </summary>
    internal static readonly DateTime TwentiethCentry = new DateTime(1900, 01, 01, 0, 0, 0, DateTimeKind.Utc).Date;

    /// <summary>
    /// Attempts to parse a functional date value to a <see cref="DateTime"/>.
    /// </summary>
    /// <param name="functionalDateValue">The fractional days since January 1, 1900 to parse into a date.</param>
    /// <param name="dateTime">If parsing <paramref name="functionalDateValue"/> was successful, the <see cref="DateTime"/> representation of the functional date value.</param>
    /// <returns>Whether or not the parsing was successful.</returns>
    public static bool TryParseDate(double functionalDateValue, out DateTime dateTime)
    {
        dateTime = TwentiethCentry.AddDays(functionalDateValue);
        return true;
    }

    /// <summary>
    /// Attempts to parse a date time into a functional date value, that is, the
    /// fractional number of days since January 1, 1900.
    /// </summary>
    /// <param name="dateTime">The date to parse as a functional date value.</param>
    /// <param name="fdv">If parsing <paramref name="dateTime"/> was successful, the functional date value.</param>
    /// <seealso cref="TwentiethCentry"/>
    /// <returns>Whether or not the parsing was successful.</returns>
    public static bool TryParseFunctionalDateValue([NotNullWhen(true)] DateTime? dateTime, out double fdv)
    {
        if (dateTime == null)
        {
            fdv = 0;
            return false;
        }

        TimeSpan ts = dateTime.Value - TwentiethCentry;
        fdv = ts.TotalDays;
        return true;
    }

    /// <summary>
    /// Attempts to parse a date time into a functional date value, that is, the
    /// fractional number of days since January 1, 1900.
    /// </summary>
    /// <param name="dateTime">The date to parse as a functional date value.</param>
    /// <param name="fdv">If parsing <paramref name="dateTime"/> was successful, the functional date value.</param>
    /// <seealso cref="TwentiethCentry"/>
    /// <returns>Whether or not the parsing was successful.</returns>
    public static bool TryParseFunctionalDateValue([NotNullWhen(true)] string? dateTime, out double fdv)
    {
        if (DateTime.TryParse(dateTime, out DateTime dt))
        {
            var result = TryParseFunctionalDateValue(dt, out double fdv2);
            fdv = fdv2;
            return result;
        }

        if (double.TryParse(dateTime, out double x))
        {
            fdv = x;
            return true;
        }

        fdv = 0;
        return false;
    }
}
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

    /// <summary>
    /// Turns a time span into a human-readable relative phrase, such as "one second ago" or "yesterday".
    /// </summary>
    /// <param name="duration">Duration to convert into a phrase.</param>
    /// <returns>An English phrase representing the elapsed relative time.</returns>
    public static string GetRelativePastTimeString(TimeSpan duration)
    {
        const int MINUTE = 60;
        const int HOUR = 60 * MINUTE;
        const int DAY = 24 * HOUR;
        const int MONTH = 30 * DAY;

        double delta = Math.Abs(duration.TotalSeconds);

        if (delta < 1 * MINUTE)
        {
            return duration.Seconds == 1 ? "one second ago" : duration.Seconds + " seconds ago";
        }

        if (delta < 2 * MINUTE)
        {
            return "a minute ago";
        }

        if (delta < 45 * MINUTE)
        {
            return duration.Minutes + " minutes ago";
        }

        if (delta < 90 * MINUTE)
        {
            return "an hour ago";
        }

        if (delta < 24 * HOUR)
        {
            return duration.Hours + " hours ago";
        }

        if (delta < 48 * HOUR)
        {
            return "yesterday";
        }

        if (delta < 30 * DAY)
        {
            return duration.Days + " days ago";
        }

        if (delta < 12 * MONTH)
        {
            int months = Convert.ToInt32(Math.Floor((double)duration.Days / 30));
            return months <= 1 ? "one month ago" : months + " months ago";
        }
        else
        {
            int years = Convert.ToInt32(Math.Floor((double)duration.Days / 365));
            return years <= 1 ? "one year ago" : years + " years ago";
        }
    }

    /// <summary>
    /// Turns a time span into a human-readable relative phrase, such as "one second" or "a day".
    /// </summary>
    /// <param name="duration">Duration to convert into a phrase.</param>
    /// <returns>An English phrase representing the elapsed relative time.</returns>
    public static string GetRelativeTimeString(TimeSpan duration)
    {
        const int MINUTE = 60;
        const int HOUR = 60 * MINUTE;
        const int DAY = 24 * HOUR;
        const int MONTH = 30 * DAY;

        double delta = Math.Abs(duration.TotalSeconds);

        if (delta < 1 * MINUTE)
        {
            return duration.Seconds == 1 ? "one second" : duration.Seconds + " seconds";
        }

        if (delta < 2 * MINUTE)
        {
            return "a minute";
        }

        if (delta < 45 * MINUTE)
        {
            return duration.Minutes + " minutes";
        }

        if (delta < 90 * MINUTE)
        {
            return "an hour";
        }

        if (delta < 24 * HOUR)
        {
            return duration.Hours + " hours";
        }

        if (delta < 48 * HOUR)
        {
            return "a day";
        }

        if (delta < 30 * DAY)
        {
            return duration.Days + " days";
        }

        if (delta < 12 * MONTH)
        {
            int months = Convert.ToInt32(Math.Floor((double)duration.Days / 30));
            return months <= 1 ? "one month" : months + " months";
        }
        else
        {
            int years = Convert.ToInt32(Math.Floor((double)duration.Days / 365));
            return years <= 1 ? "one year" : years + " years";
        }
    }
}
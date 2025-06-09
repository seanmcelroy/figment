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
using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Figment.Common;

/// <summary>
/// A field that stores a date and time value.
/// </summary>
/// <param name="Name">Name of the field on a <see cref="Schema"/>.</param>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public partial class SchemaDateField(string Name) : SchemaTextField(Name)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    /// <summary>
    /// A constant string value representing schema fields of this type.
    /// </summary>
    /// <remarks>
    /// This value is usually encoded into JSON serialized representations of
    /// schema fields and used for polymorphic type indication.
    /// </remarks>
    public new const string SCHEMA_FIELD_TYPE = "date";

    /// <summary>
    /// Formats that this date field will attempt to parse exactly, such as RFC 3339 formats.
    /// </summary>
    internal static readonly string[] _completeFormats = [
        "yyyy-MM-ddTHH:mm:ssK",
        "yyyy-MM-ddTHH:mm:ss.fffffffK",
        "yyyy-MM-ddTHH:mm:ss.ffffffK",
        "yyyy-MM-ddTHH:mm:ss.fffffK",
        "yyyy-MM-ddTHH:mm:ss.ffffK",
        "yyyy-MM-ddTHH:mm:ss.fffK",
        "yyyy-MM-ddTHH:mm:ss.ffK",
        "yyyy-MM-ddTHH:mm:ss.fK",
        "yyyy-MM-ddTHH:mm:ssZ",
        "yyyy-MM-ddTHH:mm:ss.ffZ",

        // Fallbacks
        "yyyy-MM-dd H:mm tt",
        "yyyy-MM-dd HH:mm",
        "yyyy-MM-dd",
        "MM/dd/yyyy H:mm tt",
        "MM/dd/yyyy HH:mm",
        "MM/dd/yyyy",
        "MM/dd/yy H:mm tt",
        "MM/dd/yy HH:mm",
        "MM/dd/yy",
        "M/d/yy H:mm tt",
        "M/d/yy HH:mm",
        "M/d/yy",
        "M/d/yyyy H:mm tt",
        "M/d/yyyy HH:mm",
        "M/d/yyyy",
        DateTimeFormatInfo.InvariantInfo.UniversalSortableDateTimePattern,
        DateTimeFormatInfo.InvariantInfo.SortableDateTimePattern,

        // Weird fallbacks
        "MMM d, yyyy",
        "MMMM d, yyyy",
        "MMM d yyyy",
        "MMMM d yyyy",
    ];

    /// <summary>
    /// Formats that this date field will attempt to parse exactly, such as RFC 3339 formats.
    /// </summary>
    private static readonly string[] _partialMonthDayFormats = [
        "MMM d",  // jul 4
        "MMMd",   // jul4
        "MMMM d", // july 4
        "MMMMd",  // july4
        "dMMM",   // 31oct
        "d MMM",  // 31 oct
    ];

    private static readonly string[] _partialYearMonthFormats = [
        "yyyy-MM",
        "MM/yyyy",
    ];

    private static readonly Dictionary<string, int> WordNumbers = new()
    {
        { "zero", 0 },
        { "a", 1 },
        { "one", 1 },
        { "two", 2 },
        { "a couple", 2 },
        { "a couple of", 2 },
        { "couple of", 2 },
        { "three", 3 },
        { "few", 3 },
        { "a few", 3 },
        { "four", 4 },
        { "five", 5 },
        { "six", 6 },
        { "seven", 7 },
        { "eight", 8 },
        { "nine", 9 },
        { "ten", 10 },
        { "eleven", 11 },
        { "twelve", 12 },
        { "thirteen", 13 },
        { "thirtheen", 13 }, // Handle common user misspelling
        { "fourteen", 14 },
        { "fourtheen", 14 }, // Handle common user misspelling
        { "fifteen", 15 },
        { "fiftheen", 15 }, // Handle common user misspelling
        { "sixteen", 16 },
        { "seventeen", 17 },
        { "eighteen", 18 },
        { "eightteen", 18 }, // Handle common user misspelling
        { "ninteen", 19 }, // Handle common user misspelling
        { "nineteen", 19 },
        { "twenty", 20 },
    };

    [GeneratedRegex(@"^(?:on\s+|next\s+)?(\w+)$")]
    private static partial Regex OnDayRegex();

    [GeneratedRegex(@"^last\s+(\w+)$")]
    private static partial Regex LastDayRegex();

    [GeneratedRegex(@"^(?:in\s+|after\s+)?(\d+|\w+)\s+days?$")]
    private static partial Regex InDaysRegex();

    [GeneratedRegex(@"^(?:in\s+|after\s+)?(\d+|\w+)\s+(?:weeks|wekks)?$")]
    private static partial Regex InWeeksRegex();

    [GeneratedRegex(@"^(?:in\s+|after\s+)?(\d+|\w+)\s+months?$")]
    private static partial Regex InMonthsRegex();

    /// <inheritdoc/>
    [JsonPropertyName("type")]
    public override string Type { get; } = "string"; // SCHEMA_FIELD_TYPE does not match JSON schema

    /// <summary>
    /// Gets the format of this string field.
    /// </summary>
    /// <remarks>
    /// Because dates are serialized in JSON as strings with format 'date', the value of this field
    /// is always <![CDATA[date]]>.
    /// </remarks>
    [JsonPropertyName("format")]
    public string Format { get; } = "date"; // SCHEMA_FIELD_TYPE does not match JSON schema

    /// <inheritdoc/>
    public override Task<string> GetReadableFieldTypeAsync(bool verbose, CancellationToken cancellationToken) => Task.FromResult("date");

    /// <inheritdoc/>
    public override async Task<bool> IsValidAsync(object? value, CancellationToken cancellationToken)
    {
        if (value == null)
        {
            return !Required;
        }

        // If native DateTime or DateTimeOffset, go with it.
        if (value is DateTimeOffset)
        {
            return true;
        }

        if (value is DateTime)
        {
            return true;
        }

        // Handle it like text
        if (!await base.IsValidAsync(value, cancellationToken))
        {
            return false;
        }

        // We parse here from a string because JSON doesn't support native dates.
        return TryParseDate(value.ToString(), out DateTimeOffset _);
    }

    /// <inheritdoc/>
    public override bool TryMassageInput(object? input, [MaybeNullWhen(true)] out object? output)
    {
        if (input == null || input.GetType() == typeof(DateTimeOffset))
        {
            output = input;
            return true;
        }

        var inputString = input.ToString();

        if (TryParseDate(inputString, out DateTimeOffset provisionalDateTimeOffset))
        {
            output = provisionalDateTimeOffset;
            return true;
        }

        output = null;
        return false;
    }

    /// <summary>
    /// Attempts to parse a string into a <see cref="DateTimeOffset"/> value.
    /// </summary>
    /// <param name="input">The string to parse as a boolean value.</param>
    /// <param name="output">If successful, the <see cref="DateTimeOffset"/> value parsed from the <paramref name="input"/>.</param>
    /// <returns>A boolean indicating whether or not <paramref name="input"/> could be parsed into the <paramref name="output"/> <see cref="DateTimeOffset"/>.</returns>
    public static bool TryParseDate([NotNullWhen(true)] string? input, out DateTimeOffset output)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            output = DateTimeOffset.MinValue;
            return false;
        }

        if (DateTimeOffset.TryParseExact(input, _completeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset dte))
        {
            output = dte;
            return true;
        }

        var today = DateTimeOffset.Now.Date; // By using .Date, the .Offset property is lost because it is now a DateTime and not a DateTimeOffset.

        if (DateTimeOffset.TryParseExact(input, _partialMonthDayFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTimeOffset partialDate))
        {
            // If this is Feb 29, but this is not a leap year, then it is not valid.
            if (partialDate.Month == 2 && partialDate.Day == 29 && !DateTime.IsLeapYear(today.Year))
            {
                output = DateTimeOffset.MinValue;
                return false;
            }

            var candidate = new DateTimeOffset(today.Year, partialDate.Month, partialDate.Day, 0, 0, 0, DateTimeOffset.Now.Offset);
            if (candidate < today)
            {
                candidate = candidate.AddYears(1);
            }

            output = candidate;
            return true;
        }

        if (DateTimeOffset.TryParseExact(input, _partialYearMonthFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out partialDate))
        {
            output = new DateTimeOffset(partialDate.Year, partialDate.Month, 1, 0, 0, 0, DateTimeOffset.Now.Offset);
            return true;
        }

        // Relative date parsing
        input = input.Trim().ToLowerInvariant();
        if (string.Equals(input, "today", StringComparison.CurrentCultureIgnoreCase))
        {
            output = today;
            return true;
        }

        if (string.Equals(input, "yesterday", StringComparison.CurrentCultureIgnoreCase))
        {
            output = today.AddDays(-1);
            return true;
        }

        if (string.Equals(input, "tomorrow", StringComparison.CurrentCultureIgnoreCase))
        {
            output = today.AddDays(1);
            return true;
        }

        if (string.Equals(input, "next week", StringComparison.CurrentCultureIgnoreCase)
           || string.Equals(input, "in a week", StringComparison.CurrentCultureIgnoreCase))
        {
            output = today.AddDays(7);
            return true;
        }

        if (string.Equals(input, "last week", StringComparison.CurrentCultureIgnoreCase))
        {
            output = today.AddDays(-7);
            return true;
        }

        if (string.Equals(input, "next month", StringComparison.CurrentCultureIgnoreCase)
           || string.Equals(input, "in a month", StringComparison.CurrentCultureIgnoreCase))
        {
            output = today.AddMonths(1);
            return true;
        }

        if (string.Equals(input, "last month", StringComparison.CurrentCultureIgnoreCase))
        {
            output = today.AddMonths(-1);
            return true;
        }

        if (string.Equals(input, "next year", StringComparison.CurrentCultureIgnoreCase)
           || string.Equals(input, "in a year", StringComparison.CurrentCultureIgnoreCase))
        {
            output = today.AddYears(1);
            return true;
        }

        if (string.Equals(input, "last year", StringComparison.CurrentCultureIgnoreCase))
        {
            output = today.AddYears(-1);
            return true;
        }

        // Parse "on/next {weekday}"
        {
            Match onDayMatch = OnDayRegex().Match(input);
            if (onDayMatch.Success
                && Enum.TryParse<DayOfWeek>(onDayMatch.Groups[1].Value, true, out var nextDayOfWeek))
            {
                int daysUntil = ((int)nextDayOfWeek - (int)today.DayOfWeek + 7) % 7;
                if (daysUntil == 0)
                {
                    daysUntil = 7; // Always future
                }

                output = today.AddDays(daysUntil);
                return true;
            }
        }

        // Parse "last {weekday}"
        Match lastDayMatch = LastDayRegex().Match(input);
        if (lastDayMatch.Success
            && Enum.TryParse<DayOfWeek>(lastDayMatch.Groups[1].Value, true, out var lastDayOfWeek))
        {
            int daysSince = ((int)today.DayOfWeek - (int)lastDayOfWeek + 7) % 7;
            if (daysSince == 0)
            {
                daysSince = 7; // Always past
            }

            output = today.AddDays(-daysSince);
            return true;
        }

        // Parse "in/after {number} days"
        {
            Match inDaysMatch = InDaysRegex().Match(input);
            if (inDaysMatch.Success)
            {
                if (int.TryParse(inDaysMatch.Groups[1].Value, out int inDays))
                {
                    output = today.AddDays(inDays);
                    return true;
                }

                if (WordNumbers.TryGetValue(inDaysMatch.Groups[1].Value.ToLowerInvariant(), out inDays))
                {
                    output = today.AddDays(inDays);
                    return true;
                }
            }
        }

        // Parse "in/after {number} weeks"
        {
            Match inWeeksMatch = InWeeksRegex().Match(input);
            if (inWeeksMatch.Success)
            {
                if (int.TryParse(inWeeksMatch.Groups[1].Value, out int inWeeks))
                {
                    output = today.AddDays(inWeeks * 7);
                    return true;
                }

                if (WordNumbers.TryGetValue(inWeeksMatch.Groups[1].Value.ToLowerInvariant(), out inWeeks))
                {
                    output = today.AddDays(inWeeks * 7);
                    return true;
                }
            }
        }

        // Parse "in/after {number} months"
        {
            Match inMonthsMatch = InMonthsRegex().Match(input);
            if (inMonthsMatch.Success)
            {
                if (int.TryParse(inMonthsMatch.Groups[1].Value, out int inMonths))
                {
                    output = today.AddMonths(inMonths);
                    return true;
                }

                if (WordNumbers.TryGetValue(inMonthsMatch.Groups[1].Value.ToLowerInvariant(), out inMonths))
                {
                    output = today.AddMonths(inMonths);
                    return true;
                }
            }
        }

        // No match
        output = DateTimeOffset.MinValue;
        return false;
    }
}
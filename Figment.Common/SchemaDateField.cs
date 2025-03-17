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

namespace Figment.Common;

public class SchemaDateField(string Name) : SchemaTextField(Name)
{
    public const string SCHEMA_FIELD_TYPE = "date";

    // RFC 3339 Formats
    internal static readonly string[] _formats = [
        "yyyy-MM-ddTHH:mm:ssK",
        "yyyy-MM-ddTHH:mm:ss.ffK",
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

    [JsonPropertyName("type")]
    public override string Type { get; } = "string"; // SCHEMA_FIELD_TYPE does not match JSON schema

    [JsonPropertyName("format")]
    public string Format { get; } = "date"; // SCHEMA_FIELD_TYPE does not match JSON schema

    public override Task<string> GetReadableFieldTypeAsync(bool _, CancellationToken cancellationToken) => Task.FromResult("date");

    public override async Task<bool> IsValidAsync(object? value, CancellationToken cancellationToken)
    {
        if (value == null)
            return !Required;

        // If native DateTime or DateTimeOffset, go with it.
        if (value is DateTimeOffset)
            return true;
        if (value is DateTime)
            return true;

        // Handle it like text
        if (!await base.IsValidAsync(value, cancellationToken))
            return false;

        // We parse here from a string because JSON doesn't support native dates.
        return TryParseDate(value.ToString(), out DateTimeOffset _);
    }

    public override bool TryMassageInput(object? input, out object? output)
    {
        if (input == null || input.GetType() == typeof(DateTimeOffset))
        {
            output = input;
            return true;
        }

        var prov = input.ToString();

        if (TryParseDate(prov, out DateTimeOffset provDto))
        {
            output = provDto;
            return true;
        }

        output = null;
        return false;
    }

    public static bool TryParseDate([NotNullWhen(true)] string? input, out DateTimeOffset output)
    {
        if (DateTimeOffset.TryParseExact(input, _formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset dte))
        {
            output = dte;
            return true;
        }

        output = DateTimeOffset.MinValue;
        return false;
    }
}
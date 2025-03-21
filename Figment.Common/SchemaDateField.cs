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

/// <summary>
/// A field that stores a date and time value.
/// </summary>
/// <param name="Name">Name of the field on a <see cref="Schema"/>.</param>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public class SchemaDateField(string Name) : SchemaTextField(Name)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    /// <summary>
    /// A constant string value representing schema fields of this type.
    /// </summary>
    /// <remarks>
    /// This value is usually encoded into JSON serialized representations of
    /// schema fields and used for polymorphic type indication.
    /// </remarks>
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
    public override Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken) => Task.FromResult("date");

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

    /// <summary>
    /// Attempts to parse a string into a <see cref="DateTimeOffset"/> value.
    /// </summary>
    /// <param name="input">The string to parse as a boolean value.</param>
    /// <param name="output">If successful, the <see cref="DateTimeOffset"/> value parsed from the <paramref name="input"/>.</param>
    /// <returns>A boolean indicating whether or not <paramref name="input"/> could be parsed into the <paramref name="output"/> <see cref="DateTimeOffset"/>.</returns>
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
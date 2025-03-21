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
/// An integer field representing a month and day in any non-specific year.
/// </summary>
/// <param name="Name">Name of the field on a <see cref="Schema"/>.</param>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public class SchemaMonthDayField(string Name) : SchemaIntegerField(Name)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    /// <summary>
    /// A constant string value representing schema fields of this type.
    /// </summary>
    /// <remarks>
    /// This value is usually encoded into JSON serialized representations of
    /// schema fields and used for polymorphic type indication.
    /// </remarks>
    public new const string SCHEMA_FIELD_TYPE = "monthday";

    // RFC 3339 Formats
    private static readonly string[] _formats = [
        "M-d",
        "M/d",
        "MMM d",
        "MMMM d",
    ];

    /// <inheritdoc/>
    [JsonPropertyName("type")]
    public override string Type { get; } = SchemaIntegerField.SCHEMA_FIELD_TYPE; // SCHEMA_FIELD_TYPE does not match JSON schema

    /// <inheritdoc/>
    public override Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken) => Task.FromResult("month+day");

    /// <inheritdoc/>
    public override async Task<bool> IsValidAsync(object? value, CancellationToken cancellationToken)
    {
        if (value is DateTimeOffset)
        {
            return true;
        }

        if (value is DateTime)
        {
            return true;
        }

        if (!await base.IsValidAsync(value, cancellationToken))
        {
            return false;
        }

        if (value == null)
        {
            return true;
        }

        return value is int i && IsValid(i);
    }

    /// <inheritdoc/>
    public override bool TryMassageInput(object? input, out object? output)
    {
        if (input == null)
        {
            output = input;
            return true;
        }

        var prov = input.ToString();
        if (TryParseMonthDay(prov, out int provMD))
        {
            output = provMD;
            return true;
        }

        if (SchemaDateField.TryParseDate(prov, out DateTimeOffset dto))
        {
            output = (dto.Month * 100) + dto.Day;
            return true;
        }

        output = null;
        return false;
    }

    public static bool TryParseMonthDay([NotNullWhen(true)] string? input, out int output)
    {
        if (input != null && int.TryParse(input.ToString(), out int i) && IsValid(i))
        {
            output = i;
            return true;
        }

        if (DateTimeOffset.TryParseExact(input, _formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset dte))
        {
            output = (dte.Month * 100) + dte.Day;
            return true;
        }

        output = 0;
        return false;
    }

    private static bool IsValid(int i)
    {
        int month = i / 100;
        int day = i - (month * 100);
        return month >= 1
            && month <= 12
            && day >= 1
            && day <= DateTime.DaysInMonth(2000, month); // Use a random leap year for this.
    }
}
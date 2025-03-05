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
        "yyyy-MM-dd",
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

    public override Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken) => Task.FromResult("date");

    public override async Task<bool> IsValidAsync(object? value, CancellationToken cancellationToken)
    {
        if (!await base.IsValidAsync(value, cancellationToken))
            return false;

        var str = value as string;

        return DateTimeOffset.TryParseExact(str, _formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _);
    }    
}
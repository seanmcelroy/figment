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
using System.Text.Json.Serialization;

namespace Figment.Common;

public class SchemaBooleanField(string Name) : SchemaFieldBase(Name)
{
    public const string SCHEMA_FIELD_TYPE = "bool";

    [JsonPropertyName("type")]
    public override string Type { get; } = SCHEMA_FIELD_TYPE;

    public override Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken) => Task.FromResult(SCHEMA_FIELD_TYPE);

    public override Task<bool> IsValidAsync(object? value, CancellationToken _)
    {
        if (!Required && value == null)
            return Task.FromResult(true);
        if (Required && value == null)
            return Task.FromResult(false);

        if (value is string)
            return Task.FromResult(false); // Should be native boolean.

        return Task.FromResult(bool.TryParse(value!.ToString(), out bool _));
    }

    public static bool TryParseBoolean([NotNullWhen(true)] string? input, out bool output) {
        if (bool.TryParse(input, out bool provBool))
        {
            output = provBool;
            return true;
        }

        if (string.Compare("yes", input, StringComparison.CurrentCultureIgnoreCase) == 0)
        {
            output = true;
            return true;
        }

        if (string.Compare("no", input, StringComparison.CurrentCultureIgnoreCase) == 0)
        {
            output = false;
            return true;
        }

        output = false;
        return false;
    }

    public override bool TryMassageInput(object? input, out object? output)
    {
        if (input == null || input.GetType() == typeof(bool))
        {
            output = input;
            return true;
        }

        if (input is int i)
        {
            output = i != 0;
            return true;
        }

        var prov = input.ToString();

        if (TryParseBoolean(prov, out bool provBool))
        {
            output = provBool;
            return true;
        }

        return base.TryMassageInput(input, out output);
    }
}
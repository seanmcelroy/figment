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

/// <summary>
/// A boolean field which stores a true or false value.
/// </summary>
/// <param name="Name">Name of the field on a <see cref="Schema"/>.</param>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public class SchemaBooleanField(string Name) : SchemaFieldBase(Name)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    /// <summary>
    /// A constant string value representing schema fields of this type.
    /// </summary>
    /// <remarks>
    /// This value is usually encoded into JSON serialized representations of
    /// schema fields and used for polymorphic type indication.
    /// </remarks>
    public const string SCHEMA_FIELD_TYPE = "bool";

    /// <inheritdoc/>
    [JsonPropertyName("type")]
    public override string Type { get; } = SCHEMA_FIELD_TYPE;

    /// <inheritdoc/>
    public override Task<string> GetReadableFieldTypeAsync(bool verbose, CancellationToken cancellationToken) => Task.FromResult(SCHEMA_FIELD_TYPE);

    /// <inheritdoc/>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
    public override Task<bool> IsValidAsync(object? value, CancellationToken _)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
    {
        if (value == null)
        {
            return Task.FromResult(!Required);
        }

        if (value is string)
        {
            return Task.FromResult(false); // Should be native boolean.
        }

        return Task.FromResult(bool.TryParse(value.ToString(), out bool _));
    }

    /// <summary>
    /// Attempts to parse a string into a boolean value.
    /// </summary>
    /// <param name="input">The string to parse as a boolean value.</param>
    /// <param name="output">If successful, the boolean value parsed from the <paramref name="input"/>.</param>
    /// <returns>A boolean indicating whether or not <paramref name="input"/> could be parsed into the <paramref name="output"/> boolean.</returns>
    public static bool TryParseBoolean([NotNullWhen(true)] string? input, out bool output)
    {
        if (bool.TryParse(input, out bool provBool))
        {
            output = provBool;
            return true;
        }

        if (string.Equals("yes", input, StringComparison.CurrentCultureIgnoreCase))
        {
            output = true;
            return true;
        }

        if (string.Equals("no", input, StringComparison.CurrentCultureIgnoreCase))
        {
            output = false;
            return true;
        }

        if (string.Equals("on", input, StringComparison.CurrentCultureIgnoreCase))
        {
            output = true;
            return true;
        }

        if (string.Equals("off", input, StringComparison.CurrentCultureIgnoreCase))
        {
            output = false;
            return true;
        }

        if (int.TryParse(input, out int provInt))
        {
            output = provInt != 0;
            return true;
        }

        output = false;
        return false;
    }

    /// <inheritdoc/>
    public override bool TryMassageInput(object? input, [NotNullWhen(false), MaybeNullWhen(true)] out object? output)
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

        output = false;
        return false;
    }
}
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
/// A field which stores an integral number.
/// </summary>
/// <param name="Name">Name of the field on a <see cref="Schema"/>.</param>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public class SchemaIntegerField(string Name) : SchemaFieldBase(Name)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    /// <summary>
    /// A constant string value representing schema fields of this type.
    /// </summary>
    /// <remarks>
    /// This value is usually encoded into JSON serialized representations of
    /// schema fields and used for polymorphic type indication.
    /// </remarks>
    public const string SCHEMA_FIELD_TYPE = "integer";

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

        if (value is byte
            || value is int
            || value is long
            || value is ulong)
        {
            return Task.FromResult(true);
        }

        var vs = value.ToString();

        if (long.TryParse(vs, out long _))
        {
            return Task.FromResult(true);
        }

        if (ulong.TryParse(vs, out ulong _))
        {
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public override bool TryMassageInput(object? input, [MaybeNullWhen(true)] out object? output)
    {
        if (input != null)
        {
            if (input is byte b)
            {
                output = b;
                return true;
            }

            if (input is int i)
            {
                output = i;
                return true;
            }

            if (input is long l)
            {
                output = l;
                return true;
            }

            if (input is ulong u)
            {
                output = u;
                return true;
            }

            var inputString = input.ToString();

            if (ulong.TryParse(inputString, out ulong u2))
            {
                output = u2;
                return true;
            }

            if (long.TryParse(inputString, out long l2))
            {
                output = l2;
                return true;
            }
        }

        output = null;
        return false;
    }
}
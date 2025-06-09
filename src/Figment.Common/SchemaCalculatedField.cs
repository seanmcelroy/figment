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

using System.Text.Json.Serialization;
using Figment.Common.Calculations.Parsing;
using Figment.Common.Errors;

namespace Figment.Common;

/// <summary>
/// A field which dynamically calculates its value base don a formula.
/// </summary>
/// <param name="Name">Name of the field on a <see cref="Schema"/>.</param>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public class SchemaCalculatedField(string Name) : SchemaFieldBase(Name)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    /// <summary>
    /// A constant string value representing schema fields of this type.
    /// </summary>
    /// <remarks>
    /// This value is usually encoded into JSON serialized representations of
    /// schema fields and used for polymorphic type indication.
    /// </remarks>
    public const string SCHEMA_FIELD_TYPE = "calculated";

    /// <inheritdoc/>
    [JsonPropertyName("type")]
    public override string Type { get; } = SCHEMA_FIELD_TYPE;

    /// <summary>
    /// Gets or sets the formula used to calculate new values for this field.
    /// </summary>
    [JsonPropertyName("formula")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Formula { get; set; }

    /// <inheritdoc/>
    public override Task<string> GetReadableFieldTypeAsync(bool verbose, CancellationToken cancellationToken) => Task.FromResult($"calculated: {Formula}");

    /// <inheritdoc/>
    public override Task<bool> IsValidAsync(object? value, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Formula))
        {
            return Task.FromResult(false);
        }

        var parser = new ExpressionParser();
        try
        {
            var ast = parser.Parse(Formula);
        }
        catch (ParseException pe)
        {
            AmbientErrorContext.Provider.LogDebug($"Could not parse formula '{Formula}': {pe.Message}");
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}
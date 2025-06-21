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
using System.Text.RegularExpressions;

namespace Figment.Common;

/// <summary>
/// A field which stores a string.
/// </summary>
/// <param name="Name">Name of the field on a <see cref="Schema"/>.</param>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public class SchemaTextField(string Name) : SchemaFieldBase(Name)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    /// <summary>
    /// A constant string value representing schema fields of this type.
    /// </summary>
    /// <remarks>
    /// This value is usually encoded into JSON serialized representations of
    /// schema fields and used for polymorphic type indication.
    /// </remarks>
    public const string SCHEMA_FIELD_TYPE = "string";

    /// <inheritdoc/>
    [JsonPropertyName("type")]
    public override string Type { get; } = SCHEMA_FIELD_TYPE;

    /// <summary>
    /// Gets or sets the minimum number of characters the text must have, if any.
    /// </summary>
    [JsonPropertyName("minLength")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ushort? MinLength { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of characters the text must have, if any.
    /// </summary>
    [JsonPropertyName("maxLength")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ushort? MaxLength { get; set; }

    /// <summary>
    /// Gets or sets the regular expression pattern the text must adhere to, if any.
    /// </summary>
    [JsonPropertyName("pattern")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual string? Pattern { get; set; }

    /// <inheritdoc/>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
    public override Task<bool> IsValidAsync(object? value, CancellationToken _)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
    {
        return Task.FromResult(IsValidInternal(value));
    }

    /// <summary>
    /// Validates a parsed field meets all applicable optionally-defined constraints.
    /// </summary>
    /// <param name="value">The value to evaluate for validity.</param>
    /// <returns>A value indicating that the field value is valid as defined by any constraints inherent or configured for it.</returns>
    /// <remarks>This is a version which is only relevant if validation is only synchronous.</remarks>
    protected virtual bool IsValidInternal(object? value)
    {
        if (value == null)
        {
            return !Required;
        }

        var str = value.ToString();

        if (MinLength.HasValue && (str == null || str.Length < MinLength.Value))
        {
            return false;
        }

        if (MaxLength.HasValue && str?.Length > MaxLength.Value)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(Pattern) && (str == null || !Regex.IsMatch(str, Pattern)))
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public override Task<string> GetReadableFieldTypeAsync(bool verbose, CancellationToken cancellationToken) => Task.FromResult("text");

    /// <inheritdoc/>
    public override bool TryMassageInput(object? input, [MaybeNullWhen(true)] out object? output)
    {
        if (!IsValidInternal(input))
        {
            output = null;
            return false;
        }

        output = input?.ToString();
        return true;
    }
}
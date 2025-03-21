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

using System.Text.Json;
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
    /// <inheritdoc/>
    [JsonPropertyName("type")]
    public override string Type { get; } = "string";

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
        if (value == null)
        {
            return Task.FromResult(!Required);
        }

        var str = value.ToString();

        if (MinLength.HasValue && (str == null || str.Length < MinLength.Value))
        {
            return Task.FromResult(false);
        }

        if (MaxLength.HasValue && str?.Length > MaxLength.Value)
        {
            return Task.FromResult(false);
        }

        if (!string.IsNullOrWhiteSpace(Pattern) && (str == null || !Regex.IsMatch(str, Pattern)))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public static SchemaTextField FromSchemaDefinitionProperty(string name, JsonElement prop, bool required)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (prop.Equals(default(JsonElement)))
        {
            throw new ArgumentException("Default struct value is invalid", nameof(prop));
        }

        var subs = prop.EnumerateObject().ToDictionary(k => k.Name, v => v.Value);
        ushort? minLength = null, maxLength = null;
        string? minLengthString = null, maxLengthString = null, pattern = null;
        if (subs.TryGetValue("minLength", out JsonElement typeMinLength))
        {
            minLengthString = typeMinLength.ToString();
            if (ushort.TryParse(minLengthString, out ushort ml))
            {
                minLength = ml;
            }
        }

        if (subs.TryGetValue("maxLength", out JsonElement typeMaxLength))
        {
            maxLengthString = typeMinLength.ToString();
            if (ushort.TryParse(maxLengthString, out ushort ml))
            {
                maxLength = ml;
            }
        }

        if (subs.TryGetValue("pattern", out JsonElement typePattern))
        {
            pattern = typePattern.ToString();
        }

        return new SchemaTextField(name)
        {
            MinLength = minLength,
            MaxLength = maxLength,
            Pattern = pattern,
            Required = required,
        };
    }

    /// <inheritdoc/>
    public override Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken) => Task.FromResult("text");
}
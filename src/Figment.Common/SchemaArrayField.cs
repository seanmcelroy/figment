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
/// An array field which stores an array of strings.
/// </summary>
/// <param name="Name">Name of the field on a <see cref="Schema"/>.</param>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public class SchemaArrayField(string Name) : SchemaFieldBase(Name)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    /// <summary>
    /// A constant string value representing schema fields of this type.
    /// </summary>
    /// <remarks>
    /// This value is usually encoded into JSON serialized representations of
    /// schema fields and used for polymorphic type indication.
    /// </remarks>
    public const string SCHEMA_FIELD_TYPE = "array";

    /// <inheritdoc/>
    [JsonPropertyName("type")]
    public override string Type { get; } = SCHEMA_FIELD_TYPE;

    /// <summary>
    /// Gets or sets the minimum number of items the array must contain.
    /// </summary>
    [JsonPropertyName("minItems")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ushort? MinItems { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of items the array can contain.
    /// </summary>
    [JsonPropertyName("maxItems")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ushort? MaxItems { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the items in the array must be unique.
    /// </summary>
    [JsonPropertyName("uniqueItems")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? UniqueItems { get; set; }

    /// <summary>
    /// Gets or sets the items in the array.
    /// </summary>
    [JsonPropertyName("items")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SchemaArrayFieldItems? Items { get; set; }

    /// <inheritdoc/>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
    public override Task<bool> IsValidAsync(object? value, CancellationToken _)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
    {
        if (value == null)
        {
            return Task.FromResult(!Required);
        }

        if (value is not System.Collections.IEnumerable items)
        {
            return Task.FromResult(false);
        }

        var itemCount = items == null ? 0 : items.Cast<object>().Count();

        if (MinItems.HasValue && (itemCount < MinItems.Value))
        {
            return Task.FromResult(false);
        }

        if (MaxItems.HasValue && itemCount > MaxItems.Value)
        {
            return Task.FromResult(false);
        }

        if (UniqueItems.HasValue)
        {
            var distinctCount = items == null ? 0 : items.Cast<object>().Distinct().Count();
            if (distinctCount != itemCount)
            {
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public override Task<string> GetReadableFieldTypeAsync(bool verbose, CancellationToken cancellationToken)
    {
        var itemType = Items?.Type ?? "???";
        return Task.FromResult($"array of {itemType}");
    }

    /// <inheritdoc/>
    public override bool TryMassageInput(object? input, [NotNullWhen(true)] out object? output)
    {
        if (input != null
            && input.GetType() != typeof(string)
            && input is System.Collections.IEnumerable ie)
        {
            output = input;
            return true;
        }

        if (input == null || string.IsNullOrWhiteSpace(input.ToString()))
        {
            output = Array.Empty<string>();
            return true;
        }

        if (input is not string && input.GetType().IsArray)
        {
            var arr = new List<string>();
            foreach (var ele in (Array)input)
            {
                if (ele is string s)
                {
                    arr.Add(s);
                }
                else
                {
                    var sx = ele?.ToString();
                    if (!string.IsNullOrWhiteSpace(sx))
                    {
                        arr.Add(sx);
                    }
                }
            }

            output = arr.ToArray();
            return true;
        }

        var prov = input.ToString()!;
        if (prov.StartsWith('[') && prov.EndsWith(']'))
        {
            if (prov.Length == 2)
            {
                output = Array.Empty<string>();
                return true;
            }

            prov = prov[1..^1];
        }

        // TODO: Better support for mixed-quoted csv
        output = prov.Split(',', StringSplitOptions.TrimEntries);
        return true;
    }
}
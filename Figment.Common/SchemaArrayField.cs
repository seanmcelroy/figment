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

namespace Figment.Common;

public class SchemaArrayField(string Name) : SchemaFieldBase(Name)
{
    public const string SCHEMA_FIELD_TYPE = "array";

    public class SchemaArrayFieldItems
    {
        [JsonPropertyName("type")]
        public required string Type { get; set; }
    }

    [JsonPropertyName("type")]
    public override string Type { get; } = SCHEMA_FIELD_TYPE;

    [JsonPropertyName("minItems")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ushort? MinItems { get; set; }

    [JsonPropertyName("maxItems")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ushort? MaxItems { get; set; }

    [JsonPropertyName("uniqueItems")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? UniqueItems { get; set; }

    [JsonPropertyName("items")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SchemaArrayFieldItems? Items { get; set; }

    public override Task<bool> IsValidAsync(object? value, CancellationToken _)
    {
        if (!Required && value == null)
            return Task.FromResult(true);
        if (Required && value == null)
            return Task.FromResult(false);

        if (value is not System.Collections.IEnumerable items)
            return Task.FromResult(false);

        var itemCount = items == null ? 0 : items.Cast<object>().Count();

        if (MinItems.HasValue && (itemCount < MinItems.Value))
            return Task.FromResult(false);
        if (MaxItems.HasValue && itemCount > MaxItems.Value)
            return Task.FromResult(false);

        if (UniqueItems.HasValue)
        {
            var distinctCount = items == null ? 0 : items.Cast<object>().Distinct().Count();
            if (distinctCount != itemCount)
                return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public static SchemaArrayField FromSchemaDefinitionProperty(
        string name,
        JsonElement prop,
        bool required)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (prop.Equals(default(JsonElement)))
            throw new ArgumentException("Default struct value is invalid", nameof(prop));

        var subs = prop.EnumerateObject().ToDictionary(k => k.Name, v => v.Value);
        ushort? minItems = null, maxItems = null;
        string? minItemsString = null, maxItemsString = null, uniqueItemString;
        bool? uniqueItems = null;
        if (subs.TryGetValue("minItems", out JsonElement typeMinItems))
        {
            minItemsString = typeMinItems.ToString();
            if (ushort.TryParse(minItemsString, out ushort ml))
            {
                minItems = ml;
            }
        }
        if (subs.TryGetValue("maxItems", out JsonElement typeMaxItems))
        {
            maxItemsString = typeMaxItems.ToString();
            if (ushort.TryParse(maxItemsString, out ushort ml))
            {
                maxItems = ml;
            }
        }
        if (subs.TryGetValue("uniqueItems", out JsonElement typeUniqueItems))
        {
            uniqueItemString = typeUniqueItems.ToString();
            if (bool.TryParse(uniqueItemString, out bool ui))
            {
                uniqueItems = ui;
            }
        }

        var f = new SchemaArrayField(name)
        {
            MinItems = minItems,
            MaxItems = maxItems,
            UniqueItems = uniqueItems,
            Required = required
        };
        return f;
    }

    public override Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken)
    {
        var itemType = Items?.Type ?? "???";
        return Task.FromResult($"array of {itemType}");
    }

    public override bool TryMassageInput(object? input, out object? output)
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
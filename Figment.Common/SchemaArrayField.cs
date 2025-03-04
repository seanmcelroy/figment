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
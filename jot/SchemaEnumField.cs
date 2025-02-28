using System.Text.Json;
using System.Text.Json.Serialization;

namespace Figment;

public class SchemaEnumField(string Name, object?[] Values) : SchemaFieldBase(Name)
{
    [JsonIgnore] // Only for enums.
    public override string Type { get; } = "enum";

    [JsonPropertyName("enum")]
    public object?[] Values { get; set; } = Values;

    public override Task<bool> IsValidAsync(object? value, CancellationToken _)
    {
        if (!Required && value == null)
            return Task.FromResult(true);
        if (Required && value == null)
            return Task.FromResult(false);

        if (value is string s)
            return Task.FromResult(Values.OfType<JsonElement>()
                .Where(v => v.ValueKind == JsonValueKind.String)
                .Any(v => string.CompareOrdinal(v.GetString(), s) == 0)
                || Values.OfType<string>()
                .Any(v => string.CompareOrdinal(v, s) == 0));

        if (value is int i)
            return Task.FromResult(Values.OfType<JsonElement>()
                .Where(v => v.ValueKind == JsonValueKind.Number)
                .Any(v => v.GetInt64() == i)
                || Values.OfType<int>().Any(v => v == i));

        if (value is double d)
            return Task.FromResult(Values.OfType<JsonElement>()
                .Where(v => v.ValueKind == JsonValueKind.Number)
                .Any(v => Math.Abs(v.GetDouble() - d) < double.Epsilon)
                || Values.OfType<double>().Any(v => Math.Abs(v - d) < double.Epsilon));

        if (value is bool b)
            return Task.FromResult(Values.OfType<JsonElement>()
                .Any(v => (v.ValueKind == JsonValueKind.True && b)
                    || (v.ValueKind == JsonValueKind.False && !b))
                || Values.OfType<bool>().Any(v => v == b));

        if (value == null)
            return Task.FromResult(Values.OfType<JsonElement>()
                .Any(v => v.ValueKind == JsonValueKind.Null)
            || Values.Any(v => v == null));

        throw new InvalidOperationException();
    }

    public static SchemaEnumField FromToSchemaDefinitionProperty(string name, JsonElement prop, bool required)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (prop.Equals(default(JsonElement)))
            throw new ArgumentException("Default struct value is invalid", nameof(prop));

        var subs = prop.EnumerateObject().ToDictionary(k => k.Name, v => v.Value);
        List<object?> vals = [];
        if (subs.TryGetValue("enum", out JsonElement typeEnum))
        {
            foreach (var element in typeEnum.EnumerateArray())
            {
                switch (element.ValueKind)
                {
                    case JsonValueKind.String:
                        vals.Add(element.GetString());
                        continue;
                    case JsonValueKind.True:
                        vals.Add(true);
                        continue;
                    case JsonValueKind.False:
                        vals.Add(false);
                        continue;
                    case JsonValueKind.Null:
                        vals.Add(null);
                        continue; // We don't load nulls
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        var f = new SchemaEnumField(name, [.. vals])
        {
            Required = required
        };
        return f;
    }

    public override Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken)
    {
        if (Values == null || Values.Length == 0)
            return Task.FromResult(Spectre.Console.Markup.Escape("enum []"));
        var fields = Values.Select(v => v?.ToString() ?? "null").Aggregate((c, n) => $"{c},{n}");
        return Task.FromResult(Spectre.Console.Markup.Escape($"enum [{fields}]"));
    }
}
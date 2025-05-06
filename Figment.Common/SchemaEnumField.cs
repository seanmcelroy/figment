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

/// <summary>
/// A schema field which contains one of a defined set of values.
/// </summary>
/// <param name="Name">Name of the field on a <see cref="Schema"/>.</param>
/// <param name="Values">The allowable values for the enum.</param>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public class SchemaEnumField(string Name, object?[] Values) : SchemaFieldBase(Name)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    /// <summary>
    /// A constant string value representing schema fields of this type.
    /// </summary>
    /// <remarks>
    /// This value is usually encoded into JSON serialized representations of
    /// schema fields and used for polymorphic type indication.
    /// </remarks>
    public const string SCHEMA_FIELD_TYPE = "enum";

    /// <inheritdoc/>
    [JsonIgnore] // Only for enums.
    public override string Type { get; } = SCHEMA_FIELD_TYPE;

    /// <summary>
    /// Gets or sets the allowable values for the enum.
    /// </summary>
    [JsonPropertyName("enum")]
    public object?[] Values { get; set; } = Values;

    /// <inheritdoc/>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
    public override Task<bool> IsValidAsync(object? value, CancellationToken _)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
    {
        if (value == null)
        {
            return Task.FromResult(!Required);
        }

        if (value is string s)
        {
            return Task.FromResult(Values.OfType<JsonElement>()
                .Any(v => v.ValueKind == JsonValueKind.String && string.Equals(v.GetString(), s, StringComparison.Ordinal))
                || Values.OfType<string>()
                .Any(v => string.Equals(v, s, StringComparison.Ordinal)));
        }

        if (value is int i)
        {
            return Task.FromResult(Values.OfType<JsonElement>()
                .Any(v => v.ValueKind == JsonValueKind.Number && v.GetInt64() == i)
                || Values.OfType<int>().Any(v => v == i));
        }

        if (value is double d)
        {
            return Task.FromResult(Values.OfType<JsonElement>()
                .Any(v => v.ValueKind == JsonValueKind.Number && Math.Abs(v.GetDouble() - d) < double.Epsilon)
                || Values.OfType<double>().Any(v => Math.Abs(v - d) < double.Epsilon));
        }

        if (value is bool b)
        {
            return Task.FromResult(Values.OfType<JsonElement>()
                .Any(v => (v.ValueKind == JsonValueKind.True && b)
                    || (v.ValueKind == JsonValueKind.False && !b))
                || Values.OfType<bool>().Any(v => v == b));
        }

        if (value == null)
        {
            return Task.FromResult(Values.OfType<JsonElement>()
                .Any(v => v.ValueKind == JsonValueKind.Null)
            || Values.Any(v => v == null));
        }

        throw new InvalidOperationException();
    }

    public static SchemaEnumField FromToSchemaDefinitionProperty(string name, JsonElement prop, bool required)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (prop.Equals(default(JsonElement)))
        {
            throw new ArgumentException("Default struct value is invalid", nameof(prop));
        }

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
                        throw new NotSupportedException($"Unsupported element value kind '{element.ValueKind}'");
                }
            }
        }

        return new SchemaEnumField(name, [.. vals])
        {
            Required = required,
        };
    }

    /// <inheritdoc/>
    public override Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken)
    {
        if (Values == null || Values.Length == 0)
        {
            return Task.FromResult("enum []");
        }

        var fields = Values.Select(v => v?.ToString() ?? "null").Aggregate((c, n) => $"{c},{n}");
        return Task.FromResult($"enum [{fields}]");
    }
}
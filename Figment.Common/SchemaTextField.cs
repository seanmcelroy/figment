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

public class SchemaTextField(string Name) : SchemaFieldBase(Name)
{
    [JsonPropertyName("type")]
    public override string Type { get; } = "string";

    [JsonPropertyName("minLength")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ushort? MinLength { get; set; }

    [JsonPropertyName("maxLength")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ushort? MaxLength { get; set; }

    [JsonPropertyName("pattern")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual string? Pattern { get; set; }

    public override Task<bool> IsValidAsync(object? value, CancellationToken _)
    {
        if (!Required && value == null)
            return Task.FromResult(true);
        if (Required && value == null)
            return Task.FromResult(false);

        var str = value as string;

        if (MinLength.HasValue && (str == null || str.Length < MinLength.Value))
            return Task.FromResult(false);
        if (MaxLength.HasValue && str?.Length > MaxLength.Value)
            return Task.FromResult(false);
        if (!string.IsNullOrWhiteSpace(Pattern) && (str == null || !Regex.IsMatch(str, Pattern)))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

    public static SchemaTextField FromSchemaDefinitionProperty(string name, JsonElement prop, bool required)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (prop.Equals(default(JsonElement)))
            throw new ArgumentException("Default struct value is invalid", nameof(prop));

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

        var f = new SchemaTextField(name)
        {
            MinLength = minLength,
            MaxLength = maxLength,
            Pattern = pattern,
            Required = required
        };
        return f;
    }

    public override Task<string> GetReadableFieldTypeAsync(bool _, CancellationToken cancellationToken) => Task.FromResult("text");
}
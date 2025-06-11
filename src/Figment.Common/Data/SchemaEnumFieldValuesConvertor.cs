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

namespace Figment.Common.Data;

/// <summary>
/// Json value convertor that handles mixed-type enum allowable values in <see cref="SchemaEnumField.Values"/>.
/// </summary>
#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
public class SchemaEnumFieldValuesConvertor : JsonConverter<object?[]?>
#pragma warning restore SA1011 // Closing square brackets should be spaced correctly
{
    /// <inheritdoc/>
#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
    public override object?[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
#pragma warning restore SA1011 // Closing square brackets should be spaced correctly
    {
        List<object?> objs = [];
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartArray:
                    objs = [];
                    break;
                case JsonTokenType.EndArray:
                    return [.. objs];
                case JsonTokenType.Null:
                    objs.Add(null);
                    break;
                case JsonTokenType.String:
                    objs.Add(reader.GetString());
                    break;
                case JsonTokenType.Number:
                    if (reader.TryGetInt32(out int i))
                    {
                        objs.Add(i);
                    }
                    else if (reader.TryGetUInt64(out ulong u))
                    {
                        objs.Add(u);
                    }
                    else if (reader.TryGetDouble(out double d))
                    {
                        objs.Add(d);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unsupported number type in enum!");
                    }

                    break;
                case JsonTokenType.True:
                    objs.Add(true);
                    break;
                case JsonTokenType.False:
                    objs.Add(false);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported token type: {Enum.GetName(reader.TokenType)}");
            }
        }

        return null;
    }

    /// <inheritdoc/>
#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
    public override void Write(Utf8JsonWriter writer, object?[]? value, JsonSerializerOptions options)
#pragma warning restore SA1011 // Closing square brackets should be spaced correctly
    {
        if (value == null)
        {
            return;
        }

        writer.WriteStartArray();
        foreach (var v in value)
        {
            switch (v)
            {
                case null:
                    writer.WriteNullValue();
                    break;
                case string s:
                    writer.WriteStringValue(s);
                    break;
                case ulong u:
                    writer.WriteNumberValue(u);
                    break;
                case int i:
                    writer.WriteNumberValue(i);
                    break;
                case double d:
                    writer.WriteNumberValue(d);
                    break;
                case bool b:
                    writer.WriteBooleanValue(b);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown type {v.GetType().Name}");
            }
        }

        writer.WriteEndArray();
    }
}
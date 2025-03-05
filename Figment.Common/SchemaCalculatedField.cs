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
using Figment.Common.Calculations;

namespace Figment.Common;

public class SchemaCalculatedField(string Name) : SchemaFieldBase(Name)
{
    public const string SCHEMA_FIELD_TYPE = "calculated";

    [JsonPropertyName("type")]
    public override string Type { get; } = SCHEMA_FIELD_TYPE;

    [JsonPropertyName("formula")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Formula { get; set; }

    public override Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken) => Task.FromResult($"calculated: {Formula}");

    public override Task<bool> IsValidAsync(object? value, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Formula))
            return Task.FromResult(false);

        var (success, _, _) = Parser.ParseFormula(Formula);
        return Task.FromResult(success);
    }
}
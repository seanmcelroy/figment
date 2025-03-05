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

namespace Figment.Common;

[JsonPolymorphic]
[JsonDerivedType(typeof(SchemaArrayField), typeDiscriminator: SchemaArrayField.SCHEMA_FIELD_TYPE)]
[JsonDerivedType(typeof(SchemaBooleanField), typeDiscriminator: SchemaBooleanField.SCHEMA_FIELD_TYPE)]
[JsonDerivedType(typeof(SchemaCalculatedField), typeDiscriminator: SchemaCalculatedField.SCHEMA_FIELD_TYPE)]
[JsonDerivedType(typeof(SchemaDateField), typeDiscriminator: SchemaDateField.SCHEMA_FIELD_TYPE)]
[JsonDerivedType(typeof(SchemaEmailField), typeDiscriminator: SchemaEmailField.SCHEMA_FIELD_TYPE)]
[JsonDerivedType(typeof(SchemaEnumField), typeDiscriminator: "enum")]
[JsonDerivedType(typeof(SchemaIntegerField), typeDiscriminator: SchemaIntegerField.SCHEMA_FIELD_TYPE)]
[JsonDerivedType(typeof(SchemaNumberField), typeDiscriminator: SchemaNumberField.SCHEMA_FIELD_TYPE)]
[JsonDerivedType(typeof(SchemaPhoneField), typeDiscriminator: SchemaPhoneField.SCHEMA_FIELD_TYPE)]
[JsonDerivedType(typeof(SchemaRefField), typeDiscriminator: SchemaRefField.SCHEMA_FIELD_TYPE)]
[JsonDerivedType(typeof(SchemaSchemaField), typeDiscriminator: SchemaSchemaField.SCHEMA_FIELD_TYPE)]
[JsonDerivedType(typeof(SchemaTextField), typeDiscriminator: "text")]
[JsonDerivedType(typeof(SchemaUriField), typeDiscriminator: SchemaUriField.SCHEMA_FIELD_TYPE)]
public abstract class SchemaFieldBase(string Name)
{
    [JsonPropertyName("type")]
    public abstract string Type { get; }

    [JsonIgnore]
    public string Name { get; init; } = Name;

    [JsonIgnore]
    public bool Required { get; set; }

    /// <summary>
    /// Validates a parsed field meets all applicable optionally-defined constraints
    /// </summary>
    /// <returns></returns>
    public abstract Task<bool> IsValidAsync(object? value, CancellationToken cancellationToken);

    public abstract Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Allows the field to modify the input to match its underlying requirement
    /// </summary>
    /// <param name="input">The original input value, usually a string</param>
    /// <param name="output">The massaged input value formatted as the field prefers it to be provided</param>
    /// <returns>Whether the value could be massaged or did not need to be massaged.  Otherwise, false.</returns>
    public virtual bool TryMassageInput(object? input, out object? output) {
        output = input;
        return true;
    }
}
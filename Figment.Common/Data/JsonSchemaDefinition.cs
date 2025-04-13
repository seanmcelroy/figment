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

namespace Figment.Common.Data;

/// <summary>
/// This is the definition of a <see cref="Figment.Schema"/> when it is persisted
/// to local storage, such as via <see cref="LocalDirectoryStorageProvider"/>
/// </summary>
/// <param name="Guid">The unique identiifer of the schema</param>
/// <param name="Name">The name of the schema</param>
/// <param name="Description">A description for the schema</param>
/// <param name="Plural">The plural word for the schema, which should be the plural form of the value provided in <paramref name="Name"/></param>
/// <param name="VersionGuid">The versioning plan for this schema, if the schema is versioned.</param>
[method: JsonConstructor]
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record JsonSchemaDefinition(string Guid, string Name, string? Description, string? Plural, string? VersionGuid)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    /// <summary>
    /// Gets the Json schema $schema metadata property.
    /// </summary>
    [JsonPropertyName("$schema")]
    public string Schema { get; init; } = "https://json-schema.org/draft/2020-12/schema";

    /// <summary>
    /// Gets or sets the Json schema $id metadata property.
    /// </summary>
    [JsonPropertyName("$id")]
    public string Id
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Guid))
            {
                return "???";
            }

            return $"https://figment.seanmcelroy.com/{Guid}.schema.json";
        }
        set
        {
            if (value.StartsWith("https://figment.seanmcelroy.com/", StringComparison.OrdinalIgnoreCase)
            && value.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase))
            {
                Guid = value[32..^12];
            }
        }
    }

    /// <summary>
    /// Gets or sets the name of the schema.
    /// </summary>
    [JsonPropertyName("title")]
    public string Name { get; set; } = Name;

    /// <summary>
    /// Gets or sets the description of the schema.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; } = Description;

    /// <summary>
    /// Gets or sets the plural form of <see cref="Name"/>, which is used by some command interfaces that modify entities.
    /// </summary>
    [JsonPropertyName("$plural")]
    public string? Plural { get; set; } = Plural;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";

    [JsonPropertyName("required")]
    public string[]? RequiredProperties { get; set; }

    /// <summary>
    /// Gets or sets the list of fields, keyed by name, defined for this schema.
    /// </summary>
    [JsonPropertyName("properties")]
    public Dictionary<string, SchemaFieldBase> Properties { get; set; } = [];

    /// <summary>
    /// Gets or sets the versioning plan for this schema, if the schema is versioned.
    /// </summary>
    [JsonPropertyName("versionId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VersionGuid { get; set; } = VersionGuid;

    /// <summary>
    /// Gets or sets the list of import maps that define how to import external records into new things of this schema type.
    /// </summary>
    [JsonPropertyName("importMaps")]
    public List<SchemaImportMap> ImportMaps { get; set; } = [];

    /// <summary>
    /// Gets or sets the globally unique identifier for the schema.
    /// </summary>
    [JsonIgnore]
    public string Guid { get; set; } = Guid;

    public JsonSchemaDefinition(Schema schema)
        : this(schema.Guid, schema.Name, schema.Description, schema.Plural, schema.VersionGuid)
    {
        Description = schema.Description;
        Plural = schema.Plural;
        RequiredProperties = [.. schema.Properties.Where(sp => sp.Value.Required).Select(sp => sp.Key)];
        VersionGuid = schema.VersionGuid;

        foreach (var prop in schema.Properties)
        {
            Properties.Add(prop.Key, prop.Value);
        }

        ImportMaps = schema.ImportMaps;
    }

    public Schema ToSchema()
    {
        var schema = new Schema(Guid, Name)
        {
            // Optional built-ins
            Description = Description,
            Plural = Plural,
            VersionGuid = VersionGuid,
            CreatedOn = DateTime.UnixEpoch,
            LastModified = DateTime.UnixEpoch,
            LastAccessed = DateTime.UnixEpoch,
        };

        foreach (var prop in Properties)
        {
            prop.Value.Required = RequiredProperties?.Any(sdr => string.CompareOrdinal(sdr, prop.Key) == 0) == true;
            schema.Properties.Add(prop.Key, prop.Value);
        }

        schema.ImportMaps.AddRange(ImportMaps);

        return schema;
    }

    public Schema ToSchema(FileInfo schemaFileInfo)
    {
        var schema = new Schema(Guid, Name)
        {
            // Optional built-ins
            Description = Description,
            Plural = Plural,
            VersionGuid = VersionGuid,
            CreatedOn = schemaFileInfo.CreationTimeUtc,
            LastModified = schemaFileInfo.LastWriteTimeUtc,
            LastAccessed = schemaFileInfo.LastAccessTimeUtc,
        };

        foreach (var prop in Properties)
        {
            prop.Value.Required = RequiredProperties?.Any(sdr => string.CompareOrdinal(sdr, prop.Key) == 0) == true;
            schema.Properties.Add(prop.Key, prop.Value);
        }

        schema.ImportMaps.AddRange(ImportMaps);

        return schema;
    }
}
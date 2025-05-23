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
using Figment.Common.Data;

namespace Figment.Common;

/// <summary>
/// This field is a reference to a thing in a given Schema.
/// </summary>
/// <param name="Name">The name of the field.</param>
/// <param name="SchemaGuid">The schema to which the thing (whose guid is the value of this field) must adhere.</param>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public class SchemaRefField(string Name, string SchemaGuid) : SchemaFieldBase(Name)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    /// <summary>
    /// A constant string value representing schema fields of this type.
    /// </summary>
    /// <remarks>
    /// This value is usually encoded into JSON serialized representations of
    /// schema fields and used for polymorphic type indication.
    /// </remarks>
    public const string SCHEMA_FIELD_TYPE = "ref";

    /// <inheritdoc/>
    [JsonIgnore] // Only for enums.
    public override string Type { get; } = SCHEMA_FIELD_TYPE;

    /// <summary>
    /// Gets or sets the <see cref="Schema"/> to which this field refers.
    /// </summary>
    [JsonIgnore]
    public string SchemaGuid { get; set; } = SchemaGuid;

    /// <summary>
    /// Gets or sets the identifier of the <see cref="Schema"/> to which this field refers.
    /// </summary>
    /// <remarks>
    /// Due to limitations of the .NET 9 core framework, this cannot be a
    /// $-meta property due to the polymorphism of <see cref="SchemaFieldBase"/>.
    /// </remarks>
    [JsonPropertyName("ref")] // TODO: Someday, make this a $ meta property
    public string Id
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SchemaGuid))
            {
                return "???";
            }

            return $"https://figment.seanmcelroy.com/{SchemaGuid}.schema.json";
        }
        set
        {
            if (value.StartsWith("https://figment.seanmcelroy.com/", StringComparison.OrdinalIgnoreCase)
            && value.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase))
            {
                SchemaGuid = value[32..^12];
            }
        }
    }

    /// <inheritdoc/>
    public override async Task<bool> IsValidAsync(object? value, CancellationToken cancellationToken)
    {
        if (value == null)
        {
            return !Required;
        }

        var ssp = AmbientStorageContext.StorageProvider?.GetSchemaStorageProvider();
        if (ssp == null)
        {
            return false;
        }

        if (!await ssp.GuidExists(SchemaGuid, cancellationToken))
        {
            return false;
        }

        var tsp = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (tsp == null)
        {
            return false;
        }

        if (value is string s && (string.IsNullOrWhiteSpace(s) || !await tsp.GuidExists(s, cancellationToken)))
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public override async Task<string> GetReadableFieldTypeAsync(CancellationToken cancellationToken)
    {
        var provider = AmbientStorageContext.StorageProvider?.GetSchemaStorageProvider();
        if (provider == null)
        {
            return "???";
        }

        if (!await provider.GuidExists(SchemaGuid, cancellationToken))
        {
            return "???";
        }

        var schemaLoaded = await provider.LoadAsync(SchemaGuid, cancellationToken);
        if (schemaLoaded == null)
        {
            return "???";
        }

        return $"{schemaLoaded.Name} ({schemaLoaded.Guid})";
    }
}
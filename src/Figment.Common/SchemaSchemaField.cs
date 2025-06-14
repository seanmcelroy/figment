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
/// This field is a reference to another <see cref="Schema"/> itself,
/// as opposed to <see cref="SchemaRefField"/>, which is a reference to
/// things in a given Schema.</summary>
/// <param name="Name">The name of the field.</param>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public class SchemaSchemaField(string Name) : SchemaTextField(Name)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    /// <summary>
    /// A constant string value representing schema fields of this type.
    /// </summary>
    /// <remarks>
    /// This value is usually encoded into JSON serialized representations of
    /// schema fields and used for polymorphic type indication.
    /// </remarks>
    public new const string SCHEMA_FIELD_TYPE = "schema";

    /// <inheritdoc/>
    [JsonPropertyName("type")]
    public override string Type { get; } = SCHEMA_FIELD_TYPE;

    /// <inheritdoc/>
    public override Task<string> GetReadableFieldTypeAsync(bool verbose, CancellationToken cancellationToken) => Task.FromResult(SCHEMA_FIELD_TYPE);

    /// <inheritdoc/>
    public override async Task<bool> IsValidAsync(object? value, CancellationToken cancellationToken)
    {
        if (!await base.IsValidAsync(value, cancellationToken))
        {
            return false;
        }

        var str = value as string;
        if (string.IsNullOrWhiteSpace(str))
        {
            return false;
        }

        var ssp = AmbientStorageContext.StorageProvider?.GetSchemaStorageProvider();
        if (ssp == null)
        {
            return true; // Assume.
        }

        return await ssp.GuidExists(str, cancellationToken);
    }
}
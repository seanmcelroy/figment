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

/// <summary>
/// A text field representing a URI.
/// </summary>
/// <param name="Name">Name of the field on a <see cref="Schema"/>.</param>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public class SchemaUriField(string Name) : SchemaTextField(Name)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    /// <summary>
    /// A constant string value representing schema fields of this type.
    /// </summary>
    /// <remarks>
    /// This value is usually encoded into JSON serialized representations of
    /// schema fields and used for polymorphic type indication.
    /// </remarks>
    public new const string SCHEMA_FIELD_TYPE = "uri";

    /// <inheritdoc/>
    [JsonPropertyName("type")]
    public override string Type { get; } = "string"; // SCHEMA_FIELD_TYPE does not match JSON schema

    /// <summary>
    /// Gets the format of this string field.
    /// </summary>
    /// <remarks>
    /// Because URIs are serialized in JSON as strings with format 'uri', the value of this field
    /// is always <![CDATA[uri]]>.
    /// </remarks>
    [JsonPropertyName("format")]
    public string Format { get; } = "uri";

    /// <inheritdoc/>
    [JsonPropertyName("pattern")]
    public override string? Pattern { get; set; } = @"[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)";

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
        return Uri.TryCreate(str, UriKind.RelativeOrAbsolute, out Uri? _);
    }
}
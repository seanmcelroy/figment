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

using System.Text;
using System.Text.Json;
using Figment.Common.Errors;

namespace Figment.Common.Data;

/// <summary>
/// An optional base provider that provides some common methods for <see cref="ISchemaStorageProvider"/> implementations.
/// </summary>
public abstract class SchemaStorageProviderBase : ISchemaStorageProvider
{
    /// <summary>
    /// Gets the Json serialization options to use when serializing and deserializing content.
    /// </summary>
    protected static JsonSerializerOptions SerializerOptions { get; } = new()
    {
        // Required for $ref properties with type descriminator
        AllowOutOfOrderMetadataProperties = true,
#if DEBUG
        WriteIndented = true,
#endif
    };

    /// <inheritdoc/>
    public abstract Task<CreateSchemaResult> CreateAsync(string schemaName, CancellationToken cancellationToken);

    /// <inheritdoc/>
    public abstract Task<bool> DeleteAsync(string schemaGuid, CancellationToken cancellationToken);

    /// <inheritdoc/>
    public abstract Task<Reference> FindByNameAsync(string schemaName, CancellationToken cancellationToken);

    /// <inheritdoc/>
    public abstract IAsyncEnumerable<PossibleNameMatch> FindByPartialNameAsync(string schemaNamePart, CancellationToken cancellationToken);

    /// <inheritdoc/>
    public abstract IAsyncEnumerable<Reference> FindByPluralNameAsync(string plural, CancellationToken cancellationToken);

    /// <inheritdoc/>
    public abstract IAsyncEnumerable<PossibleNameMatch> GetAll(CancellationToken cancellationToken);

    /// <inheritdoc/>
    public abstract IAsyncEnumerable<Schema> LoadAll(CancellationToken cancellationToken);

    /// <inheritdoc/>
    public abstract Task<bool> GuidExists(string schemaGuid, CancellationToken cancellationToken);

    /// <inheritdoc/>
    public abstract Task<Schema?> LoadAsync(string schemaGuid, CancellationToken cancellationToken);

    /// <summary>
    /// Loads schema from a serialized Json string.
    /// </summary>
    /// <param name="content">Json string to deserialize into a <see cref="Schema"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserliazed schema, or null if there was a serialization error.</returns>
    public async Task<Schema?> LoadJsonContentAsync(string content, CancellationToken cancellationToken)
    {
        await using var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));

        try
        {
            var schemaDefinition = await JsonSerializer.DeserializeAsync<JsonSchemaDefinition>(ms, SerializerOptions, cancellationToken);
            if (schemaDefinition == null)
            {
                AmbientErrorContext.Provider.LogError("Unable to deserialize schema from content");
                return null;
            }

            var schema = schemaDefinition.ToSchema();

            // Set name fields.
            foreach (var prop in schema.Properties)
            {
                prop.Value.Name = prop.Key;
            }

            return schema;
        }
        catch (JsonException je)
        {
            AmbientErrorContext.Provider.LogException(je, "Unable to deserialize schema from content string");
            return null;
        }
    }

    /// <inheritdoc/>
    public abstract Task<bool> RebuildIndexes(CancellationToken cancellationToken);

    /// <inheritdoc/>
    public abstract Task<(bool success, string? message)> SaveAsync(Schema schema, CancellationToken cancellationToken);
}
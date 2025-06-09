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

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Figment.Common;
using Figment.Common.Data;

namespace Figment.Data.Memory;

/// <summary>
/// A <see cref="Schema"/> storage provider implementation that stores objects in memory.
/// </summary>
public class MemorySchemaStorageProvider : SchemaStorageProviderBase, ISchemaStorageProvider
{
    private static readonly ConcurrentDictionary<string, Schema> SchemaCache = [];

    /// <inheritdoc/>
    public override Task<CreateSchemaResult> CreateAsync(string schemaName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);

        var schemaGuid = Guid.NewGuid().ToString();
        var schema = new Schema(schemaGuid, schemaName);
        var added = SchemaCache.TryAdd(schemaGuid, schema);
        if (!added)
        {
            return Task.FromResult(new CreateSchemaResult { Success = false });
        }

        return Task.FromResult(new CreateSchemaResult { Success = true, NewGuid = schemaGuid });
    }

    /// <inheritdoc/>
    public override Task<bool> DeleteAsync(string schemaGuid, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaGuid);

        var removed = SchemaCache.TryRemove(schemaGuid, out Schema? _);
        return Task.FromResult(removed);
    }

    /// <inheritdoc/>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public override async IAsyncEnumerable<PossibleNameMatch> GetAll([EnumeratorCancellation] CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        foreach (var schema in SchemaCache.Values)
        {
            yield return new PossibleNameMatch
            {
                Reference = new()
                {
                    Guid = schema.Guid,
                    Type = Reference.ReferenceType.Schema
                },
                Name = schema?.Name ?? "<UNDEFINED>"
            };
        }
    }

    /// <inheritdoc/>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public override async IAsyncEnumerable<Schema> LoadAll([EnumeratorCancellation] CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        foreach (var schema in SchemaCache.Values)
        {
            yield return schema;
        }
    }

    /// <inheritdoc/>
    public override Task<Schema?> LoadAsync(string schemaGuid, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaGuid);

        _ = SchemaCache.TryGetValue(schemaGuid, out Schema? schema);
        return Task.FromResult(schema);
    }

    /// <inheritdoc/>
    public override Task<(bool success, string? message)> SaveAsync(Schema schema, CancellationToken cancellationToken)
    {
        SchemaCache[schema.Guid] = schema;
        return Task.FromResult<(bool, string?)>((true, $"Schema {schema.Name} saved."));
    }

    /// <inheritdoc/>
    public override Task<bool> RebuildIndexes(CancellationToken cancellationToken) => Task.FromResult(true);

    /// <inheritdoc/>
    public override Task<Reference> FindByNameAsync(string schemaName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);

        foreach (var schema in SchemaCache.Values.Where(e => string.Equals(e.Name, schemaName, StringComparison.CurrentCultureIgnoreCase)))
            return Task.FromResult(new Reference
            {
                Guid = schema.Guid,
                Type = Reference.ReferenceType.Schema
            });

        return Task.FromResult(Reference.EMPTY);
    }

    /// <inheritdoc/>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public override async IAsyncEnumerable<PossibleNameMatch> FindByPartialNameAsync(string schemaNamePart, [EnumeratorCancellation] CancellationToken _)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaNamePart);

        foreach (var schema in SchemaCache.Values.Where(e => e.Name.StartsWith(schemaNamePart, StringComparison.CurrentCultureIgnoreCase)))
        {
            yield return new PossibleNameMatch
            {
                Name = schema.Name,
                Reference = new Reference
                {
                    Type = Reference.ReferenceType.Schema,
                    Guid = schema.Guid
                },
            };
        }
    }

    /// <inheritdoc/>
    public override Task<bool> GuidExists(string schemaGuid, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaGuid);
        return Task.FromResult(SchemaCache.ContainsKey(schemaGuid));
    }

    /// <inheritdoc/>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public override async IAsyncEnumerable<Reference> FindByPluralNameAsync(string plural, [EnumeratorCancellation] CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plural);


        foreach (var schema in SchemaCache.Values.Where(e => string.Equals(e.Plural, plural, StringComparison.CurrentCultureIgnoreCase)))
        {
            yield return new Reference
            {
                Type = Reference.ReferenceType.Schema,
                Guid = schema.Guid
            };
        }
    }
}

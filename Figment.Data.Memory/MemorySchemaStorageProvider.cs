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

using System.Runtime.CompilerServices;
using Figment.Common;
using Figment.Common.Data;

namespace Figment.Data.Memory;

public class MemorySchemaStorageProvider : SchemaStorageProviderBase, ISchemaStorageProvider
{
    private static readonly Dictionary<string, Schema> SchemaCache = [];

    public Task<CreateSchemaResult> CreateAsync(string schemaName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);

        var schemaGuid = Guid.NewGuid().ToString();
        var schema = new Schema(schemaGuid, schemaName);
        SchemaCache.Add(schemaGuid, schema);
        var csr = new CreateSchemaResult { Success = true, NewGuid = schemaGuid };
        return Task.FromResult(csr);
    }

    public Task<bool> DeleteAsync(string schemaGuid, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaGuid);
        return Task.FromResult(SchemaCache.Remove(schemaGuid));
    }

    /// <summary>
    /// Gets all schemas
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to abort the enumerator</param>
    /// <returns>Each schema</returns>
    /// <remarks>This may be a very expensive operation</remarks>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async IAsyncEnumerable<PossibleNameMatch> GetAll([EnumeratorCancellation] CancellationToken cancellationToken)
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

    public Task<Schema?> LoadAsync(string schemaGuid, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaGuid);

        _ = SchemaCache.TryGetValue(schemaGuid, out Schema? schema);
        return Task.FromResult(schema);
    }

    public Task<bool> SaveAsync(Schema schema, CancellationToken cancellationToken)
    {
        SchemaCache[schema.Guid] = schema;
        return Task.FromResult(true);
    }

    public Task<bool> RebuildIndexes(CancellationToken cancellationToken) => Task.FromResult(true);

    public Task<Reference> FindByNameAsync(string schemaName, CancellationToken cancellationToken)
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

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable CA1822 // Mark members as static
    private async IAsyncEnumerable<(Reference reference, string name)> FindByNameAsync(Func<string, bool> nameSelector, [EnumeratorCancellation] CancellationToken cancellationToken)
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        ArgumentNullException.ThrowIfNull(nameSelector);

        foreach (var schema in SchemaCache.Values.Where(e => nameSelector(e.Name)))
        {
            if (schema != null)
                yield return (new Reference
                {
                    Guid = schema.Guid,
                    Type = Reference.ReferenceType.Schema
                }, schema.Name);
        }

        yield break;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async IAsyncEnumerable<PossibleNameMatch> FindByPartialNameAsync(string schemaNamePart, [EnumeratorCancellation] CancellationToken _)
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

    public Task<bool> GuidExists(string schemaGuid, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaGuid);
        return Task.FromResult(SchemaCache.ContainsKey(schemaGuid));
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async IAsyncEnumerable<Reference> FindByPluralNameAsync(string plural, [EnumeratorCancellation] CancellationToken cancellationToken)
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

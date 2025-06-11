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

namespace Figment.Common.Data;

/// <summary>
/// An optional base provider that provides some common methods for <see cref="ISchemaStorageProvider"/> implementations.
/// </summary>
public abstract class SchemaStorageProviderBase : ISchemaStorageProvider
{
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

    /// <inheritdoc/>
    public abstract Task<bool> RebuildIndexes(CancellationToken cancellationToken);

    /// <inheritdoc/>
    public abstract Task<(bool success, string? message)> SaveAsync(Schema schema, CancellationToken cancellationToken);
}
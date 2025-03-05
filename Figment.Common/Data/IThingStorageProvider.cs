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

public interface IThingStorageProvider
{
    public Task<(bool, Thing?)> AssociateWithSchemaAsync(string thingGuid, string schemaGuid, CancellationToken cancellationToken);

    public Task<Thing?> CreateAsync(string? schemaGuid, string thingName, CancellationToken cancellationToken);

    public Task<bool> DeleteAsync(string schemaGuid, CancellationToken cancellationToken);

    public Task<(bool, Thing?)> DissociateFromSchemaAsync(string thingGuid, string schemaGuid, CancellationToken cancellationToken);

    public IAsyncEnumerable<Reference> GetBySchemaAsync(string schemaGuid, CancellationToken cancellationToken);

    public Task<Reference> FindByNameAsync(string exactName, CancellationToken cancellationToken, StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase);

    public IAsyncEnumerable<(Reference reference, string name)> FindByPartialNameAsync(string schemaGuid, string thingNamePart, CancellationToken cancellationToken);

    public IAsyncEnumerable<(Reference reference, string? name)> GetAll(CancellationToken cancellationToken);

    public Task<bool> GuidExists(string thingGuid, CancellationToken cancellationToken);

    public Task<Thing?> LoadAsync(string thingGuid, CancellationToken cancellationToken);

    public Task<bool> RebuildIndexes(CancellationToken cancellationToken);

    public Task<bool> SaveAsync(Thing thing, CancellationToken cancellationToken);
}
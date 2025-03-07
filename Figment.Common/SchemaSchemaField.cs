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

using Figment.Common.Data;

namespace Figment.Common;

public class SchemaSchemaField(string Name) : SchemaTextField(Name)
{
    public const string SCHEMA_FIELD_TYPE = "schema";

    public override Task<string> GetReadableFieldTypeAsync(bool _, CancellationToken cancellationToken) => Task.FromResult(SCHEMA_FIELD_TYPE);

    public override async Task<bool> IsValidAsync(object? value, CancellationToken cancellationToken)
    {
        if (!await base.IsValidAsync(value, cancellationToken))
            return false;

        var str = value as string;
        if (string.IsNullOrWhiteSpace(str))
            return false;

        var ssp = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        if (ssp == null)
            return true; // Assume.

        var exists = await ssp.GuidExists(str, cancellationToken);
        return exists;
    }
}
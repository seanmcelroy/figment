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

public readonly record struct CreateSchemaResult
{
    /// <summary>
    /// True if the operation was successful, otherwise false
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// If the operation was succesful, this is the Guid of the new schema
    /// </summary>
    public string? NewGuid { get; init; }
}
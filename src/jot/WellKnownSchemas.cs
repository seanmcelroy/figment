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

using Figment.Common;

namespace jot;

/// <summary>
/// References to default well-known schemas.
/// </summary>
public static class WellKnownSchemas
{
    /// <summary>
    /// Tasks.
    /// </summary>
    public const string TaskGuid = "00000000-0000-0000-0000-000000000004";

    /// <summary>
    /// Tasks.
    /// </summary>
    public static readonly Reference Task = new() { Guid = TaskGuid, Type = Reference.ReferenceType.Schema };
}

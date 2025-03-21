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

namespace Figment.Common;

public readonly record struct ThingUnsetProperty
{
    /// <summary>
    /// Gets the property name rendered for display with any accompanying <see cref="Schema">.
    /// </summary>
    /// <remarks>
    /// Such as Person.email
    /// </remarks>
    required public readonly string FullDisplayName { get; init; }

    /// <summary>
    /// Gets the property name rendered for display.
    /// </summary>
    /// <remarks>
    /// Such as email.
    /// </remarks>
    required public readonly string SimpleDisplayName { get; init; }

    /// <summary>
    /// Gets the Guid of the <see cref="Schema"> to which this property is associated.
    /// </summary>
    required public readonly string SchemaGuid { get; init; }

    /// <summary>
    /// Gets the name of the <see cref="Schema"> to which this property is associated.
    /// </summary>
    required public readonly string SchemaName { get; init; }

    /// <summary>
    /// Gets the field which is not set on the <see cref="Thing"/>.
    /// </summary>
    required public readonly SchemaFieldBase Field { get; init; }
}
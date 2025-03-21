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

public readonly record struct ThingProperty
{
    /// <summary>
    /// Gets the property name serialized in a JSON file on a Thing.
    /// </summary>
    /// <remarks>
    /// Such as b0c1592e-5d79-4fe4-8814-aa6e534d2b7f.email.
    /// </remarks>
    required public readonly string TruePropertyName { get; init; }

    /// <summary>
    /// Gets the property name rendered for display with any accompanying Schema.
    /// </summary>
    /// <remarks>
    /// Such as Person.email.
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
    /// Gets the unique identifier of the Schema associated with this property.
    /// </summary>
    required public readonly string? SchemaGuid { get; init; }

    /// <summary>
    /// Gets the value of the property.
    /// </summary>
    required public readonly object? Value { get; init; }

    /// <summary>
    /// Gets a value indicating whether the value of this property is valid, as
    /// according to the associated <see cref="Schema">.
    /// If no <see cref="Schema"> is associated, this is always true.
    /// </summary>
    required public readonly bool Valid { get; init; }

    required public readonly bool Required { get; init; }
    required public readonly string? SchemaFieldType { get; init; }
    required public readonly string? SchemaName { get; init; }
}
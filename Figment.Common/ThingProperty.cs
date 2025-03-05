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
    /// The property name serialized in a JSON file on a <see cref="Thing">
    /// </summary>
    /// <remarks>
    /// Such as b0c1592e-5d79-4fe4-8814-aa6e534d2b7f.email
    /// </remarks>
    public required readonly string TruePropertyName { get; init; }
    /// <summary>
    /// The property name rendered for display with any accompanying <see cref="Schema">
    /// </summary>
    /// <remarks>
    /// Such as Person.email
    /// </remarks>
    public required readonly string FullDisplayName { get; init; }
    /// <summary>
    /// The property name rendered for display
    /// </summary>
    /// <remarks>
    /// Such as email
    /// </remarks>
    public required readonly string SimpleDisplayName { get; init; }
    /// <summary>
    /// If there is a <see cref="Schema"> associated with this property, this is its unique identifier
    /// </summary>
    public required readonly string? SchemaGuid { get; init; }
    /// <summary>
    /// The value of the property
    /// </summary>
    public required readonly object? Value { get; init; }
    /// <summary>
    /// If there is a <see cref="Schema"> associated with this property,
    /// this is an indicator whether it is valid.
    /// If no <see cref="Schema"> is associated, this is always true.
    /// </summary>
    public required readonly bool Valid { get; init; }

    public required readonly bool Required { get; init; }
    public required readonly string? SchemaFieldType { get; init; }
    public required readonly string? SchemaName { get; init; }
}
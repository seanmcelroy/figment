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

/// <summary>
/// A potential match to an entity.
/// </summary>
/// <param name="Reference">The weak reference to the entity that is a possible match.</param>
/// <param name="Name">The name of the potentially-matching entity.</param>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public readonly record struct PossibleNameMatch(Reference Reference, string Name)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    /// <summary>
    /// Gets the weak reference to the entity that is a possible match.
    /// </summary>
    required public Reference Reference { get; init; } = Reference;

    /// <summary>
    /// Gets the name of the potentially-matching entity.
    /// </summary>
    required public string Name { get; init; } = Name;

    /// <summary>
    /// Gets the <see cref="Name"/> of the potentially-matching entity.
    /// </summary>
    /// <returns>The value of the <see cref="Name"/> property.</returns>
    public override string ToString() => Name;
}
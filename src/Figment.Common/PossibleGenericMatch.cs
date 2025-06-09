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
/// <param name="Labeler">Function that retrieves a label for the entity.</param>
/// <param name="Entity">The potentially-matching entity, fully loaded from its store.</param>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public readonly record struct PossibleGenericMatch<T>(Func<T, string> Labeler, T Entity)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    /// <summary>
    /// Gets the function that retrieves a label for the entity.
    /// </summary>
    public Func<T, string> Labeler { get; init; } = Labeler;

    /// <summary>
    /// Gets the fully loaded/hydrated potentially-matching entity.
    /// </summary>
    public T Entity { get; init; } = Entity;

    /// <summary>
    /// Gets the type and name of the object.
    /// </summary>
    /// <returns>A string representing the potentially matching entity's type and name.</returns>
    public override string ToString() => Labeler.Invoke(Entity);
}
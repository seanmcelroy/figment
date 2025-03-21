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
/// A link is an entity that connects two other non-link entities with some kind of relationship.
/// </summary>
/// <param name="Guid">The globally unique identifier for the link itself.</param>
/// <param name="Source">The source of the linked relationship.</param>
/// <param name="Destination">The destination of the linked relationship.</param>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public class Link(string Guid, Reference Source, Reference Destination)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    private const string NameIndexFileName = "_link.names.csv";

    /// <summary>
    /// Gets the globally unique identifier for the link itself.
    /// </summary>
    public string Guid { get; init; } = Guid;

    /// <summary>
    /// Gets the source of the linked relationship.
    /// </summary>
    public Reference Source { get; init; } = Source;

    /// <summary>
    /// Gets the destination of the linked relationship.
    /// </summary>
    public Reference Destination { get; init; } = Destination;
}
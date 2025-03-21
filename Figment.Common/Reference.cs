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

using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Figment.Common;

/// <summary>
/// A weak reference to an entity.
/// </summary>
public readonly record struct Reference
{
    /// <summary>
    /// The type of the entity to which the reference refers.
    /// </summary>
    public enum ReferenceType
    {
        /// <summary>
        /// An unknown entity type.
        /// </summary>
        [Description("Unknown")]
        Unknown = 0,

        /// <summary>
        /// A link between two entities.
        /// </summary>
        [Description("Link")]
        Link = 1,

        /// <summary>
        /// A page which represents a user interface grouping of entities.
        /// </summary>
        [Description("Page")]
        Page = 2,

        /// <summary>
        /// A <see cref="Schema"/>
        /// </summary>
        [Description("Schema")]
        Schema = 3,

        /// <summary>
        /// A <see cref="Thing"/>
        /// </summary>
        [Description("Thing")]
        Thing = 4,
    }

    /// <summary>
    /// An empty reference, which refers to nothing.
    /// </summary>
    public static readonly Reference EMPTY = new() { Type = ReferenceType.Unknown, Guid = System.Guid.Empty.ToString() };

    /// <summary>
    /// Gets the type of entity to which this reference points.
    /// </summary>
    [JsonPropertyName("Type")]
    public readonly ReferenceType Type { get; init; }

    /// <summary>
    /// Gets the unique identifier of the entity to which this reference points.
    /// </summary>
    [JsonPropertyName("Guid")]
    public readonly string Guid { get; init; }

    public static implicit operator Reference(Link? l) => new() { Type = ReferenceType.Link, Guid = l?.Guid ?? string.Empty };
    public static implicit operator Reference(Schema? s) => new() { Type = ReferenceType.Schema, Guid = s?.Guid ?? string.Empty };
    public static implicit operator Reference(Thing? t) => new() { Type = ReferenceType.Thing, Guid = t?.Guid ?? string.Empty };
}
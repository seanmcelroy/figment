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

namespace Figment.Common;

public readonly record struct Reference
{
    public enum ReferenceType
    {
        [Description("Unknown")]
        Unknown = 0,
        [Description("Link")]
        Link = 1,
        [Description("Page")]
        Page = 2,
        [Description("Schema")]
        Schema = 3,
        [Description("Thing")]
        Thing = 4,
    }

    public static readonly Reference EMPTY = new() { Type = ReferenceType.Unknown, Guid = System.Guid.Empty.ToString() };

    public readonly ReferenceType Type { get; init; }
    public readonly string Guid { get; init; }

    public static implicit operator Reference(Link? l) => new() { Type = ReferenceType.Link, Guid = l?.Guid ?? string.Empty };
    public static implicit operator Reference(Schema? s) => new() { Type = ReferenceType.Schema, Guid = s?.Guid ?? string.Empty };
    public static implicit operator Reference(Thing? t) => new() { Type = ReferenceType.Thing, Guid = t?.Guid ?? string.Empty };
}
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
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

/// <summary>
/// The settings supplied to the <see cref="SetSchemaPluralCommand"/>.
/// </summary>
public class SetSchemaPluralCommandSettings : SchemaCommandSettings
{
    /// <summary>
    /// Gets the plural word for items of this schema, used as a keyword to enumerate items in interactive mode.
    /// </summary>
    [Description("Plural word for items of this schema, used as a keyword to enumerate items in interactive mode")]
    [CommandArgument(0, "[PLURAL]")]
    public string? Plural { get; init; }
}
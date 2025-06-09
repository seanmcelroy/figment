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

namespace jot.Commands.Interactive;

/// <summary>
/// The settings supplied to the <see cref="SelectCommand"/>.
/// </summary>
public class SelectCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets the name of the entity to select. If nothing is specified, selection is cleared.
    /// </summary>
    [Description("Name of the entity to select. If nothing is specified, selection is cleared")]
    [CommandArgument(0, "[NAME]")]
    public string? Name { get; init; }
}
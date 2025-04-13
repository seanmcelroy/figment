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

namespace jot.Commands.Things;

/// <summary>
/// The settings supplied to the <see cref="PrintThingCommand"/>.
/// </summary>
public class PrintThingCommandSettings : ThingCommandSettings
{
    /// <summary>
    /// Gets a value indicating whether to show the true property names instead of localized display names.
    /// </summary>
    [Description("Shows the true property names instead of localized display names")]
    [CommandOption("--no-pretty")]
    required public bool? NoPrettyDisplayNames { get; init; } = false;
}
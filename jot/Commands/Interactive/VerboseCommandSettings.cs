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
/// The settings supplied to the <see cref="VerboseCommand"/>.
/// </summary>
public class VerboseCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets the true/false value of the setting.  If not specified, then the default will be applied.
    /// </summary>
    [Description("True/false value of the setting.  If not specified, then the default will be applied")]
    [CommandArgument(0, "[VALUE]")]
    public string? Value { get; init; }
}
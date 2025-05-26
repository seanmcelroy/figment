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

namespace jot.Commands;

/// <summary>
/// The settings supplied to the <see cref="ListThingsCommand"/>.
/// </summary>
public class ListThingsCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets an optional partial name match to filter results.
    /// </summary>
    [Description("An optional partial name match to filter results")]
    [CommandArgument(0, "[PARTIAL_NAME]")]
    public string? PartialNameMatch { get; init; }

    /// <summary>
    /// Gets a value indicating whether the command output should be in a human-readable tabular format.
    /// </summary>
    [Description("Outputs the list in a human-readable tabular format")]
    [CommandOption("--as-table")]
    public bool? AsTable { get; init; } = false;

    /// <summary>
    /// Gets an optional filter expression to apply to things.
    /// </summary>
    [Description("An optional filter expression to apply to things (e.g., \"[[name]] = 'John'\" or \"[[age]] = 25\")")]
    [CommandOption("--filter")]
    public string? Filter { get; init; }
}
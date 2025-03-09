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
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Things;

public class ThingCommandSettings : CommandSettings
{
    public const int ARG_POSITION_NAME = 0;

    [Description("Name of the thing to select")]
    [CommandArgument(ARG_POSITION_NAME, "<NAME>")]
    public string? ThingName { get; init; }

    [Description("Provides verbose detail, if available, for any outputs")]
    [CommandOption("-v|--verbose")]
    public required bool? Verbose { get; init; } = Program.Verbose;

    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(ThingName)
            ? ValidationResult.Error("Name must either be the GUID of a thing or a name that resolves to just one")
            : ValidationResult.Success();
    }
}
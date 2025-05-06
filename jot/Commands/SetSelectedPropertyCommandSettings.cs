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

namespace jot.Commands;

/// <summary>
/// The settings supplied to the <see cref="SetSelectedPropertyCommand"/>.
/// </summary>
public class SetSelectedPropertyCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets the name of the property to change.
    /// </summary>
    [Description("Name of the property to change")]
    [CommandArgument(0, "<PROPERTY>")]
    public string? PropertyName { get; init; }

    /// <summary>
    /// Gets the subcommand and parameter specifying what to change.
    /// </summary>
    [Description("Subcommand and parameter specifying what to change")]
    [CommandArgument(1, "[VALUES]")]
#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
    public string[]? Values { get; init; }
#pragma warning restore SA1011 // Closing square brackets should be spaced correctly

    /// <summary>
    /// Gets whether to provide verbose detail, if available, for any outputs.
    /// </summary>
    [Description("Provides verbose detail, if available, for any outputs")]
    [CommandOption("-v|--verbose")]
    required public bool? Verbose { get; init; } = Program.Verbose;

    /// <summary>
    /// Gets a value indicating whether to instruct the command to ignore validation of the property name.
    /// </summary>
    [Description("Instructs the command to ignore validation of the property name")]
    [CommandOption("--override-validation")]
    required public bool OverrideValidation { get; init; } = false;

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(PropertyName)
            ? ValidationResult.Error("Property name must be set")
            : ValidationResult.Success();
    }
}
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

/// <summary>
/// The settings supplied to the <see cref="SetThingPropertyCommand"/>.
/// </summary>
public class SetThingPropertyCommandSettings : ThingCommandSettings
{
    /// <summary>
    /// Gets the name of the property to change.
    /// </summary>
    [Description("Name of the property to change")]
    [CommandArgument(0, "<PROPERTY>")]
    required public string PropertyName { get; init; }

    /// <summary>
    /// Gets the value to set, or to delete if blank.
    /// </summary>
    [Description("Value to set, or to delete if blank")]
    [CommandArgument(1, "[VALUE]")]
    public string? Value { get; init; }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(PropertyName)
            ? ValidationResult.Error("Property name must be set")
            : ValidationResult.Success();
    }
}
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
using System.Globalization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

/// <summary>
/// The settings supplied to the <see cref="SetSchemaPropertyDisplayCommand"/>.
/// </summary>
public class SetSchemaPropertyDisplayCommandSettings : SchemaPropertyCommandSettings
{
    /// <summary>
    /// Gets the text to display instead of the actual property name.  If not specified, the display name will be deleted.
    /// </summary>
    [Description("Text to display instead of the actual property name.  If not specified, the display name will be deleted")]
    [CommandArgument(0, "[PRETTY_NAME]")]
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets the culture to use this display name.  If not specified, is en-US.
    /// </summary>
    [Description("Culture to use this display name.  If not specified, is en-US")]
    [CommandArgument(1, "[CULTURE]")]
    public string? Culture { get; init; } = "en-US";

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(PropertyName))
        {
            return ValidationResult.Error("Property name must be set");
        }

        if (!string.IsNullOrWhiteSpace(Culture)
            && !CultureInfo.GetCultures(CultureTypes.AllCultures).Any(c => string.Equals(c.Name, Culture, StringComparison.InvariantCultureIgnoreCase)))
        {
            return ValidationResult.Error("Invalid culture specified.  Try 'en-US' if unsure.");
        }

        return ValidationResult.Success();
    }
}
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

namespace jot.Commands.Schemas.ImportMaps;

/// <summary>
/// The settings supplied to commands that need to target na <see cref="Figment.Common.SchemaImportMap"/>
/// and have no other arguments.
/// </summary>
/// <seealso cref="DeleteImportMapCommand"/>
/// <seealso cref="NewImportMapCommand"/>
public class ImportMapCommandSettings : SchemaCommandSettings
{
    /// <summary>
    /// Gets the name of the import map.
    /// </summary>
    [Description("Name of the import map")]
    [CommandArgument(0, "<MAP_NAME>")]
    public string? ImportMapName { get; init; }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(ImportMapName))
        {
            return ValidationResult.Error("Import map name must be specified");
        }

        // Because we inherit from a non-base class of settings, call down to the base class validation.
        return base.Validate();
    }
}
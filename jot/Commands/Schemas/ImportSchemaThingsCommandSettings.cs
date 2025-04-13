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

namespace jot.Commands.Schemas;

/// <summary>
/// The settings supplied to the <see cref="ImportSchemaThingsCommand"/>.
/// </summary>
public class ImportSchemaThingsCommandSettings : SchemaCommandSettings
{
    /// <summary>
    /// Gets the full file path of things to import.
    /// </summary>
    [Description("Full file path of things to import")]
    [CommandArgument(0, "<FILE_PATH>")]
    public string? FilePath { get; init; }

    /// <summary>
    /// Gets the format of the file, such as 'csv'.
    /// </summary>
    [Description("Format of the file, such as 'csv'")]
    [DefaultValue("csv")]
    [CommandArgument(1, "[FORMAT]")]
    public string? Format { get; init; }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(FilePath))
        {
            return ValidationResult.Error("File path must be set");
        }

        if (!File.Exists(FilePath))
        {
            return ValidationResult.Error("File path does not exist");
        }

        return ValidationResult.Success();
    }
}
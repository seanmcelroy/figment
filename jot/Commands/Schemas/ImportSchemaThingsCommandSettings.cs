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
using Figment.Common;
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

    /// <summary>
    /// Gets a value indicating whether to ignore duplicate records in the file and import non-duplicate records.
    /// </summary>
    [Description("Ignores duplicate records in the file and imports non-duplicates instead of erroring and not importing any when any duplicates are found.")]
    [CommandOption("--ignore-dupes")]
    required public bool? IgnoreDuplicates { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether the operation could run to completion, without actually importing any data to the underlying data store.
    /// </summary>
    [Description("Checks whether the operation could run to completion, without actually importing any data to the underlying data store.")]
    [CommandOption("--dry-run")]
    required public bool? DryRun { get; init; } = false;

    /// <summary>
    /// Gets the line number in the CSV file rows should be read (1-based).
    /// </summary>
    /// <remarks>
    /// Row range filtering when reading from a comma separated value file.
    /// </remarks>
    [Description("Import rows from a CSV file from a specific line number (1-based)")]
    [CommandOption("--csv-from-row")]
    public int? CsvFromRow { get; init; } = 1;

    /// <summary>
    /// Gets the line number in the CSV file rows which data should be read (1-based, inclusive).
    /// </summary>
    /// <remarks>
    /// Row range filtering when reading from a comma separated value file.
    /// </remarks>
    [Description("Import rows to line number (1-based, inclusive)")]
    [CommandOption("--csv-to-row")]
    public int? CsvToRow { get; init; }

    /// <summary>
    /// Gets the number of records to skip before beginning an import.
    /// </summary>
    /// <remarks>
    /// This is a general alternative to a source-specific selector, like <see cref="CsvFromRow"/>.
    /// </remarks>
    [Description("Number of records to skip before beginning an import.  This applies to records read from any given source or format.")]
    [CommandArgument(2, "[SKIP]")]
    public int? RecordsToSkip { get; init; }

    /// <summary>
    /// Gets the number of records to import.  If unspecified, all parsable records will be imported.
    /// </summary>
    /// <remarks>
    /// This is a general alternative to a source-specific selector, like <see cref="CsvToRow"/>.
    /// </remarks>
    [Description("Number of records to import.  If unspecified, all parsable records will be imported.  This applies to records read from any given source or format.")]
    [CommandArgument(3, "[COUNT]")]
    public int? RecordsToImport { get; init; }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(FilePath))
        {
            return ValidationResult.Error("File path must be set");
        }

        var expandedPath = FileUtility.ExpandRelativePaths(FilePath);
        if (!File.Exists(expandedPath))
        {
            return ValidationResult.Error($"File path '{expandedPath}' does not exist");
        }

        if (CsvFromRow.HasValue && CsvFromRow < 1)
        {
            return ValidationResult.Error($"CSV starting row must be 1 or greater");
        }

        if (CsvToRow.HasValue && CsvToRow < 1)
        {
            return ValidationResult.Error($"CSV ending row must be 1 or greater");
        }

        if (CsvFromRow.HasValue && CsvToRow.HasValue && CsvFromRow.Value > CsvToRow.Value)
        {
            return ValidationResult.Error($"CSV ending row must be greater than or equal to the starting row");
        }

        if (RecordsToImport.HasValue && RecordsToImport < 1)
        {
            return ValidationResult.Error($"Records to import must be greater than 1");
        }

        if (RecordsToSkip.HasValue && RecordsToSkip < 0)
        {
            return ValidationResult.Error($"Records to import must be greater than 0");
        }

        return ValidationResult.Success();
    }
}
using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas.ImportMaps;

/// <summary>
/// The settings supplied to the <see cref="NewImportMapCommand"/>.
/// </summary>
public class NewImportMapCommandSettings : ImportMapCommandSettings
{
    /// <summary>
    /// Gets the type of the file this map applies to, such as 'csv'.
    /// </summary>
    [Description("Type of the file this map applies to, such as 'csv'")]
    [CommandArgument(1, "<FILE_TYPE>")]
    required public string FileType { get; init; }

    /// <summary>
    /// Gets the path to a sample file from which to read file fields into the import map.
    /// </summary>
    [Description("Path to a sample file from which to read file fields into the import map")]
    [CommandArgument(2, "<SAMPLE_FILE_PATH>")]
    required public string SampleFilePath { get; init; }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(FileType))
        {
            return ValidationResult.Error("File type must be specified");
        }

        if (!string.Equals(FileType, "csv", StringComparison.InvariantCultureIgnoreCase))
        {
            return ValidationResult.Error("csv is the only currently supported file type");
        }

        if (string.IsNullOrWhiteSpace(SampleFilePath))
        {
            return ValidationResult.Error("Sample file must be specified");
        }

        if (string.IsNullOrWhiteSpace(SampleFilePath))
        {
            return ValidationResult.Error($"File path '{SampleFilePath}' was not found.");
        }

        // Because we inherit from a non-base class of settings, call down to the base class validation.
        return base.Validate();
    }
}
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
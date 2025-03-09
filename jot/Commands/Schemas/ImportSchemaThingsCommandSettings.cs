using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

public class ImportSchemaThingsCommandSettings : SchemaCommandSettings
{
    public const int ARG_POSITION_FILE_PATH = 0;
    public const int ARG_POSITION_FORMAT = 1;

    [Description("Full file path of things to import")]
    [CommandArgument(ARG_POSITION_FILE_PATH, "<FILE_PATH>")]
    public string? FilePath { get; init; }

    [Description("Format of the file, such as 'csv'")]
    [DefaultValue("csv")]
    [CommandArgument(ARG_POSITION_FORMAT, "[FORMAT]")]
    public string? Format { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(FilePath))
            return ValidationResult.Error("File path must be set");

        if (!File.Exists(FilePath))
            return ValidationResult.Error("File path does not exist");

        return ValidationResult.Success();
    }
}
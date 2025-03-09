
using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas.ImportMaps;

public class NewImportMapCommandSettings : SchemaCommandSettings
{
    public const int ARG_POSITION_MAP_NAME = 0;

    [Description("Name of the import map")]
    [CommandArgument(ARG_POSITION_MAP_NAME, "<MAP_NAME>")]
    public string? ImportMapName { get; init; }

    public const int ARG_POSITION_FILE_TYPE = 1;

    [Description("Type of the file this map applies to, such as 'csv'")]
    [CommandArgument(ARG_POSITION_FILE_TYPE, "<FILE_TYPE>")]
    public string? FileType { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(ImportMapName))
            return ValidationResult.Error("Import map name must be specified");

        if (string.IsNullOrWhiteSpace(FileType))
            return ValidationResult.Error("File type must be specified");

        if (string.Compare(FileType, "csv", StringComparison.InvariantCultureIgnoreCase) != 0)
            return ValidationResult.Error("csv is the only currently supported file type");

        return ValidationResult.Success();
    }
}
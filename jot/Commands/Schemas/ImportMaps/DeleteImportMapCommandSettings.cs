
using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas.ImportMaps;

public class DeleteImportMapCommandSettings : SchemaCommandSettings
{
    public const int ARG_POSITION_MAP_NAME = 0;

    [Description("Name of the import map")]
    [CommandArgument(ARG_POSITION_MAP_NAME, "<MAP_NAME>")]
    public string? ImportMapName { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(ImportMapName))
            return ValidationResult.Error("Import map name must be specified");

        return ValidationResult.Success();
    }
}
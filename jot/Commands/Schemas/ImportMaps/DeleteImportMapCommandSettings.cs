using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas.ImportMaps;

/// <summary>
/// The settings supplied to the <see cref="DeleteImportMapCommand"/>.
/// </summary>
public class DeleteImportMapCommandSettings : SchemaCommandSettings
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

        return ValidationResult.Success();
    }
}
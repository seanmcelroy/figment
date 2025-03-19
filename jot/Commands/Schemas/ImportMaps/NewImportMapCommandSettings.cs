using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas.ImportMaps;

/// <summary>
/// The settings supplied to the <see cref="NewImportMapCommand"/>.
/// </summary>
public class NewImportMapCommandSettings : SchemaCommandSettings
{
    /// <summary>
    /// Gets the name of the import map.
    /// </summary>
    [Description("Name of the import map")]
    [CommandArgument(0, "<MAP_NAME>")]
    required public string ImportMapName { get; init; }

    /// <summary>
    /// Gets the type of the file this map applies to, such as 'csv'.
    /// </summary>
    [Description("Type of the file this map applies to, such as 'csv'")]
    [CommandArgument(1, "<FILE_TYPE>")]
    required public string FileType { get; init; }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(ImportMapName))
        {
            return ValidationResult.Error("Import map name must be specified");
        }

        if (string.IsNullOrWhiteSpace(FileType))
        {
            return ValidationResult.Error("File type must be specified");
        }

        if (!string.Equals(FileType, "csv", StringComparison.InvariantCultureIgnoreCase))
        {
            return ValidationResult.Error("csv is the only currently supported file type");
        }

        return ValidationResult.Success();
    }
}
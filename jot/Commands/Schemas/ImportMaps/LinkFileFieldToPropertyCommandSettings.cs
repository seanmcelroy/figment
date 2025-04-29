using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas.ImportMaps;

/// <summary>
/// The settings supplied to the <see cref="LinkFileFieldToPropertyCommand"/>.
/// </summary>
public class LinkFileFieldToPropertyCommandSettings : ImportMapCommandSettings
{
    /// <summary>
    /// Gets the name of the source import file field.
    /// </summary>
    [Description("Name of the source import file field")]
    [CommandArgument(0, "<FILE_FIELD>")]
    public string FileField { get; init; }

    /// <summary>
    /// Gets the name of the destination schema property.
    /// </summary>
    [Description("Name of the destination schema property")]
    [CommandArgument(1, "[PROPERTY]")]
    public string? SchemaProperty { get; init; }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(FileField)
            ? ValidationResult.Error("File field name must be provided and cannot be blank or only whitespaces")
            : ValidationResult.Success();
    }
}
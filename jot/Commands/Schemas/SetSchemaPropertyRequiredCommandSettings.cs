using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

/// <summary>
/// The settings supplied to the <see cref="SetSchemaPropertyRequiredCommand"/>.
/// </summary>
public class SetSchemaPropertyRequiredCommandSettings : SchemaPropertyCommandSettings
{
    /// <summary>
    /// Gets a value indicating whether the property is required.
    /// </summary>
    [Description("Value indicating whether the property is required")]
    [CommandArgument(0, "<REQUIRED>")]
    required public bool Required { get; init; } = true;

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(PropertyName)
            ? ValidationResult.Error("Property name must be set")
            : ValidationResult.Success();
    }
}
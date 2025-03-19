using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

/// <summary>
/// The settings supplied to the <see cref="SchemaPropertyCommand"/>.
/// </summary>
public class SchemaPropertyCommandSettings : SchemaCommandSettings
{
    /// <summary>
    /// Gets the name of the property to change.
    /// </summary>
    [Description("Name of the property to change")]
    [CommandArgument(0, "<PROPERTY>")]
    public string? PropertyName { get; init; }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(PropertyName))
        {
            return ValidationResult.Error("Property name must be set");
        }

        return ValidationResult.Success();
    }
}
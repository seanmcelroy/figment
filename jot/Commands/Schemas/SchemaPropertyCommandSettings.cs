using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

public class SchemaPropertyCommandSettings : SchemaCommandSettings
{
    public const int ARG_POSITION_PROPERTY_NAME = 0;

    [Description("Name of the property to change")]
    [CommandArgument(ARG_POSITION_PROPERTY_NAME, "<PROPERTY>")]
    public string? PropertyName { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(PropertyName))
            return ValidationResult.Error("Property name must be set");

        return ValidationResult.Success();
    }
}
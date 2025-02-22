using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class RequireSchemaPropertyCommandSettings : SchemaCommandSettings
{
    public const int ARG_POSITION_PROPERTY_NAME = 0;

    [Description("The name of the property to change")]
    [CommandArgument(ARG_POSITION_PROPERTY_NAME, "<PROPERTY>")]
    public string? PropertyName { get; init; }

    [Description("The value indicating whether the property is required")]
    [CommandOption("--required")]
    public bool? Required { get; init; } = true;

    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(PropertyName)
            ? ValidationResult.Error("Property name must be set")
            : ValidationResult.Success();
    }
}
using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class SetSchemaPropertyRequiredCommandSettings : SchemaPropertyCommandSettings
{
    public const int ARG_POSITION_REQUIRED = 0;
    [Description("The value indicating whether the property is required")]
    [CommandArgument(ARG_POSITION_REQUIRED, "<REQUIRED>")]
    public bool? Required { get; init; } = true;

    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(PropertyName)
            ? ValidationResult.Error("Property name must be set")
            : ValidationResult.Success();
    }
}
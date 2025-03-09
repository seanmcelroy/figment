using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Things;

public class SetThingPropertyCommandSettings : ThingCommandSettings
{
    public const int ARG_POSITION_PROPERTY_NAME = 0;
    public const int ARG_POSITION_VALUE = 1;

    [Description("Name of the property to change")]
    [CommandArgument(ARG_POSITION_PROPERTY_NAME, "<PROPERTY>")]
    public string? PropertyName { get; init; }

    [Description("Value to set, or to delete if blank")]
    [CommandArgument(ARG_POSITION_VALUE, "[VALUE]")]
    public string? Value { get; init; }

    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(PropertyName)
            ? ValidationResult.Error("Property name must be set")
            : ValidationResult.Success();
    }
}
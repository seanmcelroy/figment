using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Things;

public class PromoteThingPropertyCommandSettings : ThingCommandSettings
{
    public const int ARG_POSITION_PROPERTY_NAME = 0;

    [Description("Name of the property to promote")]
    [CommandArgument(ARG_POSITION_PROPERTY_NAME, "<PROPERTY>")]
    public string? PropertyName { get; init; }

    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(PropertyName)
            ? ValidationResult.Error("Property name must be set")
            : ValidationResult.Success();
    }
}
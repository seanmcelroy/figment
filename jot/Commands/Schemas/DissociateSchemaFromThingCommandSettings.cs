using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

public class DissociateSchemaFromThingCommandSettings : SchemaCommandSettings
{
    public const int ARG_POSITION_PROPERTY_NAME = 0;

    [Description("Name of the thing to dissociate from the schema")]
    [CommandArgument(ARG_POSITION_PROPERTY_NAME, "<THING_NAME>")]
    public string? ThingName { get; init; }

    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(ThingName)
            ? ValidationResult.Error("Thing name must be set")
            : ValidationResult.Success();
    }
}
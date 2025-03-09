using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

public class AssociateSchemaWithThingCommandSettings : SchemaCommandSettings
{
    public const int ARG_POSITION_THING_NAME = 0;

    [Description("Name of the thing to associate with the schema")]
    [CommandArgument(ARG_POSITION_THING_NAME, "<THING_NAME>")]
    public string? ThingName { get; init; }

    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(ThingName)
            ? ValidationResult.Error("Thing name must be set")
            : ValidationResult.Success();
    }
}
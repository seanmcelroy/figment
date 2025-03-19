using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

/// <summary>
/// The settings supplied to the <see cref="DissociateSchemaFromThingCommand"/>.
/// </summary>
public class DissociateSchemaFromThingCommandSettings : SchemaCommandSettings
{
    /// <summary>
    /// Gets the name of the <see cref="Thing"/> to dissociate from the <see cref="Schema"/>.
    /// </summary>
    [Description("Name of the thing to dissociate from the schema")]
    [CommandArgument(0, "<THING_NAME>")]
    required public string ThingName { get; init; }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(ThingName)
            ? ValidationResult.Error("Thing name must be set")
            : ValidationResult.Success();
    }
}
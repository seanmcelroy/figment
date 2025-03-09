using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

public class SetSchemaDescriptionCommandSettings : SchemaCommandSettings
{
    public const int ARG_POSITION_DESCRIPTION = 0;

    [Description("Description of the schema")]
    [CommandArgument(ARG_POSITION_DESCRIPTION, "[DESCRIPTION]")]
    public string? Description { get; init; }
}
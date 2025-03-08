using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands;

public class SetSchemaDescriptionCommandSettings : SchemaCommandSettings
{
    public const int ARG_POSITION_DESCRIPTION = 0;

    [Description("The description of this schema.")]
    [CommandArgument(ARG_POSITION_DESCRIPTION, "[DESCRIPTION]")]
    public string? Description { get; init; }
}
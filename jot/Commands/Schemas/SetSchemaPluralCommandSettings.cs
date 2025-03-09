using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

public class SetSchemaPluralCommandSettings : SchemaCommandSettings
{
    public const int ARG_POSITION_PLURAL = 0;

    [Description("Plural word for items of this schema, used as a keyword to enumerate items in interactive mode")]
    [CommandArgument(ARG_POSITION_PLURAL, "[PLURAL]")]
    public string? Plural { get; init; }
}
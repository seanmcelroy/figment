using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands;

public class SetSchemaPluralCommandSettings : SchemaCommandSettings
{
    public const int ARG_POSITION_FORMULA = 0;

    [Description("The plural word for items of this schema, used as a keyword to enumerate items in interactive mode.")]
    [CommandArgument(ARG_POSITION_FORMULA, "[PLURAL]")]
    public string? Plural { get; init; }
}
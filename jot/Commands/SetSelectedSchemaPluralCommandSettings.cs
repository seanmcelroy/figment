using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands;

public class SetSelectedSchemaPluralCommandSettings : CommandSettings
{
    public const int ARG_POSITION_PLURAL = 0;

    [Description("Plural word for items of this schema, used as a keyword to enumerate items in interactive mode")]
    [CommandArgument(ARG_POSITION_PLURAL, "[PLURAL]")]
    public string? Plural { get; init; }

    [Description("Provides verbose detail, if available, for any outputs")]
    [CommandOption("-v|--verbose")]
    public required bool? Verbose { get; init; } = Program.Verbose;
}
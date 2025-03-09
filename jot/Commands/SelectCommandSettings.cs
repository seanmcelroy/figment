
using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands;

public class SelectCommandSettings : CommandSettings
{
    public const int ARG_POSITION_NAME = 0;

    [Description("Name of the entity to select. If nothing is specified, selection is cleared")]
    [CommandArgument(ARG_POSITION_NAME, "[NAME]")]
    public string? Name { get; init; }
}
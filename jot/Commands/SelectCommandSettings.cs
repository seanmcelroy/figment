
using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class SelectCommandSettings : CommandSettings
{
    public const int ARG_POSITION_NAME = 0;

    [Description("The entity to select")]
    [CommandArgument(ARG_POSITION_NAME, "[NAME]")]
    public string? Name { get; init; }
}
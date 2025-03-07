
using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands;

public class VerboseCommandSettings : CommandSettings
{
    public const int ARG_POSITION_NAME = 0;

    [Description("The true/false value of the setting.  If not specified, then the default will be applied.")]
    [CommandArgument(ARG_POSITION_NAME, "[VALUE]")]
    public string? Value { get; init; }
}
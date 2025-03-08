using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands;

public class PrintSelectedCommandSettings : CommandSettings
{
    [Description("Provides verbose detail, if available, for any outputs")]
    [CommandOption("-v|--verbose")]
    public required bool? Verbose { get; init; } = Program.Verbose;

    [Description("Shows the true property names instead of localized display names")]
    [CommandOption("--raw|--no-pretty")]
    public required bool? NoPrettyDisplayNames { get; init; } = false;
}

using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands;

public class PrintThingCommandSettings : ThingCommandSettings
{
    [Description("Shows the true property names instead of localized display names")]
    [CommandOption("--no-pretty")]
    public bool? NoPrettyDisplayNames { get; init; } = false;
}
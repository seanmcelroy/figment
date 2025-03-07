using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands;

public class PrintSelectedCommandSettings : CommandSettings
{
    public const int ARG_POSITION_PROPERTY_NAME = 0;

    [Description("The name of the entity to print, if one is not already selected")]
    [CommandArgument(ARG_POSITION_PROPERTY_NAME, "[NAME]")]
    public string? EntityName { get; init; }

    [Description("Provides verbose detail, if available, for any outputs")]
    [CommandOption("-v")]
    public bool? Verbose { get; init; } = Program.Verbose;

    [Description("Shows the true property names instead of localized display names")]
    [CommandOption("--no-pretty")]
    public bool? NoPrettyDisplayNames { get; init; } = false;
}
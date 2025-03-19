using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands;

/// <summary>
/// The settings supplied to the <see cref="PrintSelectedCommand"/>.
/// </summary>
public class PrintSelectedCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets whether to provide verbose detail, if available, for any outputs.
    /// </summary>
    [Description("Provides verbose detail, if available, for any outputs")]
    [CommandOption("-v|--verbose")]
    required public bool? Verbose { get; init; } = Program.Verbose;

    /// <summary>
    /// Gets a value indicating whether to show the true property names instead of localized display names.
    /// </summary>
    [Description("Shows the true property names instead of localized display names")]
    [CommandOption("--raw|--no-pretty")]
    required public bool? NoPrettyDisplayNames { get; init; } = false;
}
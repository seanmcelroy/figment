using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands.Things;

/// <summary>
/// The settings supplied to the <see cref="PrintThingCommand"/>.
/// </summary>
public class PrintThingCommandSettings : ThingCommandSettings
{
    /// <summary>
    /// Gets a value indicating whether to show the true property names instead of localized display names.
    /// </summary>
    [Description("Shows the true property names instead of localized display names")]
    [CommandOption("--no-pretty")]
    required public bool? NoPrettyDisplayNames { get; init; } = false;
}
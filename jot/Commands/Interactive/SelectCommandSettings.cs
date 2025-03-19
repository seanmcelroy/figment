using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands.Interactive;

/// <summary>
/// The settings supplied to the <see cref="SelectCommand"/>.
/// </summary>
public class SelectCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets the name of the entity to select. If nothing is specified, selection is cleared.
    /// </summary>
    [Description("Name of the entity to select. If nothing is specified, selection is cleared")]
    [CommandArgument(0, "[NAME]")]
    public string? Name { get; init; }
}
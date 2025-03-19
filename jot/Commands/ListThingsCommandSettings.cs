using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands;

/// <summary>
/// The settings supplied to the <see cref="ListThingsCommand"/>.
/// </summary>
public class ListThingsCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets a value indicating whether the command output should be in a human-readable tabular format.
    /// </summary>
    [Description("Outputs the list in a human-readable tabular format")]
    [CommandOption("--as-table")]
    public bool? AsTable { get; init; } = false;
}
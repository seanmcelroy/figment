using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

/// <summary>
/// The settings supplied to the <see cref="ListSchemaMembersCommand"/>.
/// </summary>
public class ListSchemaMembersCommandSettings : SchemaCommandSettings
{
    /// <summary>
    /// Gets a value indicating whether the command output should be in a human-readable tabular format.
    /// </summary>
    [Description("Outputs the list in a human-readable tabular format")]
    [CommandOption("--as-table")]
    public bool? AsTable { get; init; } = false;
}
using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands.Interactive;

/// <summary>
/// The settings supplied to the <see cref="ListSelectedSchemaMembersCommand"/>.
/// </summary>
public class ListSelectedSchemaMembersCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets whether to provide verbose detail, if available, for any outputs.
    /// </summary>
    [Description("Provides verbose detail, if available, for any outputs")]
    [CommandOption("-v|--verbose")]
    required public bool? Verbose { get; init; } = Program.Verbose;
}
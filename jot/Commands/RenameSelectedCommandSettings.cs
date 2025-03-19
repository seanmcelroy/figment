using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands;

/// <summary>
/// The settings supplied to the <see cref="RenameSelectedCommand"/>.
/// </summary>
public class RenameSelectedCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets the new name for the entity.
    /// </summary>
    [Description("New name for the entity")]
    [CommandArgument(0, "<NEW_NAME>")]
    required public string NewName { get; init; }

    /// <summary>
    /// Gets whether to provide verbose detail, if available, for any outputs.
    /// </summary>
    [Description("Provides verbose detail, if available, for any outputs")]
    [CommandOption("-v|--verbose")]
    required public bool? Verbose { get; init; } = Program.Verbose;
}
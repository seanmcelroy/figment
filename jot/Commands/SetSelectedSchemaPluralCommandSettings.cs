using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands;

/// <summary>
/// The settings supplied to the <see cref="SetSelectedSchemaPluralCommand"/>.
/// </summary>
public class SetSelectedSchemaPluralCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets the plural word for items of this schema, used as a keyword to enumerate items in interactive mode
    /// </summary>
    [Description("Plural word for items of this schema, used as a keyword to enumerate items in interactive mode")]
    [CommandArgument(0, "[PLURAL]")]
    public string? Plural { get; init; }

    /// <summary>
    /// Gets whether to provide verbose detail, if available, for any outputs.
    /// </summary>
    [Description("Provides verbose detail, if available, for any outputs")]
    [CommandOption("-v|--verbose")]
    required public bool? Verbose { get; init; } = Program.Verbose;
}
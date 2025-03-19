using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands.Interactive;

/// <summary>
/// The settings supplied to the <see cref="VerboseCommand"/>.
/// </summary>
public class VerboseCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets the true/false value of the setting.  If not specified, then the default will be applied.
    /// </summary>
    [Description("True/false value of the setting.  If not specified, then the default will be applied")]
    [CommandArgument(0, "[VALUE]")]
    public string? Value { get; init; }
}
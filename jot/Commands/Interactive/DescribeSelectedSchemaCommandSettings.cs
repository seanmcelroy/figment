using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands.Interactive;

/// <summary>
/// The settings supplied to the <see cref="DescribeSelectedSchemaCommand"/>.
/// </summary>
public class DescribeSelectedSchemaCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets the description of this <see cref="Schema"/>.
    /// </summary>
    [Description("Description of this schema")]
    [CommandArgument(0, "[DESCRIPTION]")]
    public string? Description { get; init; }

    /// <summary>
    /// Gets whether to provide verbose detail, if available, for any outputs.
    /// </summary>
    [Description("Provides verbose detail, if available, for any outputs")]
    [CommandOption("-v|--verbose")]
    required public bool? Verbose { get; init; } = Program.Verbose;
}
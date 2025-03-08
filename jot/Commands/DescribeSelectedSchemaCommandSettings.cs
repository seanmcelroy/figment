using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands;

public class DescribeSelectedSchemaCommandSettings : CommandSettings
{
    public const int ARG_POSITION_DESCRIPTION = 0;

    [Description("The description of this schema.")]
    [CommandArgument(ARG_POSITION_DESCRIPTION, "[DESCRIPTION]")]
    public string? Description { get; init; }

    [Description("Provides verbose detail, if available, for any outputs")]
    [CommandOption("-v")]
    public required bool? Verbose { get; init; } = Program.Verbose;
}
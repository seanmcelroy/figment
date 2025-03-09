using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands;

public class RenameSelectedCommandSettings : CommandSettings
{
    public const int ARG_POSITION_NEW_NAME = 0;

    [Description("New name for the entity")]
    [CommandArgument(ARG_POSITION_NEW_NAME, "[NEW_NAME]")]
    public string? NewName { get; init; }

    [Description("Provides verbose detail, if available, for any outputs")]
    [CommandOption("-v|--verbose")]
    public required bool? Verbose { get; init; } = Program.Verbose;
}
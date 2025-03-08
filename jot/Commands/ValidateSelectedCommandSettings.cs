using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands;

public class ValidateSelectedCommandSettings : CommandSettings
{
    [Description("Provides verbose detail, if available, for any outputs")]
    [CommandOption("-v|--verbose")]
    public required bool? Verbose { get; init; } = Program.Verbose;
}
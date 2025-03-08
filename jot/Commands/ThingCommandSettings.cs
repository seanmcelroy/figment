
using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class ThingCommandSettings : CommandSettings
{
    public const int ARG_POSITION_NAME = 0;

    [Description("The thing to select")]
    [CommandArgument(ARG_POSITION_NAME, "<NAME>")]
    public string? ThingName { get; init; }

    [Description("Provides verbose detail, if available, for any outputs")]
    [CommandOption("-v|--verbose")]
    public required bool? Verbose { get; init; } = Program.Verbose;

    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(ThingName)
            ? ValidationResult.Error("Name must either be the GUID of a thing or a name that resolves to just one")
            : ValidationResult.Success();
    }
}
using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class ThingRenameCommandSettings : ThingCommandSettings
{
    public const int ARG_POSITION_NEW_NAME = 0;

    [Description("The new name for the thing.")]
    [CommandArgument(ARG_POSITION_NEW_NAME, "<NEW_NAME>")]
    public string? NewName { get; init; }

    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(NewName)
            ? ValidationResult.Error("New name must be provided and cannot be blank or only whitespaces")
            : ValidationResult.Success();
    }
}

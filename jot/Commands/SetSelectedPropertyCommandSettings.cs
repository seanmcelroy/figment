using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class SetSelectedPropertyCommandSettings : CommandSettings
{
    public const int ARG_POSITION_PROPERTY_NAME = 0;
    public const int ARG_POSITION_VALUE = 1;

    [Description("Name of the property to change")]
    [CommandArgument(ARG_POSITION_PROPERTY_NAME, "<PROPERTY>")]
    public string? PropertyName { get; init; }

    [Description("Subcommand and parameter specifying what to change")]
    [CommandArgument(ARG_POSITION_VALUE, "[VALUES]")]
    public string[]? Values { get; init; }

    [Description("Provides verbose detail, if available, for any outputs")]
    [CommandOption("-v|--verbose")]
    public required bool? Verbose { get; init; } = Program.Verbose;

    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(PropertyName)
            ? ValidationResult.Error("Property name must be set")
            : ValidationResult.Success();
    }
}
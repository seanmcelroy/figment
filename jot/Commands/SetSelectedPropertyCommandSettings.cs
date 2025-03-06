using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class SetSelectedPropertyCommandSettings : CommandSettings
{
    public const int ARG_POSITION_PROPERTY_NAME = 0;
    public const int ARG_POSITION_VALUE = 1;

    [Description("The name of the property to change")]
    [CommandArgument(ARG_POSITION_PROPERTY_NAME, "<PROPERTY>")]
    public string? PropertyName { get; init; }

    [Description("The subcommand and parameter specifying what to change")]
    [CommandArgument(ARG_POSITION_VALUE, "[VALUES]")]
    public string[]? Values { get; init; }

    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(PropertyName)
            ? ValidationResult.Error("Property name must be set")
            : ValidationResult.Success();
    }
}
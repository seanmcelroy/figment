using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands;

public class ValidateSelectedCommandSettings : CommandSettings
{
    public const int ARG_POSITION_PROPERTY_NAME = 0;

    [Description("The name of the entity to validate, if one is not already selected")]
    [CommandArgument(ARG_POSITION_PROPERTY_NAME, "[NAME]")]
    public string? EntityName { get; init; }
}
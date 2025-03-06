using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands;

public class RenameSelectedCommandSettings : CommandSettings
{
    public const int ARG_POSITION_PROPERTY_NAME = 0;

    [Description("The existing name of the entity to rename, if one is not already selected")]
    [CommandArgument(ARG_POSITION_PROPERTY_NAME, "[NAME]")]
    public string? EntityName { get; init; }

    public const int ARG_POSITION_NEW_NAME = 1;

    [Description("The new name for the entity.")]
    [CommandArgument(ARG_POSITION_NEW_NAME, "[NEW_NAME]")]
    public string? NewName { get; init; }

}
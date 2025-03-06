using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class SchemaRenameCommandSettings : SchemaCommandSettings
{
    public const int ARG_POSITION_NEW_NAME = 0;

    [Description("The new name for the schema.  This is usually one singular term.")]
    [CommandArgument(ARG_POSITION_NEW_NAME, "<NEW_NAME>")]
    public string? NewName { get; init; }

    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(NewName)
            ? ValidationResult.Error("New schema name must be provided and cannot be blank or only whitespaces")
            : ValidationResult.Success();
    }
}

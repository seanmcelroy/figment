
using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class SchemaCommandSettings : CommandSettings
{
    public const int ARG_POSITION_NAME = 0;

    [Description("The schema to select")]
    [CommandArgument(ARG_POSITION_NAME, "<NAME>")]
    public string? Name { get; init; }

    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(Name)
            ? ValidationResult.Error("Name must either be the GUID of a schema or a name that resolves to just one")
            : ValidationResult.Success();
    }
}
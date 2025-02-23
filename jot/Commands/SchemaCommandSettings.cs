
using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class SchemaCommandSettings : CommandSettings
{
    public const int ARG_POSITION_NAME = 0;

    [Description("Name of the schema to target")]
    [CommandArgument(ARG_POSITION_NAME, "<SCHEMA_NAME>")]
    public string? SchemaName { get; init; }

    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(SchemaName)
            ? ValidationResult.Error("Name must either be the GUID of a schema or a name that resolves to just one")
            : ValidationResult.Success();
    }
}
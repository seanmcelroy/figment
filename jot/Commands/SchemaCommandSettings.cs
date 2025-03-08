
using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class SchemaCommandSettings : CommandSettings
{
    public const int ARG_POSITION_SCHEMA_NAME = 0;

    [Description("Name of the schema to target")]
    [CommandArgument(ARG_POSITION_SCHEMA_NAME, "<SCHEMA>")]
    public string? SchemaName { get; init; }

    [Description("Provides verbose detail, if available, for any outputs")]
    [CommandOption("-v")]
    public required bool? Verbose { get; init; } = Program.Verbose;

    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(SchemaName)
            ? ValidationResult.Error("Name must either be the GUID of a schema or a name that resolves to just one")
            : ValidationResult.Success();
    }
}
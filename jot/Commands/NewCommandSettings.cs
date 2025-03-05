
using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class NewCommandSettings : CommandSettings
{
    public const int ARG_POSITION_SCHEMA = 0;

    [Description("The type of entity to create.  If this schema does not exist, it will be created.")]
    [CommandArgument(ARG_POSITION_SCHEMA, "<SCHEMA>")]
    public string? SchemaName { get; init; }

    public const int ARG_POSITION_NAME = 1;

    [Description("The name of the new entity.  If omitted, only a schema will be created but no thing of that schema's type.")]
    [CommandArgument(ARG_POSITION_NAME, "[NAME]")]
    public string? ThingName { get; init; }

    [Description("The value indicating whether a schema should be created by default if one with a matching <SCHEMA> name does not exist.")]
    [CommandOption("--auto-create-schema")]
    [DefaultValue(true)]
    public bool? AutoCreateSchema { get; init; } = true;

    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(SchemaName)
            ? ValidationResult.Error("Schema must either be the GUID or a name that resolves to just one")
            : ValidationResult.Success();
    }
}
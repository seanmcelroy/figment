using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands;

public class ListSelectedSchemaMembersCommandSettings : CommandSettings
{
    public const int ARG_POSITION_PROPERTY_SCHEMA = 0;

    [Description("The name of the schema to enumerate members, if one is not already selected")]
    [CommandArgument(ARG_POSITION_PROPERTY_SCHEMA, "[SCHEMA]")]
    public string? SchemaName { get; init; }
}
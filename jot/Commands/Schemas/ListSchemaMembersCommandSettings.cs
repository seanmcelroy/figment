using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

public class ListSchemaMembersCommandSettings : SchemaCommandSettings
{
    [Description("Outputs the list in a human-readable tabular format")]
    [CommandOption("--as-table")]
    public bool? AsTable { get; init; } = false;
}
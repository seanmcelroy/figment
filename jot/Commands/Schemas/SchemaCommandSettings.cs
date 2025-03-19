using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

/// <summary>
/// The settings supplied to commands that only need to target a <see cref="Schema"/>
/// and have no other arguments.
/// </summary>
/// <seealso cref="DeleteSchemaCommand"/>
/// <seealso cref="PrintSchemaCommand"/>
/// <seealso cref="ValidateSchemaCommand"/>
public class SchemaCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets the name of the <see cref="Schema"/> to target.
    /// </summary>
    [Description("Name of the schema to target")]
    [CommandArgument(0, "<SCHEMA>")]
    required public string SchemaName { get; init; }

    /// <summary>
    /// Gets whether to provide verbose detail, if available, for any outputs.
    /// </summary>
    [Description("Provides verbose detail, if available, for any outputs")]
    [CommandOption("-v|--verbose")]
    required public bool? Verbose { get; init; } = Program.Verbose;

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(SchemaName)
            ? ValidationResult.Error("Name must either be the GUID of a schema or a name that resolves to just one")
            : ValidationResult.Success();
    }
}
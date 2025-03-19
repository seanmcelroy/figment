using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

/// <summary>
/// The settings supplied to the <see cref="SetSchemaDescriptionCommand"/>.
/// </summary>
public class SetSchemaDescriptionCommandSettings : SchemaCommandSettings
{
    /// <summary>
    /// Gets the description of the <see cref="Schema"/>.  If blank, the description will be cleared.
    /// </summary>
    [Description("Description of the schema. If blank, the description will be cleared")]
    [CommandArgument(0, "[DESCRIPTION]")]
    public string? Description { get; init; }
}
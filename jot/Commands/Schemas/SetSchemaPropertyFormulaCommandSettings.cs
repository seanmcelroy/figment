using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

/// <summary>
/// The settings supplied to the <see cref="SetSchemaPropertyFormulaCommand"/>.
/// </summary>
public class SetSchemaPropertyFormulaCommandSettings : SchemaPropertyCommandSettings
{
    /// <summary>
    /// Gets the formula to use if the field type is 'calculated'.
    /// </summary>
    /// <seealso cref="SchemaCalculatedField"/>
    [Description("If the field type is 'calculated', this is the formula to use")]
    [CommandArgument(0, "[FORMULA]")]
    public string? Formula { get; init; }
}
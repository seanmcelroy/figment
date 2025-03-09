using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

public class SetSchemaPropertyFormulaCommandSettings : SchemaPropertyCommandSettings
{
    public const int ARG_POSITION_FORMULA = 0;

    [Description("If the field type is 'calculated', this is the formula to use")]
    [CommandArgument(ARG_POSITION_FORMULA, "[FORMULA]")]
    public string? Formula { get; init; }
}
using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

/// <summary>
/// The settings supplied to the <see cref="SetSchemaPluralCommand"/>.
/// </summary>
public class SetSchemaPluralCommandSettings : SchemaCommandSettings
{
    /// <summary>
    /// Gets the plural word for items of this schema, used as a keyword to enumerate items in interactive mode.
    /// </summary>
    [Description("Plural word for items of this schema, used as a keyword to enumerate items in interactive mode")]
    [CommandArgument(0, "[PLURAL]")]
    public string? Plural { get; init; }
}
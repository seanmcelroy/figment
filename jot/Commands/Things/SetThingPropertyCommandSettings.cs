using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Things;

/// <summary>
/// The settings supplied to the <see cref="SetThingPropertyCommand"/>.
/// </summary>
public class SetThingPropertyCommandSettings : ThingCommandSettings
{
    /// <summary>
    /// Gets the name of the property to change.
    /// </summary>
    [Description("Name of the property to change")]
    [CommandArgument(0, "<PROPERTY>")]
    required public string PropertyName { get; init; }

    /// <summary>
    /// Gets the value to set, or to delete if blank.
    /// </summary>
    [Description("Value to set, or to delete if blank")]
    [CommandArgument(1, "[VALUE]")]
    public string? Value { get; init; }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(PropertyName)
            ? ValidationResult.Error("Property name must be set")
            : ValidationResult.Success();
    }
}
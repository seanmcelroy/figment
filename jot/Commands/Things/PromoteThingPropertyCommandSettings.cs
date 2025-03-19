using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Things;

/// <summary>
/// The settings supplied to the <see cref="PromoteThingPropertyCommand"/>.
/// </summary>
public class PromoteThingPropertyCommandSettings : ThingCommandSettings
{
    /// <summary>
    /// Gets the name of the property to promote.
    /// </summary>
    [Description("Name of the property to promote")]
    [CommandArgument(0, "<PROPERTY>")]
    required public string PropertyName { get; init; }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(PropertyName)
            ? ValidationResult.Error("Property name must be set")
            : ValidationResult.Success();
    }
}
using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

/// <summary>
/// The settings supplied to the <see cref="PromoteSelectedPropertyCommand"/>.
/// </summary>
public class PromoteSelectedPropertyCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets the name of the property to promote.
    /// </summary>
    [Description("Name of the property to promote")]
    [CommandArgument(0, "<PROPERTY>")]
    public string? PropertyName { get; init; }

    /// <summary>
    /// Gets whether to provide verbose detail, if available, for any outputs.
    /// </summary>
    [Description("Provides verbose detail, if available, for any outputs")]
    [CommandOption("-v|--verbose")]
    required public bool? Verbose { get; init; } = Program.Verbose;

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(PropertyName)
            ? ValidationResult.Error("Property name must be set")
            : ValidationResult.Success();
    }
}
using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

/// <summary>
/// The settings supplied to the <see cref="SetSelectedPropertyCommand"/>.
/// </summary>
public class SetSelectedPropertyCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets the name of the property to change.
    /// </summary>
    [Description("Name of the property to change")]
    [CommandArgument(0, "<PROPERTY>")]
    public string? PropertyName { get; init; }

    /// <summary>
    /// Gets the subcommand and parameter specifying what to change.
    /// </summary>
    [Description("Subcommand and parameter specifying what to change")]
    [CommandArgument(1, "[VALUES]")]
#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
    public string[]? Values { get; init; }
#pragma warning restore SA1011 // Closing square brackets should be spaced correctly

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
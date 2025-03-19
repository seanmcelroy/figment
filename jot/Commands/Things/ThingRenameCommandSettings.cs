using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Things;

/// <summary>
/// The settings supplied to the <see cref="ThingRenameCommand"/>.
/// </summary>
public class ThingRenameCommandSettings : ThingCommandSettings
{
    /// <summary>
    /// Gets the new name for the <see cref="Thing"/>.
    /// </summary>
    [Description("New name for the thing")]
    [CommandArgument(0, "<NEW_NAME>")]
    required public string NewName { get; init; }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(NewName)
            ? ValidationResult.Error("New name must be provided and cannot be blank or only whitespaces")
            : ValidationResult.Success();
    }
}

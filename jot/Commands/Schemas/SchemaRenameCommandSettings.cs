using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

/// <summary>
/// The settings supplied to the <see cref="SchemaRenameCommand"/>.
/// </summary>
public class SchemaRenameCommandSettings : SchemaCommandSettings
{
    /// <summary>
    /// Gets the new name for the <see cref="Schema"/>.  This is usually one singular term.
    /// </summary>
    [Description("New name for the schema.  This is usually one singular term")]
    [CommandArgument(0, "<NEW_NAME>")]
    required public string NewName { get; init; }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(NewName)
            ? ValidationResult.Error("New schema name must be provided and cannot be blank or only whitespaces")
            : ValidationResult.Success();
    }
}

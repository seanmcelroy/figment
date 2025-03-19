using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

/// <summary>
/// The settings supplied to the <see cref="NewCommand"/>.
/// </summary>
public class NewCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets the name of the <see cref="Schema"/> for which to create a new entity.  If this schema does not exist, it will be created.
    /// </summary>
    [Description("Type of entity to create.  If this schema does not exist, it will be created")]
    [CommandArgument(0, "<SCHEMA>")]
    public string? SchemaName { get; init; }

    /// <summary>
    /// Gets the name of the new <see cref="Thing"/>.  If omitted, only a <see cref="Schema"/> will be created but no thing of that schema's type.
    /// </summary>
    [Description("Name of the new entity.  If omitted, only a schema will be created but no thing of that schema's type")]
    [CommandArgument(1, "[NAME]")]
    public string? ThingName { get; init; }

    /// <summary>
    /// Gets a value indicating whether a <see cref="Schema"/> should be created by default if one with a matching <see cref="SchemaName"/> does not exist.
    /// </summary>
    [Description("Value indicating whether a schema should be created by default if one with a matching <SCHEMA> name does not exist")]
    [CommandOption("--auto-create-schema")]
    [DefaultValue(true)]
    public bool? AutoCreateSchema { get; init; } = true;

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(SchemaName)
            ? ValidationResult.Error("Schema must either be the GUID or a name that resolves to just one")
            : ValidationResult.Success();
    }
}
using System.ComponentModel;
using System.Globalization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class SetSchemaPropertyDisplayCommandSettings : SchemaPropertyCommandSettings
{
    public const int ARG_POSITION_PRETTY_NAME = 0;
    [Description("Text to display instead of the actual property name.  If not specified, the display name will be deleted.")]
    [CommandArgument(ARG_POSITION_PRETTY_NAME, "<PRETTY_NAME>")]
    public string? DisplayName { get; init; }

    public const int ARG_POSITION_CULTURE = 1;
    [Description("Culture to use this display name.  If not specified, is en-US.")]
    [CommandArgument(ARG_POSITION_CULTURE, "[CULTURE]")]
    public string? Culture { get; init; } = "en-US";

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(PropertyName))
            return ValidationResult.Error("Property name must be set");

        if (!string.IsNullOrWhiteSpace(Culture)
            && !CultureInfo.GetCultures(CultureTypes.AllCultures).Any(c => string.Compare(c.Name, Culture, StringComparison.InvariantCultureIgnoreCase) == 0))
            return ValidationResult.Error("Invalid culture specified.  Try 'en-US' if unsure.");

        return ValidationResult.Success();
    }
}
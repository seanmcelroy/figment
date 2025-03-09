using Figment.Common;
using Figment.Common.Errors;
using jot.Commands.Schemas;
using jot.Commands.Things;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class SetSelectedPropertyCommand : CancellableAsyncCommand<SetSelectedPropertyCommandSettings>, ICommand
{
    private static void PrintSchemaSubcommandHelp()
    {
        AmbientErrorContext.Provider.LogError("Subcommand and argument not provided.\r\n");

        AnsiConsole.MarkupLine("[gold3_1]USAGE:[/]");
        AnsiConsole.MarkupLine("    set [aqua]<PROPERTY>[/] [lime]<SUBCOMMAND>[/] <SUBCOMMAND VALUES>\r\n");

        AnsiConsole.MarkupLine("[gold3_1]SCHEMA SET SUBCOMMANDS AND VALUES:[/]");
        AnsiConsole.MarkupLine("    [lime]display[/] <PRETTY_NAME> [[CULTURE]]");
        AnsiConsole.MarkupLine("    [lime]type[/] [[FIELD_TYPE]] (Leave blank to delete the field)");
        AnsiConsole.MarkupLine("    [lime]require[/] <REQUIRED>");
        AnsiConsole.MarkupLine("    [lime]formula[/] <FORMULA>");
    }

    public override async Task<int> ExecuteAsync(CommandContext context, SetSelectedPropertyCommandSettings settings, CancellationToken cancellationToken)
    {
        if (Program.SelectedEntity.Equals(Reference.EMPTY))
        {
            AmbientErrorContext.Provider.LogError("To set properties on an entity in interactive mode, you must first 'select' it.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        switch (Program.SelectedEntity.Type)
        {
            case Reference.ReferenceType.Schema:
                {
                    if (context.Arguments.Count < 3)
                    {
                        PrintSchemaSubcommandHelp();
                        return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
                    }

                    if (string.Compare("display", context.Arguments[2], StringComparison.CurrentCultureIgnoreCase) == 0)
                    {
                        var subset = new SetSchemaPropertyDisplayCommandSettings
                        {
                            SchemaName = Program.SelectedEntity.Guid,
                            PropertyName = settings.PropertyName,
                            DisplayName = context.Arguments.Count < 4 ? null : context.Arguments[3],
                            Culture = context.Arguments.Count < 5 ? null : context.Arguments[4],
                            Verbose = settings.Verbose
                        };
                        var cmd = new SetSchemaPropertyDisplayCommand();
                        return await cmd.ExecuteAsync(context, subset, cancellationToken);
                    }

                    if (string.Compare("type", context.Arguments[2], StringComparison.CurrentCultureIgnoreCase) == 0)
                    {
                        var subset = new SetSchemaPropertyTypeCommandSettings
                        {
                            SchemaName = Program.SelectedEntity.Guid,
                            PropertyName = settings.PropertyName,
                            FieldType = context.Arguments.Count < 4 ? null : context.Arguments[3],
                            Verbose = settings.Verbose
                        };
                        var cmd = new SetSchemaPropertyTypeCommand();
                        return await cmd.ExecuteAsync(context, subset, cancellationToken);
                    }

                    if (string.Compare("require", context.Arguments[2], StringComparison.CurrentCultureIgnoreCase) == 0)
                    {
                        var parsable = SchemaBooleanField.TryParseBoolean(context.Arguments.Count < 4 ? null : context.Arguments[3], out bool required);
                        if (!parsable)
                        {
                            AmbientErrorContext.Provider.LogError($"Unable to parse '{context.Arguments[3]}' as a true/false boolean variable.");
                            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
                        }
                        var subset = new SetSchemaPropertyRequiredCommandSettings
                        {
                            SchemaName = Program.SelectedEntity.Guid,
                            PropertyName = settings.PropertyName,
                            Required = required,
                            Verbose = settings.Verbose
                        };
                        var cmd = new SetSchemaPropertyRequiredCommand();
                        return await cmd.ExecuteAsync(context, subset, cancellationToken);
                    }

                    if (string.Compare("formula", context.Arguments[2], StringComparison.CurrentCultureIgnoreCase) == 0)
                    {
                        var subset = new SetSchemaPropertyFormulaCommandSettings
                        {
                            SchemaName = Program.SelectedEntity.Guid,
                            PropertyName = settings.PropertyName,
                            Formula = context.Arguments.Count < 4 ? null : context.Arguments[3],
                            Verbose = settings.Verbose
                        };
                        var cmd = new SetSchemaPropertyFormulaCommand();
                        return await cmd.ExecuteAsync(context, subset, cancellationToken);
                    }

                    AmbientErrorContext.Provider.LogError($"Unsupported subcommand '{context.Arguments[2]}' in interactive mode.");
                    PrintSchemaSubcommandHelp();
                    return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
                }
            case Reference.ReferenceType.Thing:
                {
                    var cmd = new SetThingPropertyCommand();
                    return await cmd.ExecuteAsync(context, new SetThingPropertyCommandSettings { ThingName = Program.SelectedEntity.Guid, PropertyName = settings.PropertyName, Value = settings.Values?[0], Verbose = settings.Verbose }, cancellationToken);
                }
            default:
                {
                    AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(Program.SelectedEntity.Type)}'.");
                    return (int)Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE;
                }
        }
    }
}
using Figment.Common;
using Figment.Common.Errors;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class SetSelectedPropertyCommand : CancellableAsyncCommand<SetSelectedPropertyCommandSettings>, ICommand
{
    private enum ERROR_CODES : int
    {
        ARGUMENT_ERROR = Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR,
        UNKNOWN_TYPE = Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE,
    }

    private static void PrintSubcommandHelp()
    {
        AmbientErrorContext.Provider.LogError("Subcommand and argument not provided.\r\n");
        AnsiConsole.MarkupLine("[gold3_1]SUBCOMMANDS:[/]");
        AnsiConsole.WriteLine("    type <FIELD_NAME>");
        AnsiConsole.WriteLine("    require <REQUIRED>");
        AnsiConsole.WriteLine("    formula <FORMULA>");
    }

    public override async Task<int> ExecuteAsync(CommandContext context, SetSelectedPropertyCommandSettings settings, CancellationToken cancellationToken)
    {
        if (Program.SelectedEntity.Equals(Reference.EMPTY))
        {
            AmbientErrorContext.Provider.LogError("To set properties on an entity in interactive mode, you must first 'select' it.");
            return (int)ERROR_CODES.ARGUMENT_ERROR;
        }

        switch (Program.SelectedEntity.Type)
        {
            case Reference.ReferenceType.Schema:
                {
                    if (context.Arguments.Count < 4)
                    {
                        PrintSubcommandHelp();
                        return (int)ERROR_CODES.ARGUMENT_ERROR;
                    }

                    if (string.Compare("type", context.Arguments[2], StringComparison.CurrentCultureIgnoreCase) == 0)
                    {
                        var subset = new SetSchemaPropertyTypeCommandSettings
                        {
                            SchemaName = Program.SelectedEntity.Guid,
                            PropertyName = settings.PropertyName,
                            FieldType = context.Arguments[3],
                        };
                        var cmd = new SetSchemaPropertyTypeCommand();
                        return await cmd.ExecuteAsync(context, subset, cancellationToken);
                    }

                    if (string.Compare("require", context.Arguments[2], StringComparison.CurrentCultureIgnoreCase) == 0)
                    {
                        var parsable = SchemaBooleanField.TryParseBoolean(context.Arguments[3], out bool required);
                        if (!parsable)
                        {
                            AmbientErrorContext.Provider.LogError($"Unable to parse '{context.Arguments[3]}' as a true/false boolean variable.");
                            return (int)ERROR_CODES.ARGUMENT_ERROR;
                        }
                        var subset = new SetSchemaPropertyRequiredCommandSettings
                        {
                            SchemaName = Program.SelectedEntity.Guid,
                            PropertyName = settings.PropertyName,
                            Required = required,
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
                            Formula = context.Arguments[3],
                        };
                        var cmd = new SetSchemaPropertyFormulaCommand();
                        return await cmd.ExecuteAsync(context, subset, cancellationToken);
                    }

                    AmbientErrorContext.Provider.LogError($"Unsupported subcommand '{context.Arguments[2]}' in interactive mode.");
                    PrintSubcommandHelp();
                    return (int)ERROR_CODES.ARGUMENT_ERROR;
                }
            case Reference.ReferenceType.Thing:
                {
                    var cmd = new SetThingPropertyCommand();
                    return await cmd.ExecuteAsync(context, new SetThingPropertyCommandSettings { ThingName = Program.SelectedEntity.Guid, PropertyName = settings.PropertyName, Value = settings.Values?[0] }, cancellationToken);
                }
            default:
                {
                    AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(Program.SelectedEntity.Type)}'.");
                    return (int)ERROR_CODES.UNKNOWN_TYPE;
                }
        }
    }
}
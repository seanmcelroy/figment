using Figment;
using Figment.Data;
using jot.Commands;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace jot;

internal class Program
{
    /// <summary>
    /// For interactive mode, this is the currently selected entity that commands can reference in a REPL.
    /// </summary>
    internal static Reference SelectedEntity = Reference.EMPTY;

    private static async Task<int> Main(string[] args)
    {
        AnsiConsole.MarkupLine("[bold fuchsia]jot[/] v0.0.1");
        var cts = new CancellationTokenSource();

        var interactive = args.Length == 0
            && (AnsiConsole.Profile.Capabilities.Interactive || Debugger.IsAttached);

        // Setup the storage provider. TODO: Allow CLI config
        StorageUtility.StorageProvider = new LocalDirectoryStorageProvider();

        var app = new CommandApp();
        app.Configure(config =>
        {
            config.AddCommand<NewCommand>("new")
                .WithDescription("Creates new types (schemas) or instances of things");
            config.AddBranch("reindex", schema =>
                {
                    schema.AddCommand<ReindexSchemasCommand>("schemas")
                        .WithDescription("Rebuilds the index files for schemas for consistency");
                    schema.AddCommand<ReindexThingsCommand>("things")
                        .WithDescription("Rebuilds the index files for things for consistency");
                });
            config.AddCommand<ListSchemasCommand>("schemas")
                .WithDescription("Lists all the schemas in the database");
            config.AddBranch<SchemaCommandSettings>("schema", schema =>
                {
                    schema.AddCommand<AssociateSchemaWithThingCommand>("associate")
                        .WithDescription("Associates a thing with a schema");
                    schema.AddCommand<DissociateSchemaFromThingCommand>("dissociate")
                        .WithDescription("Dissociates a thing from a schema");
                    schema.AddCommand<RequireSchemaPropertyCommand>("require")
                        .WithDescription("Changes whether a property is required");
                    schema.AddCommand<SetSchemaPropertyCommand>("set")
                        .WithDescription("Sets the data type of a property");
                    schema.AddCommand<ValidateSchemaCommand>("validate")
                        .WithDescription("Validates the schema is consistent");
                    schema.AddCommand<PrintSchemaCommand>("view")
                        .WithAlias("print")
                        .WithDescription("Views all fields on a schema");
                });
            config.AddCommand<ListThingsCommand>("things")
                .WithDescription("Lists all the things in the database");
            config.AddBranch<ThingCommandSettings>("thing", thing =>
                {
                    thing.AddCommand<DeleteThingCommand>("delete")
                        .WithDescription("Permanently deletes a thing");
                    thing.AddCommand<PromoteThingPropertyCommand>("promote");
                    thing.AddCommand<SetThingPropertyCommand>("set")
                        .WithDescription("Sets the value of a property on a thing");
                    thing.AddCommand<ValidateThingCommand>("validate")
                        .WithDescription("Validates a thing is consistent with its schema");
                    thing.AddCommand<PrintThingCommand>("view")
                        .WithAlias("print")
                        .WithDescription("Views the values of all properties on a thing");
                });

            // Interactive commands
            if (interactive)
            {
                config.AddCommand<HelpCommand>("ihelp");

                config.AddCommand<DeleteSelectedCommand>("delete")
                    .WithAlias("del")
                    .WithDescription("Interactive mode command.  Deletes the currently selected entity.")
                    .IsHidden();

                config.AddCommand<PrintSelectedCommand>("print")
                    .WithAlias("details")
                    .WithAlias("view")
                    .WithAlias("?")
                    .IsHidden();

                config.AddCommand<PromoteSelectedPropertyCommand>("promote")
                    .IsHidden();

                config.AddCommand<QuitCommand>("quit")
                    .WithAlias("exit")
                    .IsHidden();

                config.AddCommand<SelectCommand>("select")
                    .WithAlias("sel")
                    .WithAlias("s")
                    .IsHidden();

                config.AddCommand<SetSelectedPropertyCommand>("set")
                    .IsHidden();

                config.AddCommand<ValidateSelectedCommand>("validate")
                    .WithAlias("val");
            }
        });

        if (!interactive)
        {
            // Command mode.
            return await app.RunAsync(args);
        }

        // Interactive mode
        AnsiConsole.MarkupLine("\r\njot is running in [bold underline white]interactive mode[/].  Press ctrl-C to exit or type '[purple bold]quit[/]'.");
        AnsiConsole.MarkupLine("\r\nThere are additional undocumented commands in this mode.  Type [purple bold]ihelp[/] for help on this mode interactive.");

        var schemaProvider = StorageUtility.StorageProvider.GetSchemaStorageProvider();
        if (schemaProvider == null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load schema storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var thingProvider = StorageUtility.StorageProvider.GetThingStorageProvider();
        if (thingProvider == null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load thing storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        do
        {
            string? input;
            if (AnsiConsole.Profile.Capabilities.Interactive)
            {
                input = AnsiConsole.Prompt(
                   new TextPrompt<string>("[green]>[/]")

               );
            }
            else
            {
                // Handle vscode Debug Console
                Console.Write("> ");
                input = Console.ReadLine();
            }

            var inputArgs = Globals.SplitArgs(input);

            // First, let's see if they are typing a plural schemas command.
            if (!string.IsNullOrWhiteSpace(input))
            {
                var handled = false;
                await foreach (var schemaRef in schemaProvider.FindByPluralNameAsync(input, cts.Token))
                {
                    var schema = await schemaProvider.LoadAsync(schemaRef.Guid, cts.Token);
                    if (schema != null)
                    {
                        await foreach (var thingRef in thingProvider.GetBySchemaAsync(schema.Guid, cts.Token))
                        {
                            var thing = await thingProvider.LoadAsync(thingRef.Guid, cts.Token);
                            if (thing != null)
                                await Console.Out.WriteLineAsync(thing.Name);
                        }
                        handled = true;
                        break;
                    }
                }
                if (handled)
                    continue;
            }

            // Proceed as normal in interactive mode
            var result = await app.RunAsync(inputArgs);

        } while (!cts.Token.IsCancellationRequested);


        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}
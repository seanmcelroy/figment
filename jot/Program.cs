using Spectre.Console;
using System.Runtime.CompilerServices;
using Figment;
using jot.Commands;
using Spectre.Console.Cli;
using System.Diagnostics;

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
                await foreach (var schemaRef in Schema.ResolvePluralNameAsync(input, cts.Token))
                {
                    var schema = await Schema.LoadAsync(schemaRef.Guid, cts.Token);
                    if (schema != null)
                    {
                        await foreach (var thing in Thing.GetBySchema(schema.Guid, cts.Token))
                        {
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

    public static async IAsyncEnumerable<string> ResolveGuidFromPartialNameAsync(string indexFile, string namePart, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Load index
        if (!File.Exists(indexFile))
            yield break; // Happens on new install if no items, nothing in index, and so no file

        await foreach (var entry in IndexManager.LookupAsync(
            indexFile
            , e => e.Key.StartsWith(namePart, StringComparison.CurrentCultureIgnoreCase)
            , cancellationToken
        ))
        {
            var fileName = entry.Value;
            if (Path.IsPathFullyQualified(fileName))
                yield return Path.GetFileName(fileName).Split('.')[0];
            else
                yield return fileName.Split('.')[0];
        }
    }

    public static async IAsyncEnumerable<string> ResolveGuidFromExactNameAsync(string indexFile, string name, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Load index
        if (!File.Exists(indexFile))
            yield break; // Happens on new install if no items, nothing in index, and so no file

        await foreach (var entry in IndexManager.LookupAsync(
            indexFile
            , e => string.Compare(e.Key, name, StringComparison.CurrentCultureIgnoreCase) == 0
            , cancellationToken
        ))
        {
            var fileName = entry.Value;
            if (Path.IsPathFullyQualified(fileName))
            {
                // Full file path
                var guid = Path.GetFileName(fileName).Split('.')[0];
                yield return guid;
            }
            else
            {
                // Filename only
                var guid = fileName.Split('.')[0];
                yield return guid;
            }
        }
    }
}
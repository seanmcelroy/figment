using jot.Commands;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics;
using Figment.Common.Data;
using Figment.Common;
using Figment.Common.Errors;
using jot.Errors;

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

        // Setup the providers. TODO: Allow CLI config
        AmbientErrorContext.ErrorProvider = new SpectreConsoleErrorProvider();
        AmbientStorageContext.StorageProvider = new Figment.Data.Local.LocalDirectoryStorageProvider();

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

        var schemaProvider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        if (schemaProvider == null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load schema storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var thingProvider = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
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
                Schema? schema = null;
                List<Thing> thingsToDisplay = [];
                await foreach (var schemaRef in schemaProvider.FindByPluralNameAsync(input, cts.Token))
                {
                    schema = await schemaProvider.LoadAsync(schemaRef.Guid, cts.Token);
                    if (schema != null)
                    {
                        await foreach (var thingRef in thingProvider.GetBySchemaAsync(schema.Guid, cts.Token))
                        {
                            var thing = await thingProvider.LoadAsync(thingRef.Guid, cts.Token);
                            if (thing != null)
                                thingsToDisplay.Add(thing);
                        }
                        handled = true;
                        break;
                    }
                }
                if (handled)
                {
                    // Display, then 'continue' to break out of await-for.
                    await RenderView(schema!, thingsToDisplay, cts.Token);
                    continue;
                }
            }

            // Proceed as normal in interactive mode
            var result = await app.RunAsync(inputArgs);

        } while (!cts.Token.IsCancellationRequested);


        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }

    private static async Task RenderView(
        Schema commonSchema,
        IEnumerable<Thing> thingsToDisplay,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(commonSchema);

        // Is there a view for this schema?
        var ssp = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        var tsp = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        if (ssp != null && tsp != null)
        {
            var viewSchemaRef = await ssp.FindByNameAsync("view", cancellationToken);
            if (!viewSchemaRef.Equals(Reference.EMPTY))
            {
                await foreach (var viewRef in tsp.GetBySchemaAsync(viewSchemaRef.Guid, cancellationToken))
                {
                    var viewInstance = await tsp.LoadAsync(viewRef.Guid, cancellationToken);
                    if (viewInstance != null
                        && viewInstance.Properties.TryGetValue($"{viewSchemaRef.Guid}.for", out object? forSchemaGuidObject)
                        && forSchemaGuidObject != null
                        && forSchemaGuidObject is string forSchemaGuid
                        && !string.IsNullOrWhiteSpace(forSchemaGuid)
                        && string.CompareOrdinal(forSchemaGuid, commonSchema.Guid) == 0
                        && await ssp.GuidExists(forSchemaGuid, cancellationToken))
                    {
                        // Found a matching view!
                        var anyDisplayColumns = viewInstance.Properties.TryGetValue($"{viewSchemaRef.Guid}.displayColumns", out object? viewColumnsObject);
                        var schema = await ssp.LoadAsync(forSchemaGuid, cancellationToken);

                        Console.Error.WriteLine($"DEBUG: Using view '{viewInstance.Name}'");

                        if (anyDisplayColumns
                            && viewColumnsObject is System.Collections.IEnumerable viewColumns
                            && schema != null)
                        {
                            // Do it this way so we preserve viewColumns order
                            List<string> columns = [];
                            var viewableColumns = schema.Properties.Select(p => p.Key)
                                .Union([nameof(Thing.Name), nameof(Thing.Guid)])
                                .ToArray();
                            foreach (var vc in viewColumns.Cast<object?>().Select(v => v?.ToString()))
                            {
                                if (!string.IsNullOrWhiteSpace(vc) && viewableColumns.Contains(vc, StringComparer.InvariantCultureIgnoreCase))
                                    columns.Add(vc);
                            }

                            Table t = new();
                            foreach (var col in columns)
                                t.AddColumn(col);

                            foreach (var thing in thingsToDisplay)
                            {
                                Dictionary<string, object?> cellValues = [];
                                // Add special view built-ins
                                cellValues.Add(nameof(Thing.Name).ToLowerInvariant(), thing.Name);
                                cellValues.Add(nameof(Thing.Guid).ToLowerInvariant(), thing.Guid);
                                await foreach (var thingProperty in thing.GetProperties(cancellationToken))
                                {
                                    if (columns.Contains(thingProperty.SimpleDisplayName, StringComparer.InvariantCultureIgnoreCase)
                                        && !cellValues.ContainsKey(thingProperty.SimpleDisplayName))
                                        cellValues.Add(thingProperty.SimpleDisplayName.ToLowerInvariant(), thingProperty.Value);
                                }

                                List<string> cells = [];
                                foreach (var col in columns)
                                {
                                    if (!cellValues.TryGetValue(col.ToLowerInvariant(), out object? cellValue))
                                        cells.Add("[red]<UNSET>[/]");
                                    else
                                        cells.Add(cellValue?.ToString() ?? string.Empty);
                                }
                                t.AddRow(cells.ToArray());
                            }
                            AnsiConsole.Write(t);
                            return;
                        }
                    }
                }
            }
        }

        // Fallback
        foreach (var thing in thingsToDisplay)
        {
            await Console.Out.WriteLineAsync(thing.Name);
        }

    }
}
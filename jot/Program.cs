/*
Figment
Copyright (C) 2025  Sean McElroy

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Diagnostics;
using System.Globalization;
using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using Figment.Data.Memory;
using jot.Commands;
using jot.Commands.Interactive;
using jot.Commands.Pomodoro;
using jot.Commands.Schemas;
using jot.Commands.Schemas.ImportMaps;
using jot.Commands.Tasks;
using jot.Commands.Things;
using jot.Errors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;
using Spectre.Console.Cli;
using TextPromptWithHistory;

namespace jot;

/// <summary>
/// The console application for jot.
/// </summary>
internal class Program
{
#pragma warning disable SA1401 // Fields should be private
    /// <summary>
    /// For interactive mode, this is the currently selected entity that commands can reference in a REPL.
    /// </summary>
    internal static Reference SelectedEntity = Reference.EMPTY;

    /// <summary>
    /// The name of the <see cref="SelectedEntity"/>.
    /// </summary>
    internal static string SelectedEntityName = string.Empty;

    /// <summary>
    /// Whether commands should provide verbose output.
    /// </summary>
    internal static bool Verbose = false;
#pragma warning restore SA1401 // Fields should be private

    private static async Task<int> Main(string[] args)
    {
        using var cts = new CancellationTokenSource();

        var interactive = args.Length == 0
            && (AnsiConsole.Profile.Capabilities.Interactive || Debugger.IsAttached);

        // Build host
        var hostBuilder = Host.CreateDefaultBuilder();
        using var host = hostBuilder.Build();
        var registrar = new TypeRegistrar(hostBuilder, host);

        // Pre-run configuration
        var config = registrar.Host?.Services.GetRequiredService<IConfiguration>();
        Queue<Action> postBannerActionQueue = new();

        // // Error Provider
        if (interactive)
        {
            var ep = new SpectreConsoleErrorProvider();
            AmbientErrorContext.Provider = ep;
        }

        // // Storage provider
        {
            var storageConfig = config?.GetSection("StorageProvider");
            Dictionary<string, string> storageSettings;

            string storageProviderType;
            if (storageConfig == null)
            {
                // No storage provider is configured.
                AmbientErrorContext.Provider.LogWarning("No storage provider configuration found.  Proceeding with an in-memory configuration.");
                storageProviderType = MemoryStorageProvider.PROVIDER_TYPE;
                storageSettings = [];
            }
            else
            {
                var spt = storageConfig?.GetValue<string>("Type");
                if (string.IsNullOrWhiteSpace(spt))
                {
                    AmbientErrorContext.Provider.LogWarning("Storage provider type not specified.  Proceeding with an in-memory configuration.");
                    storageProviderType = MemoryStorageProvider.PROVIDER_TYPE;
                    storageSettings = [];
                }
                else
                {
                    storageProviderType = spt;
                    var settingsSection = storageConfig!.GetSection("Settings");
                    if (settingsSection == null)
                    {
                        AmbientErrorContext.Provider.LogError("Missing required settings section");
                        return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR; // TODO: Config error, perhaps.
                    }

                    storageSettings = settingsSection.Get<Dictionary<string, string>>() ?? [];
                }
            }

            switch (storageProviderType)
            {
                case Figment.Data.Local.LocalDirectoryStorageProvider.PROVIDER_TYPE:
                    var ldsp = new Figment.Data.Local.LocalDirectoryStorageProvider();
                    if (storageSettings.TryGetValue(Figment.Data.Local.LocalDirectoryStorageProvider.SETTINGS_KEY_DB_PATH, out string? dataDir))
                    {
                        if (interactive)
                        {
                            postBannerActionQueue.Enqueue(() => AmbientErrorContext.Provider.LogInfo($"Using local storage of database at {ldsp.DatabasePath}"));
                        }
                    }
                    else
                    {
                        AmbientErrorContext.Provider.LogError($"Missing required setting: {Figment.Data.Local.LocalDirectoryStorageProvider.SETTINGS_KEY_DB_PATH}");
                        return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR; // TODO: Config error, perhaps.
                    }

                    AmbientStorageContext.StorageProvider = ldsp;
                    break;
                case MemoryStorageProvider.PROVIDER_TYPE:
                    var msp = new MemoryStorageProvider();
                    AmbientStorageContext.StorageProvider = msp;
                    break;
                default:
                    AmbientErrorContext.Provider.LogError($"Unknown storage provider type: {storageProviderType}");
                    return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR; // TODO: Config error, perhaps.
            }

            await AmbientStorageContext.StorageProvider!.InitializeAsync(storageSettings, cts.Token);
        }

        var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "UNKNOWN";

        var app = new CommandApp(registrar);
        app.Configure(config =>
        {
            config.Settings.ApplicationName = "jot";
            config.Settings.ApplicationVersion = version;

            config.AddCommand<NewCommand>("new")
                .WithDescription("Creates new types (schemas) or instances of things");
            config.AddCommand<ListSchemasCommand>("schemas")
                .WithDescription("Lists all the schemas in the database");
            config.AddCommand<ListThingsCommand>("things")
                .WithDescription("Lists all the things in the database")
                .WithExample("things", "--filter", "\"[Email]='me@seanmcelroy.com'\"");

            config.AddBranch("pomodoro", pomo =>
                {
                    pomo.AddCommand<StartPomodoro>("start")
                        .WithDescription("Starts a pomodoro timer");
                })
                .WithAlias("pomo");

            config.AddBranch("task", task =>
                {
                    task.AddCommand<ListTasksCommand>("list")
                        .WithDescription("Lists all tasks")
                        .WithAlias("ls")
                        .WithAlias("l")
                        .WithExample("task", "list", "due:today") // show tasks due today
                        .WithExample("task", "ls", "duebefore:tom") // show tasks due before tomorrow (today and earlier)
                        .WithExample("task", "l", "dueafter:tod") // show tasks due after today
                        .WithExample("task", "l", "completed:true") // show only completed tasks
                        .WithExample("task", "l", "completed:false") // show only incomplete tasks
                        .WithExample("task", "l", "priority:true") // show only prioritized tasks
                        .WithExample("task", "l", "priority:false") // show only non-prioritized tasks
                        .WithExample("task", "l", "archived:true") // show archived tasks
                        .WithExample("task", "l", "completed:tod") // show tasks that were completed today
                        .WithExample("task", "l", "completed:thisweek") // show tasks that were completed this week
                        ;
                    task.AddCommand<AddTaskCommand>("add")
                        .WithDescription("Adds a new task")
                        .WithAlias("a")
                        .WithExample("task", "a", "Prepare meeting notes about +importantProject for the meeting with @bob due:today")
                        ;
                    task.AddCommand<CompleteTaskCommand>("complete")
                        .WithDescription("Marks a task complete")
                        .WithAlias("c")
                        .WithAlias("done")
                        .WithExample("task", "c", "1")
                        ;
                    task.AddCommand<UncompleteTaskCommand>("uncomplete")
                        .WithDescription("Marks a task incomplete")
                        .WithAlias("uc")
                        .WithAlias("undo")
                        .WithExample("task", "uc", "1")
                        ;
                    task.AddCommand<ArchiveTaskCommand>("archive")
                        .WithDescription("Archives a task")
                        .WithAlias("ar")
                        .WithAlias("arc")
                        .WithAlias("arch")
                        .WithExample("task", "archive", "1")
                        .WithExample("task", "ar", "c")
                        .WithExample("task", "ar", "gc")
                        ;
                    task.AddCommand<UnarchiveTaskCommand>("unarchive")
                        .WithDescription("Unarchives a task")
                        .WithAlias("unarch")
                        .WithAlias("una")
                        .WithAlias("uarc")
                        .WithAlias("uar")
                        .WithAlias("ua")
                        .WithExample("task", "uar", "1")
                        ;
                    task.AddCommand<PrioritizeTaskCommand>("prioritize")
                        .WithDescription("Prioritizes a task")
                        .WithAlias("p")
                        .WithAlias("pri")
                        .WithAlias("prio")
                        .WithExample("task", "prioritize", "1")
                        .WithExample("task", "p", "2")
                        ;
                    task.AddCommand<UnprioritizeTaskCommand>("unprioritize")
                        .WithDescription("Unprioritizes a task")
                        .WithAlias("up")
                        .WithAlias("unp")
                        .WithAlias("unpri")
                        .WithAlias("unprio")
                        .WithExample("task", "unprioritize", "1")
                        .WithExample("task", "up", "2")
                        ;
                });

            config.AddBranch<SchemaCommandSettings>("schema", schema =>
                {
                    schema.AddCommand<AssociateSchemaWithThingCommand>("associate")
                        .WithDescription("Associates a thing with a schema")
                        ;
                    schema.AddCommand<SetSchemaDescriptionCommand>("describe")
                        .WithDescription("Changes the description of a schema")
                        ;
                    schema.AddCommand<DeleteSchemaCommand>("delete")
                        .WithAlias("del") // Have grace.
                        .WithAlias("remove") // Have grace.
                        .WithDescription("Permanently deletes a schema")
                        ;
                    schema.AddCommand<DissociateSchemaFromThingCommand>("dissociate")
                        .WithDescription("Dissociates a thing from a schema")
                        ;
                    schema.AddCommand<ImportSchemaThingsCommand>("import")
                        .WithDescription("Imports entities as things of this schema type")
                        .WithExample("schema", "person", "import", "~/Downloads/contacts.csv", "csv")
                        ;
                    schema.AddCommand<ListImportMapsCommand>("import-maps")
                        .WithDescription("Lists all import maps defined on the schema")
                        ;
                    schema.AddBranch<ImportMapCommandSettings>("import-map", map =>
                    {
                        map.AddCommand<NewImportMapCommand>("new")
                            .WithDescription("Creates a new import map to link file fields to schema properties");
                        map.AddCommand<PrintImportMapCommand>("view")
                            .WithAlias("details")
                            .WithAlias("print")
                            .WithAlias("show")
                            .WithDescription("Views all fields on an import map on the schema");
                        map.AddCommand<LinkFileFieldToPropertyCommand>("link")
                            .WithDescription("Links a given source file field to a schema property for imports");
                        map.AddCommand<ImportMapRenameCommand>("rename")
                            .WithAlias("ren")
                            .WithDescription("Changes the name of an import map");
                        map.AddCommand<DeleteImportMapCommand>("delete")
                            .WithAlias("del") // Have grace.
                            .WithAlias("remove") // Have grace.
                            .WithDescription("Deletes an import map from the schema configuration");
                        map.AddCommand<ValidateImportMapCommand>("validate")
                            .WithAlias("val") // Have grace.
                            .WithDescription("Validates an import map on the schema configuration");
                    }).WithAlias("import-maps")
                    ;
                    schema.AddCommand<ListSchemaMembersCommand>("members")
                        .WithDescription("Lists all the things associated with a schema")
                        ;
                    schema.AddCommand<SetSchemaPluralCommand>("plural")
                        .WithDescription("Sets the plural name for the schema")
                        ;
                    schema.AddCommand<SchemaRenameCommand>("rename")
                        .WithAlias("ren")
                        .WithDescription("Changes the name of a schema")
                        ;
                    schema.AddBranch<SchemaPropertyCommandSettings>("set", set =>
                    {
                        // Note, adding one here requires a manual edit to SetSelectedPropertyCommand
                        set.AddCommand<SetSchemaPropertyDisplayCommand>("display")
                            .WithDescription("Sets a 'pretty' display name for the property");
                        set.AddCommand<SetSchemaPropertyTypeCommand>("type")
                            .WithDescription("Sets the data type of a property");
                        set.AddCommand<SetSchemaPropertyRequiredCommand>("require")
                            .WithAlias("required") // Have grace.
                            .WithDescription("Changes whether a property is required");
                        set.AddCommand<SetSchemaPropertyFormulaCommand>("formula")
                            .WithDescription("Sets the formula expression of a calculated property");
                    })
                    ;
                    schema.AddCommand<ValidateSchemaCommand>("validate")
                        .WithAlias("val") // Have grace.
                        .WithDescription("Validates the schema is consistent")
                        ;
                    schema.AddCommand<SetSchemaVersionCommand>("version")
                        .WithDescription("Sets the versioning plan for the schema")
                        ;
                    schema.AddCommand<PrintSchemaCommand>("view")
                        .WithAlias("details")
                        .WithAlias("print")
                        .WithAlias("show")
                        .WithDescription("Views all fields on a schema")
                        ;
                });

            config.AddBranch<ThingCommandSettings>("thing", thing =>
                {
                    thing.AddCommand<DeleteThingCommand>("delete")
                        .WithAlias("del") // Have grace.
                        .WithAlias("remove") // Have grace.
                        .WithDescription("Permanently deletes a thing")
                        ;
                    thing.AddCommand<PromoteThingPropertyCommand>("promote")
                        .WithDescription("Promotes a property on one thing to become a property defined on a schema")
                        ;
                    thing.AddCommand<ThingRenameCommand>("rename")
                        .WithAlias("ren")
                        .WithDescription("Changes the name of a thing")
                        ;
                    thing.AddCommand<SetThingPropertyCommand>("set")
                        .WithDescription("Sets the value of a property on a thing")
                        ;
                    thing.AddCommand<ValidateThingCommand>("validate")
                        .WithAlias("val") // Have grace.
                        .WithDescription("Validates a thing is consistent with its schema")
                        ;
                    thing.AddCommand<PrintThingCommand>("view")
                        .WithAlias("details")
                        .WithAlias("print")
                        .WithAlias("show")
                        .WithDescription("Views the values of all properties on a thing")
                        ;
                });

            config.AddBranch("configure", configure =>
                {
                    configure.AddBranch("initialize", reindex =>
                        {
                            reindex.AddCommand<InitSchemasCommand>("schemas")
                                .WithDescription("Creates built-in schemas from system defaults, overwriting any customizations.");
                        })
                        .WithAlias("init");
                    configure.AddBranch("reindex", reindex =>
                        {
                            reindex.AddCommand<ReindexSchemasCommand>("schemas")
                                .WithDescription("Rebuilds the index files for schemas for consistency");
                            reindex.AddCommand<ReindexThingsCommand>("things")
                                .WithDescription("Rebuilds the index files for things for consistency");
                        });

                    if (interactive)
                    {
                        configure.AddCommand<VerboseCommand>("verbosity")
                            .WithAlias("verbose")
                            .WithDescription("Toggles verbosity.  When verbosity is on, '-v' is specified automatically with every supported command");
                    }
                })
                .WithAlias("config");

            config.AddCommand<HelpCommand>("help")
                .IsHidden();

            // Interactive commands
            if (interactive)
            {
                config.AddCommand<HelpCommand>("ihelp")
                    .WithDescription("Shows additional commands only for this interactive mode");

                config.AddCommand<AssociateSchemaWithSelectedThingCommand>("associate")
                    .WithDescription("Interactive mode command.  Associates the currently selected thing with the specified schema")
                    .IsHidden();

                config.AddCommand<ClearCommand>("clear")
                    .WithDescription("Interactive mode command.  Clears the current console.")
                    .IsHidden();

                config.AddCommand<DescribeSelectedSchemaCommand>("describe")
                    .WithDescription("Interactive mode command.  Sets the description for the currently selected schema")
                    .IsHidden();

                config.AddCommand<DeleteCommand>("delete")
                    .WithAlias("del") // Have grace.
                    .WithAlias("remove") // Have grace.
                    .WithDescription("Deletes an entity by name or ID, or the selected entity if no name is provided.")
                    .IsHidden();

                config.AddCommand<DissociateSchemaFromSelectedThingCommand>("dissociate")
                    .WithDescription("Interactive mode command.  Dissociates the currently selected thing from the specified schema")
                    .IsHidden();

                config.AddCommand<ListSelectedSchemaMembersCommand>("members")
                    .IsHidden();

                config.AddCommand<SetSelectedSchemaPluralCommand>("plural")
                    .WithDescription("Interactive mode command.  Sets the plural name for the schema")
                    .IsHidden();

                config.AddCommand<PrintSelectedCommand>("print")
                    .WithAlias("details")
                    .WithAlias("view")
                    .WithAlias("show")
                    .WithAlias("?")
                    .WithAlias("??")
                    .IsHidden();

                config.AddCommand<PromoteSelectedPropertyCommand>("promote")
                    .IsHidden();

                config.AddCommand<QuitCommand>("quit")
                    .WithAlias("exit")
                    .IsHidden();

                config.AddCommand<RenameSelectedCommand>("rename")
                    .WithAlias("ren")
                    .IsHidden();

                config.AddCommand<SelectCommand>("select")
                    .WithAlias("sel")
                    .WithAlias("s")
                    .WithDescription("Selects an entity by name or ID");

                config.AddCommand<SetSelectedPropertyCommand>("set")
                    .IsHidden(); // TODO this is probably busted with the 'set' refactor

                config.AddCommand<ValidateSelectedCommand>("validate")
                    .WithAlias("val") // Have grace.
                    .IsHidden();
            }
        });

        if (!interactive)
        {
            // Command mode.
            return await app.RunAsync(args);
        }

        // Interactive mode
        AnsiConsole.MarkupLine($"[bold fuchsia]jot[/] version {version}");

        while (postBannerActionQueue.Count > 0)
        {
            var action = postBannerActionQueue.Dequeue();
            action.Invoke();
        }

        AnsiConsole.MarkupLine("\r\njot is running in [bold underline white]interactive mode[/].  Press ctrl-C to exit or type '[purple bold]quit[/]'.");

        var schemaProvider = AmbientStorageContext.StorageProvider?.GetSchemaStorageProvider();
        if (schemaProvider == null)
        {
            AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_SCHEMA_STORAGE_PROVIDER);
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var thingProvider = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (thingProvider == null)
        {
            AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_THING_STORAGE_PROVIDER);
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        List<string> history = [];

        do
        {
            string? input;
            do
            {
                if (AnsiConsole.Profile.Capabilities.Interactive)
                {
                    if (string.IsNullOrWhiteSpace(SelectedEntityName))
                    {
                        input = AnsiConsole.Prompt(new TextPromptWithHistory<string>("[green]>[/]").AddHistory(history));
                    }
                    else
                    {
                        input = AnsiConsole.Prompt(new TextPromptWithHistory<string>($"[green]({Markup.Escape(SelectedEntityName)})>[/]").AddHistory(history));
                    }
                }
                else
                {
                    // Handle vscode Debug Console, which is not 'interactive'
                    if (string.IsNullOrWhiteSpace(SelectedEntityName))
                    {
                        Console.Write($"> ");
                    }
                    else
                    {
                        Console.Write($"({SelectedEntityName})> ");
                    }

                    input = Console.ReadLine();
                }
            }
            while (input == null);

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
                            {
                                thingsToDisplay.Add(thing);
                            }
                        }

                        handled = true;
                        break;
                    }
                }

                if (handled)
                {
                    // Display, then 'continue' to break out of await-for.
                    await RenderView(schema!, thingsToDisplay, cts.Token);
                    history.Add(input);
                    continue;
                }
            }

            // Proceed as normal in interactive mode
            var result = await app.RunAsync(inputArgs);
            if (input != null)
            {
                history.Add(input);
            }
        }
        while (!cts.Token.IsCancellationRequested);

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }

    private static async Task RenderView(
        Schema commonSchema,
        IEnumerable<Thing> thingsToDisplay,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(commonSchema);
        ArgumentNullException.ThrowIfNull(thingsToDisplay);

        // Is there a view for this schema?
        var ssp = AmbientStorageContext.StorageProvider?.GetSchemaStorageProvider();
        var tsp = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (ssp != null && tsp != null)
        {
            var viewSchemaRef = await ssp.FindByNameAsync("view", cancellationToken);
            if (!viewSchemaRef.Equals(Reference.EMPTY))
            {
                await foreach (var viewRef in tsp.GetBySchemaAsync(viewSchemaRef.Guid, cancellationToken))
                {
                    var viewInstance = await tsp.LoadAsync(viewRef.Guid, cancellationToken);
                    if (viewInstance == null)
                    {
                        AmbientErrorContext.Provider.LogError($"Unable to load view {viewRef.Guid}.");
                        return;
                    }

                    var viewProps = new List<ThingProperty>();
                    await foreach (var prop in viewInstance.GetProperties(cancellationToken))
                    {
                        viewProps.Add(prop);
                    }

                    var forSchemaGuidObject = viewProps
                        .Where(p => string.Equals(p.TruePropertyName, $"{viewSchemaRef.Guid}.for", StringComparison.Ordinal))
                        .FirstOrDefault();

                    if (forSchemaGuidObject != default
                        && forSchemaGuidObject.Value is string forSchemaGuid
                        && !string.IsNullOrWhiteSpace(forSchemaGuid)
                        && string.Equals(forSchemaGuid, commonSchema.Guid, StringComparison.Ordinal)
                        && await ssp.GuidExists(forSchemaGuid, cancellationToken))
                    {
                        // Found a matching view!
                        var viewColumnsObject = viewProps
                            .Where(p => string.Equals(p.TruePropertyName, $"{viewSchemaRef.Guid}.displayColumns", StringComparison.Ordinal))
                            .FirstOrDefault();

                        var schema = await ssp.LoadAsync(forSchemaGuid, cancellationToken);

                        if (viewColumnsObject != default
                            && viewColumnsObject.Value is System.Collections.IEnumerable viewColumns
                            && schema != null)
                        {
                            // Do it this way so we preserve viewColumns order
                            List<string> columns = [];
                            var viewableColumns = schema.Properties.Select(p => p.Key)
                                .Union([nameof(Thing.Name), nameof(Thing.Guid)])
                                .ToArray();
                            foreach (var vc in viewColumns.Cast<object?>().Select(v => v?.ToString()))
                            {
                                // If there are variances in capitalization between the property name
                                //    and the name as specified in the viewColumns, the viewColumns
                                //    will win.  This is a 'cheap' way to allow fields to be lowercased
                                //    and the user to specify title-casing or space-separated words
                                //    without defining localized display names on schema.
                                //    However, if there is a schema display name, that will always
                                //    take the higher precedence.
                                if (!string.IsNullOrWhiteSpace(vc)
                                    && viewableColumns.Contains(vc, StringComparer.InvariantCultureIgnoreCase))
                                {
                                    var columnName = vc;

                                    if (schema.Properties.TryGetValue(vc[(vc.IndexOf('.') + 1)..], out SchemaFieldBase? schprop)
                                        && schprop.DisplayNames != null
                                        && schprop.DisplayNames.TryGetValue(CultureInfo.CurrentCulture.Name, out string? prettyDisplayName))
                                    {
                                        columnName = prettyDisplayName;
                                    }

                                    columns.Add(columnName);
                                }
                            }

                            Table t = new();
                            foreach (var col in columns)
                            {
                                t.AddColumn(col);
                            }

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
                                    {
                                        string? text;
                                        if (schema.Properties.TryGetValue(thingProperty.FullDisplayName[(thingProperty.FullDisplayName.IndexOf('.') + 1)..], out SchemaFieldBase? schprop))
                                        {
                                            if (!thingProperty.Valid)
                                            {
                                                text = $"[yellow]{await PrintThingCommand.GetMarkedUpFieldValue(schprop, thingProperty.Value, null, cancellationToken)}[/]";
                                            }
                                            else
                                            {
                                                text = await PrintThingCommand.GetMarkedUpFieldValue(schprop, thingProperty.Value, null, cancellationToken);
                                            }
                                        }
                                        else
                                        {
                                            text = thingProperty.Value?.ToString();
                                        }

                                        // Use ToLowerInvariant so column definition casing in views is forgiving.
                                        cellValues.Add(thingProperty.SimpleDisplayName.ToLowerInvariant(), text);
                                    }
                                }

                                List<string> cells = [];
                                foreach (var col in columns)
                                {
                                    // Use ToLowerInvariant so column definition casing in views is forgiving.
                                    if (!cellValues.TryGetValue(col.ToLowerInvariant(), out object? cellValue))
                                    {
                                        cells.Add("[red]<UNSET>[/]");
                                    }
                                    else
                                    {
                                        cells.Add(cellValue?.ToString() ?? string.Empty);
                                    }
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
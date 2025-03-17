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

using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using jot.Commands;
using jot.Commands.Schemas;
using jot.Commands.Schemas.ImportMaps;
using jot.Commands.Things;
using jot.Errors;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics;
using System.Globalization;

namespace jot;

internal class Program
{
    /// <summary>
    /// For interactive mode, this is the currently selected entity that commands can reference in a REPL.
    /// </summary>
    internal static Reference SelectedEntity = Reference.EMPTY;
    internal static string SelectedEntityName = string.Empty;
    internal static bool Verbose = false;

    private static async Task<int> Main(string[] args)
    {
        var cts = new CancellationTokenSource();

        var interactive = args.Length == 0
            && (AnsiConsole.Profile.Capabilities.Interactive || Debugger.IsAttached);

        // Setup the providers. TODO: Allow CLI config
        AmbientErrorContext.Provider = new SpectreConsoleErrorProvider();
        {
            var ldsp = new Figment.Data.Local.LocalDirectoryStorageProvider("/home/sean/src/figment/jot/db");
            await ldsp.InitializeAsync(cts.Token);
            AmbientStorageContext.StorageProvider = ldsp;
        }

        var app = new CommandApp();
        app.Configure(config =>
        {
            config.AddCommand<NewCommand>("new")
                .WithDescription("Creates new types (schemas) or instances of things");
            config.AddCommand<ListSchemasCommand>("schemas")
                .WithDescription("Lists all the schemas in the database");
            config.AddCommand<ListThingsCommand>("things")
                .WithDescription("Lists all the things in the database");
            config.AddBranch<SchemaCommandSettings>("schema", schema =>
                {
                    schema.AddCommand<AssociateSchemaWithThingCommand>("associate")
                        .WithDescription("Associates a thing with a schema");
                    schema.AddCommand<SetSchemaDescriptionCommand>("describe")
                        .WithDescription("Changes the description of a schema");
                    schema.AddCommand<DeleteSchemaCommand>("delete")
                        .WithDescription("Permanently deletes a schema");
                    schema.AddCommand<DissociateSchemaFromThingCommand>("dissociate")
                        .WithDescription("Dissociates a thing from a schema");
                    schema.AddCommand<ImportSchemaThingsCommand>("import")
                        .WithDescription("Imports things as entities of this schema type");
                    schema.AddBranch("import-map", map =>
                    {
                        map.AddCommand<NewImportMapCommand>("new")
                            .WithDescription("Creates a new import map to link file fields to schema properties");
                        map.AddCommand<ListImportMapsCommand>("list")
                            .WithDescription("Lists all import maps defined on the schema");
                        map.AddCommand<DeleteImportMapCommand>("delete")
                            .WithDescription("Deletes an import map from the schema configuration");
                    });
                    schema.AddCommand<ListSchemaMembersCommand>("members")
                        .WithDescription("Lists all the things associated with a schema");
                    schema.AddCommand<SetSchemaPluralCommand>("plural")
                        .WithDescription("Sets the plural name for the schema");
                    schema.AddCommand<SchemaRenameCommand>("rename")
                        .WithDescription("Changes the name of a schema");
                    schema.AddBranch<SchemaPropertyCommandSettings>("set", set =>
                    {
                        // Note, adding one here requires a manual edit to SetSelectedPropertyCommand
                        set.AddCommand<SetSchemaPropertyDisplayCommand>("display")
                            .WithDescription("Sets a 'pretty' display name for the column");
                        set.AddCommand<SetSchemaPropertyTypeCommand>("type")
                            .WithDescription("Sets the data type of a property");
                        set.AddCommand<SetSchemaPropertyRequiredCommand>("require")
                            .WithDescription("Changes whether a property is required");
                        set.AddCommand<SetSchemaPropertyFormulaCommand>("formula")
                            .WithDescription("Sets the formula expression of a calculated property");
                    });
                    schema.AddCommand<ValidateSchemaCommand>("validate")
                        .WithDescription("Validates the schema is consistent");
                    schema.AddCommand<PrintSchemaCommand>("view")
                        .WithAlias("print")
                        .WithDescription("Views all fields on a schema");
                });
            config.AddBranch<ThingCommandSettings>("thing", thing =>
                {
                    thing.AddCommand<DeleteThingCommand>("delete")
                        .WithDescription("Permanently deletes a thing");
                    thing.AddCommand<PromoteThingPropertyCommand>("promote")
                        .WithDescription("Promotes a property on one thing to become a property defined on a schema");
                    thing.AddCommand<ThingRenameCommand>("rename")
                        .WithDescription("Changes the name of a thing");
                    thing.AddCommand<SetThingPropertyCommand>("set")
                        .WithDescription("Sets the value of a property on a thing");
                    thing.AddCommand<ValidateThingCommand>("validate")
                        .WithDescription("Validates a thing is consistent with its schema");
                    thing.AddCommand<PrintThingCommand>("view")
                        .WithAlias("print")
                        .WithDescription("Views the values of all properties on a thing");
                });
            config.AddBranch("reindex", reindex =>
                {
                    reindex.AddCommand<ReindexSchemasCommand>("schemas")
                        .WithDescription("Rebuilds the index files for schemas for consistency");
                    reindex.AddCommand<ReindexThingsCommand>("things")
                        .WithDescription("Rebuilds the index files for things for consistency");
                });

            // Interactive commands
            if (interactive)
            {
                config.AddCommand<HelpCommand>("ihelp")
                    .WithDescription("Shows additional commands only for this interactive mode");

                config.AddCommand<AssociateSchemaWithSelectedThingCommand>("associate")
                    .WithDescription("Interactive mode command.  Associates the currently selected thing with the specified schema")
                    .IsHidden();

                config.AddCommand<DescribeSelectedSchemaCommand>("describe")
                    .WithDescription("Interactive mode command.  Sets the description for the currently selected schema")
                    .IsHidden();

                config.AddCommand<DeleteSelectedCommand>("delete")
                    .WithAlias("del")
                    .WithDescription("Interactive mode command.  Deletes the currently selected entity")
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
                    .WithAlias("?")
                    .WithAlias("??")
                    .IsHidden();

                config.AddCommand<PromoteSelectedPropertyCommand>("promote")
                    .IsHidden();

                config.AddCommand<QuitCommand>("quit")
                    .WithAlias("exit")
                    .IsHidden();

                config.AddCommand<RenameSelectedCommand>("rename")
                    .IsHidden();

                config.AddCommand<SelectCommand>("select")
                    .WithAlias("sel")
                    .WithAlias("s")
                    .WithDescription("Selects an entity by name or ID");

                config.AddCommand<SetSelectedPropertyCommand>("set")
                    .IsHidden(); // TODO this is probably busted with the 'set' refactor

                config.AddCommand<ValidateSelectedCommand>("validate")
                    .WithAlias("val")
                    .IsHidden();

                config.AddCommand<VerboseCommand>("verbose")
                    .WithDescription("Toggles verbosity.  When verbosity is on, '-v' is specified automatically with every supported command");
            }
        });

        if (!interactive)
        {
            // Command mode.
            return await app.RunAsync(args);
        }

        // Interactive mode
        AnsiConsole.MarkupLine("[bold fuchsia]jot[/] v0.0.1");
        AnsiConsole.MarkupLine("\r\njot is running in [bold underline white]interactive mode[/].  Press ctrl-C to exit or type '[purple bold]quit[/]'.");
        AnsiConsole.MarkupLine("\r\nThere are additional undocumented commands in this mode.  Type [purple bold]ihelp[/] for help on this mode interactive.");

        var schemaProvider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        if (schemaProvider == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load schema storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var thingProvider = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        if (thingProvider == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load thing storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        List<string> history = [];

        do
        {
            string? input;
            if (AnsiConsole.Profile.Capabilities.Interactive)
            {
                if (string.IsNullOrWhiteSpace(SelectedEntityName))
                    input = AnsiConsole.Prompt(new TextPrompt<string>("[green]>[/]")/* { History = history }*/);
                else
                    input = AnsiConsole.Prompt(new TextPrompt<string>($"[green]({SelectedEntityName})>[/]")/* { History = history }*/);
            }
            else
            {
                // Handle vscode Debug Console, which is not 'interactive'
                if (string.IsNullOrWhiteSpace(SelectedEntityName))
                    Console.Write($"> ");
                else
                    Console.Write($"({SelectedEntityName})> ");
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
                    history.Add(input);
                    continue;
                }
            }

            // Proceed as normal in interactive mode
            var result = await app.RunAsync(inputArgs);
            if (input != null)
                history.Add(input);

        } while (!cts.Token.IsCancellationRequested);


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
                    if (viewInstance == null)
                    {
                        AmbientErrorContext.Provider.LogError($"Unable to load view {viewRef.Guid}.");
                        return;
                    }

                    var viewProps = viewInstance.GetProperties(cancellationToken).ToBlockingEnumerable(cancellationToken).ToArray();

                    var forSchemaGuidObject = viewProps
                        .Where(p => string.CompareOrdinal(p.TruePropertyName, $"{viewSchemaRef.Guid}.for") == 0)
                        .Select(p => p.Value)
                        .FirstOrDefault();

                    if (forSchemaGuidObject != default
                        && forSchemaGuidObject is string forSchemaGuid
                        && !string.IsNullOrWhiteSpace(forSchemaGuid)
                        && string.CompareOrdinal(forSchemaGuid, commonSchema.Guid) == 0
                        && await ssp.GuidExists(forSchemaGuid, cancellationToken))
                    {
                        // Found a matching view!

                        var viewColumnsObject = viewProps
                            .Where(p => string.CompareOrdinal(p.TruePropertyName, $"{viewSchemaRef.Guid}.displayColumns") == 0)
                            .Select(p => p.Value)
                            .FirstOrDefault();

                        var schema = await ssp.LoadAsync(forSchemaGuid, cancellationToken);

                        //Console.Error.WriteLine($"DEBUG: Using view '{viewInstance.Name}'");

                        if (viewColumnsObject != default
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
                                        columnName = prettyDisplayName;

                                    columns.Add(columnName);
                                }
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
                                    {
                                        string? text;
                                        if (schema.Properties.TryGetValue(thingProperty.FullDisplayName[(thingProperty.FullDisplayName.IndexOf('.') + 1)..], out SchemaFieldBase? schprop))
                                            text = await PrintThingCommand.GetMarkedUpFieldValue(schprop, thingProperty.Value, cancellationToken);
                                        else
                                            text = thingProperty.Value?.ToString();

                                        // Use ToLowerInvariant so column definition casing in views is forgiving.
                                        cellValues.Add(thingProperty.SimpleDisplayName.ToLowerInvariant(), text);
                                    }
                                }

                                List<string> cells = [];
                                foreach (var col in columns)
                                {
                                    // Use ToLowerInvariant so column definition casing in views is forgiving.
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
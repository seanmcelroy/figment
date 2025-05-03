using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

/// <summary>
/// Renders all fields on a schema to output.
/// </summary>
public class PrintSchemaCommand : SchemaCancellableAsyncCommand<SchemaCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, SchemaCommandSettings settings, CancellationToken cancellationToken)
    {
        var (tgs, schema, _) = await TryGetSchema(settings, cancellationToken);
        if (tgs != Globals.GLOBAL_ERROR_CODES.SUCCESS)
        {
            return (int)tgs;
        }

        var masterTable = new Table()
            .AddColumn(
                new TableColumn(new Text("Name", new Style(decoration: Decoration.Conceal))).Padding(0, 0, 2, 2))
            .AddColumn(
                new TableColumn(new Text("Value", new Style(decoration: Decoration.Conceal))).Padding(0, 0, 2, 2))
            .HideHeaders()
            .NoBorder();

        masterTable.AddRow("[indianred1]Schema[/]", $"[bold orange1]{Markup.Escape(schema.Name)}[/]");
        masterTable.AddRow("[indianred1]Description[/]", string.IsNullOrWhiteSpace(schema.Description) ? "[red]<UNSET>[/]" : schema.Description);
        masterTable.AddRow("[indianred1]Plural[/]", string.IsNullOrWhiteSpace(schema.Plural) ? "[red]<UNSET>[/]" : schema.Plural);

        if (settings.Verbose ?? Program.Verbose)
        {
            masterTable.AddRow("[indianred1]GUID[/]", $"[gray]{Markup.Escape(schema.Guid)}[/]");
            masterTable.AddRow("[indianred1]Created On[/]", $"[gray]{schema.CreatedOn.ToLocalTime().ToLongDateString()} at {schema.CreatedOn.ToLocalTime().ToLongTimeString()}[/]");
            masterTable.AddRow("[indianred1]Modified On[/]", $"[gray]{schema.LastModified.ToLocalTime().ToLongDateString()} at {schema.LastModified.ToLocalTime().ToLongTimeString()}[/]");
        }

        var propertyTable = new Table()
            .AddColumn(
                new TableColumn(new Text("Property Name", new Style(decoration: Decoration.Bold | Decoration.Underline))))
            .AddColumn(
                new TableColumn(new Text("Data Type", new Style(decoration: Decoration.Bold | Decoration.Underline))))
            .AddColumn(
                new TableColumn(new Text("Display Name", new Style(decoration: Decoration.Bold | Decoration.Underline))))
            .AddColumn(
                new TableColumn(new Text("Required?", new Style(decoration: Decoration.Bold | Decoration.Underline))))
            .ShowRowSeparators()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Orange1);

        if (schema!.Properties != null && schema.Properties.Count > 0)
        {
            foreach (var prop in schema.Properties)
            {
                propertyTable.AddRow(
                    $"[dodgerblue1]{Markup.Escape(prop.Key)}[/]",
                    Markup.Escape(await prop.Value.GetReadableFieldTypeAsync(cancellationToken)),
                    prop.Value.DisplayNames?.Select(x => x.Value).FirstOrDefault() ?? Emoji.Known.CrossMark,
                    prop.Value.Required ? Emoji.Known.CheckMarkButton : Emoji.Known.CrossMark);
            }
        }

        if (!string.IsNullOrWhiteSpace(schema.VersionGuid))
        {
            var provider = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
            if (provider == null)
            {
                AmbientErrorContext.Provider.LogError($"Unable to load thing storage provider.");
                return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
            }

            var version = await provider.LoadAsync(schema.VersionGuid, cancellationToken);
            if (version == null)
            {
                AmbientErrorContext.Provider.LogError($"Unable to load version '{schema.VersionGuid}'.");
                return (int)Globals.GLOBAL_ERROR_CODES.THING_LOAD_ERROR;
            }

            AnsiConsole.MarkupLine($"Version     : {version.Name}");
        }

        masterTable.AddRow(new Markup("[indianred1]Properties[/]"), propertyTable);

        AnsiConsole.WriteLine();
        AnsiConsole.Write(masterTable);
        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}
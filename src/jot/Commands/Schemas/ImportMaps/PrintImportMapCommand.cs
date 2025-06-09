using Figment.Common.Errors;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas.ImportMaps;

/// <summary>
/// Prints the details of an import map on a <see cref="Figment.Common.Schema"/> configuration.
/// </summary>
public class PrintImportMapCommand : SchemaCancellableAsyncCommand<PrintImportMapCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, PrintImportMapCommandSettings settings, CancellationToken cancellationToken)
    {
        var verbose = settings.Verbose ?? Program.Verbose;

        if (string.IsNullOrWhiteSpace(settings.ImportMapName))
        {
            AmbientErrorContext.Provider.LogError("Import map name must be specified.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var (tgs, schema, _) = await TryGetSchema(settings, cancellationToken);
        if (tgs != Globals.GLOBAL_ERROR_CODES.SUCCESS)
        {
            return (int)tgs;
        }

        var importMap = schema!.ImportMaps.FirstOrDefault(i => string.Equals(i.Name, settings.ImportMapName, StringComparison.InvariantCultureIgnoreCase));

        if (importMap == null)
        {
            AmbientErrorContext.Provider.LogError($"Schema '{schema.Name}' does not have an import map named '{settings.ImportMapName}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
        }

        var masterTable = new Table()
            .AddColumn(
                new TableColumn(new Text("Name", new Style(decoration: Decoration.Conceal))).Padding(0, 0, 2, 2))
            .AddColumn(
                new TableColumn(new Text("Value", new Style(decoration: Decoration.Conceal))).Padding(0, 0, 2, 2))
            .HideHeaders()
            .NoBorder();

        masterTable.AddRow("[indianred1]Import Map[/]", $"[bold orange1]{Markup.Escape(importMap.Name)}[/]");
        if (verbose)
        {
            masterTable.AddRow("[indianred1]Schema[/]", $"[aqua]{Markup.Escape(schema.Name)}[/] [gray]({Markup.Escape(schema.Guid)})[/]");
        }
        else
        {
            masterTable.AddRow("[indianred1]Schema[/]", $"[aqua]{Markup.Escape(schema.Name)}[/]");
        }

        masterTable.AddRow("[indianred1]Format[/]", string.IsNullOrWhiteSpace(importMap.Format) ? "[red]<UNSET>[/]" : importMap.Format);

        var fieldConfigurationTable = new Table()
            .AddColumn(
                new TableColumn(new Text("File Field", new Style(decoration: Decoration.Bold | Decoration.Underline))))
            .AddColumn(
                new TableColumn(new Text("Property Name", new Style(decoration: Decoration.Bold | Decoration.Underline))))
            .AddColumn(
                new TableColumn(new Text("Skip Missing", new Style(decoration: Decoration.Bold | Decoration.Underline))))
            .AddColumn(
                new TableColumn(new Text("Skip Invalid", new Style(decoration: Decoration.Bold | Decoration.Underline))))
            .ShowRowSeparators()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Orange1);

        if (importMap.FieldConfiguration != null && importMap.FieldConfiguration.Count > 0)
        {
            importMap.EnsureMetadataFields();

            foreach (var fc in importMap.FieldConfiguration)
            {
                // Is this field configuration actively mapped, that is, is the SchemaPropertyName not null?
                var isFileSet = !string.IsNullOrWhiteSpace(fc.ImportFieldName);
                var isPropSet = !string.IsNullOrWhiteSpace(fc.SchemaPropertyName);
                fieldConfigurationTable.AddRow(
                    !isFileSet ? "[red]<UNSET>[/]" : $"[dodgerblue1]{Markup.Escape(fc.ImportFieldName!)}[/]",
                    !isPropSet ? "[red]<UNSET>[/]" : fc.SchemaPropertyName!,
                    fc.SkipRecordIfMissing && (isPropSet || fc.SchemaPropertyName?[0] == '$') ? Emoji.Known.CheckMarkButton : Emoji.Known.CrossMark,
                    fc.SkipRecordIfInvalid && (isPropSet || fc.SchemaPropertyName?[0] == '$') ? Emoji.Known.CheckMarkButton : Emoji.Known.CrossMark);
            }
        }

        masterTable.AddRow(new Markup("[indianred1]Field Configurations[/]"), fieldConfigurationTable);

        AnsiConsole.WriteLine();
        AnsiConsole.Write(masterTable);

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}
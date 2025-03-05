using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class SelectCommand : CancellableAsyncCommand<SelectCommandSettings>
{
    private enum ERROR_CODES : int
    {
        SUCCESS = Globals.GLOBAL_ERROR_CODES.SUCCESS,
        ARGUMENT_ERROR = Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR,
        NOT_FOUND = Globals.GLOBAL_ERROR_CODES.NOT_FOUND,
        AMBIGUOUS_MATCH = Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH,
        UNKNOWN_TYPE = Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE,
        SCHEMA_LOAD_ERROR = Globals.GLOBAL_ERROR_CODES.SCHEMA_LOAD_ERROR,
        THING_LOAD_ERROR = Globals.GLOBAL_ERROR_CODES.THING_LOAD_ERROR,
    }

    public override async Task<int> ExecuteAsync(CommandContext context, SelectCommandSettings settings, CancellationToken cancellationToken)
    {
        // select microsoft

        if (string.IsNullOrWhiteSpace(settings.Name))
        {
            if (Program.SelectedEntity != Reference.EMPTY)
            {
                // Select with no arguments just clears the selection
                AmbientErrorContext.Provider.LogDone($"Selection cleared.");
                Program.SelectedEntity = Reference.EMPTY;
                Program.SelectedEntityName = string.Empty;
                return (int)ERROR_CODES.SUCCESS;
            }

            AnsiConsole.MarkupLine("[yellow]ERROR[/]: You must first 'select' one by specifying a [[NAME]] argument.");
            Program.SelectedEntity = Reference.EMPTY; // On any non-success, clear the selected entity for clarity.
            Program.SelectedEntityName = string.Empty;
            return (int)ERROR_CODES.ARGUMENT_ERROR;
        }

        var possibilities =
            Schema.ResolveAsync(settings.Name, cancellationToken)
                .ToBlockingEnumerable(cancellationToken)
                .Concat([.. Thing.ResolveAsync(settings.Name, cancellationToken).ToBlockingEnumerable(cancellationToken)]
                ).Distinct()
                .ToArray();

        switch (possibilities.Length)
        {
            case 0:
                AnsiConsole.MarkupLine("[red]ERROR[/]: Nothing found with that name");
                return (int)ERROR_CODES.NOT_FOUND;
            case 1:
                switch (possibilities[0].Type)
                {
                    case Reference.ReferenceType.Schema:
                        {
                            var provider = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
                            if (provider == null)
                            {
                                AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load schema storage provider.");
                                return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
                            }

                            var schemaLoaded = await provider.LoadAsync(possibilities[0].Guid, cancellationToken);
                            if (schemaLoaded == null)
                            {
                                AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load schema with Guid '{possibilities[0].Guid}'.");
                                Program.SelectedEntity = Reference.EMPTY; // On any non-success, clear the selected entity for clarity.
                                Program.SelectedEntityName = string.Empty;
                                return (int)ERROR_CODES.SCHEMA_LOAD_ERROR;
                            }

                            AmbientErrorContext.Provider.LogDone($"Schema {schemaLoaded.Name} selected.");
                            Program.SelectedEntity = possibilities[0];
                            Program.SelectedEntityName = schemaLoaded.Name;
                            return (int)ERROR_CODES.SUCCESS;
                        }
                    case Reference.ReferenceType.Thing:
                        var thingProvider = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
                        if (thingProvider == null)
                        {
                            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load thing storage provider.");
                            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
                        }

                        var thingLoaded = await thingProvider.LoadAsync(possibilities[0].Guid, cancellationToken);
                        if (thingLoaded == null)
                        {
                            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load thing with Guid '{possibilities[0].Guid}'.");
                            Program.SelectedEntity = Reference.EMPTY; // On any non-success, clear the selected entity for clarity.
                            Program.SelectedEntityName = string.Empty;
                            return (int)ERROR_CODES.THING_LOAD_ERROR;
                        }

                        AmbientErrorContext.Provider.LogDone($"Thing {thingLoaded.Name} selected.");
                        Program.SelectedEntity = possibilities[0];
                        Program.SelectedEntityName = thingLoaded.Name;
                        return (int)ERROR_CODES.SUCCESS;

                    default:
                        AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: This command does not support type '{Enum.GetName(possibilities[0].Type)}'.");
                        Program.SelectedEntity = Reference.EMPTY; // On any non-success, clear the selected entity for clarity.
                        Program.SelectedEntityName = string.Empty;
                        return (int)ERROR_CODES.UNKNOWN_TYPE;
                }
            default:
                var loadAnyEntity = new Func<Reference, CancellationToken, Task<object?>>(
                        async (reference, cancellationToken1) =>
                {
                    switch (reference.Type)
                    {
                        case Reference.ReferenceType.Schema:
                            var ssp = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
                            return ssp == null ? null : await ssp.LoadAsync(reference.Guid, cancellationToken1);
                        case Reference.ReferenceType.Thing:
                            var tsp = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
                            return tsp == null ? null : await tsp.LoadAsync(reference.Guid, cancellationToken1);
                        default:
                            return null;
                    }
                });

                var disambig = possibilities
                    .Select(p => new { Guid = p, Object = loadAnyEntity(p, cancellationToken).Result })
                    .Where(p => p.Object != null)
                    .Select(p => new PossibleEntityMatch(p.Guid, p.Object!))
                    .ToArray();

                if (!AnsiConsole.Profile.Capabilities.Interactive)
                {
                    // Cannot show selection prompt, so just error message.
                    AnsiConsole.MarkupLine("[red]ERROR[/]: Ambiguous match; more than one entity matches this name.");
                    Program.SelectedEntity = Reference.EMPTY; // On any non-success, clear the selected entity for clarity.
                    Program.SelectedEntityName = string.Empty;
                    return (int)ERROR_CODES.AMBIGUOUS_MATCH;
                }

                var which = AnsiConsole.Prompt(
                    new SelectionPrompt<PossibleEntityMatch>()
                        .Title($"There was more than one entity matching '{settings.Name}'.  Which do you want to select?")
                        .PageSize(5)
                        .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
                        .EnableSearch()
                        .AddChoices(disambig));

                Program.SelectedEntity = which.Reference;
                Program.SelectedEntityName = which.Entity.ToString() ?? which.Reference.Guid ?? string.Empty;
                AmbientErrorContext.Provider.LogDone($"{which.Entity} selected.");
                return (int)ERROR_CODES.SUCCESS;
        }
    }
}
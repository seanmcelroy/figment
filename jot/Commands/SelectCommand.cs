using Figment;
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
                AnsiConsole.MarkupLineInterpolated($"[green]DONE[/]: Selection cleared.\r\n");
                Program.SelectedEntity = Reference.EMPTY;
                return (int)ERROR_CODES.SUCCESS;
            }

            AnsiConsole.MarkupLine("[yellow]ERROR[/]: You must first 'select' one by specifying a [[NAME]] argument.");
            Program.SelectedEntity = Reference.EMPTY; // On any non-success, clear the selected entity for clarity.
            return (int)ERROR_CODES.ARGUMENT_ERROR;
        }

        var possibilities = Reference.ResolveAsync(settings.Name, cancellationToken)
            .ToBlockingEnumerable(cancellationToken)
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
                        var schemaLoaded = await Schema.LoadAsync(possibilities[0].Guid, cancellationToken);
                        if (schemaLoaded == null)
                        {
                            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load schema with Guid '{possibilities[0].Guid}'.");
                            Program.SelectedEntity = Reference.EMPTY; // On any non-success, clear the selected entity for clarity.
                            return (int)ERROR_CODES.SCHEMA_LOAD_ERROR;
                        }
                        else
                        {
                            AnsiConsole.MarkupLineInterpolated($"[green]DONE[/]: Schema {schemaLoaded.Name} selected.\r\n");
                            Program.SelectedEntity = possibilities[0];
                            return (int)ERROR_CODES.SUCCESS;
                        }
                    case Reference.ReferenceType.Thing:
                        var thingLoaded = await Thing.LoadAsync(possibilities[0].Guid, cancellationToken);
                        if (thingLoaded == null)
                        {
                            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load thing with Guid '{possibilities[0].Guid}'.");
                            Program.SelectedEntity = Reference.EMPTY; // On any non-success, clear the selected entity for clarity.
                            return (int)ERROR_CODES.THING_LOAD_ERROR;
                        }
                        else
                        {
                            AnsiConsole.MarkupLineInterpolated($"[green]DONE[/]: Thing {thingLoaded.Name} selected.\r\n");
                            Program.SelectedEntity = possibilities[0];
                            return (int)ERROR_CODES.SUCCESS;
                        }
                    default:
                        AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: This command does not support type '{Enum.GetName(possibilities[0].Type)}'.");
                        Program.SelectedEntity = Reference.EMPTY; // On any non-success, clear the selected entity for clarity.
                        return (int)ERROR_CODES.UNKNOWN_TYPE;
                }
            default:
                var disambig = possibilities
                    .Select(p => new { Guid = p, Object = p.LoadAsync(cancellationToken).Result })
                    .Where(p => p.Object != null)
                    .Select(p => new PossibleEntityMatch(p.Guid, p.Object!))
                    .ToArray();

                if (!AnsiConsole.Profile.Capabilities.Interactive)
                {
                    // Cannot show selection prompt, so just error message.
                    AnsiConsole.MarkupLine("[red]ERROR[/]: Ambiguous match; more than one entity matches this name.");
                    Program.SelectedEntity = Reference.EMPTY; // On any non-success, clear the selected entity for clarity.
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
                AnsiConsole.MarkupLineInterpolated($"[green]DONE[/]: {which.Entity} selected.\r\n");
                return (int)ERROR_CODES.SUCCESS;
        }
    }
}
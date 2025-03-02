using Figment.Common;
using Figment.Common.Data;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

public class SetThingPropertyCommand : CancellableAsyncCommand<SetThingPropertyCommandSettings>
{
    private enum ERROR_CODES : int
    {
        SUCCESS = Globals.GLOBAL_ERROR_CODES.SUCCESS,
        ARGUMENT_ERROR = Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR,
        NOT_FOUND = Globals.GLOBAL_ERROR_CODES.NOT_FOUND,
        AMBIGUOUS_MATCH = Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH,
        UNKNOWN_TYPE = Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE,
        THING_LOAD_ERROR = Globals.GLOBAL_ERROR_CODES.THING_LOAD_ERROR,
        THING_SAVE_ERROR = Globals.GLOBAL_ERROR_CODES.THING_SAVE_ERROR,
    }

    public override async Task<int> ExecuteAsync(CommandContext context, SetThingPropertyCommandSettings settings, CancellationToken cancellationToken)
    {
        // set work phone +12125555555
        // auto-selects text
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY))
        {
            if (string.IsNullOrWhiteSpace(settings.Name))
            {
                AnsiConsole.MarkupLine("[yellow]ERROR[/]: To view properties on a thing, you must first 'select' a thing.");
                return (int)ERROR_CODES.ARGUMENT_ERROR;
            }

            var possibilities = Thing.ResolveAsync(settings.Name, cancellationToken)
                .ToBlockingEnumerable(cancellationToken)
                .ToArray();
            switch (possibilities.Length)
            {
                case 0:
                    AnsiConsole.MarkupLine("[red]ERROR[/]: Nothing found with that name");
                    return (int)ERROR_CODES.NOT_FOUND;
                case 1:
                    selected = possibilities[0];
                    break;
                default:
                    AnsiConsole.MarkupLine("[red]ERROR[/]: Ambiguous match; more than one entity matches this name.");
                    return (int)ERROR_CODES.AMBIGUOUS_MATCH;
            }
        }

        var propName = settings.PropertyName;
        if (string.IsNullOrWhiteSpace(propName))
        {
            AnsiConsole.MarkupLine("[yellow]ERROR[/]: To change a property on a thing, specify the property's name.");
            return (int)ERROR_CODES.ARGUMENT_ERROR;
        }

        if (selected.Type != Reference.ReferenceType.Thing)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: This command does not support type '{Enum.GetName(selected.Type)}'.");
            return (int)ERROR_CODES.UNKNOWN_TYPE;
        }

        var thingProvider = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        if (thingProvider == null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load thing storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var thingLoaded = await thingProvider.LoadAsync(selected.Guid, cancellationToken);
        if (thingLoaded == null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to load thing with Guid '{selected.Guid}'.");
            return (int)ERROR_CODES.THING_LOAD_ERROR;
        }

        var propValue = settings.Value;

        static PossibleNameMatch chooserHandler(string title, IEnumerable<PossibleNameMatch> choices)
        {
            var which = AnsiConsole.Prompt(
                new SelectionPrompt<PossibleNameMatch>()
                    .Title(title)
                    .PageSize(5)
                    .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
                    .AddChoices(choices));
            return which;
        }

        var saved = await thingLoaded.Set(propName, propValue, cancellationToken,
            AnsiConsole.Profile.Capabilities.Interactive ? chooserHandler : null);

        if (!saved.Success)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Unable to edit thing with Guid '{selected.Guid}'.");
            return (int)ERROR_CODES.THING_SAVE_ERROR;
        }

        AnsiConsole.MarkupLineInterpolated($"[green]DONE[/]: {thingLoaded.Name} saved.\r\n");
        return (int)ERROR_CODES.SUCCESS;
    }
}
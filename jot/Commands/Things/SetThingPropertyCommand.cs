using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Things;

/// <summary>
/// Sets the value of a property on a <see cref="Thing"/>.
/// </summary>
public class SetThingPropertyCommand : CancellableAsyncCommand<SetThingPropertyCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, SetThingPropertyCommandSettings settings, CancellationToken cancellationToken)
    {
        // set work phone +12125555555
        // auto-selects text
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY))
        {
            if (string.IsNullOrWhiteSpace(settings.ThingName))
            {
                AmbientErrorContext.Provider.LogError("To modify a thing, you must first 'select' one.");
                return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
            }

            var possibilities = Thing.ResolveAsync(settings.ThingName, cancellationToken)
                .ToBlockingEnumerable(cancellationToken)
                .ToArray();
            switch (possibilities.Length)
            {
                case 0:
                    AmbientErrorContext.Provider.LogError("Nothing found with that name");
                    return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
                case 1:
                    selected = possibilities[0];
                    break;
                default:
                    AmbientErrorContext.Provider.LogError("Ambiguous match; more than one entity matches this name.");
                    return (int)Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH;
            }
        }

        var propName = settings.PropertyName;
        if (string.IsNullOrWhiteSpace(propName))
        {
            AmbientErrorContext.Provider.LogError("To change a property on a thing, specify the property's name.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        if (selected.Type != Reference.ReferenceType.Thing)
        {
            AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(selected.Type)}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE;
        }

        var tsp = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        if (tsp == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load thing storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var thing = await tsp.LoadAsync(selected.Guid, cancellationToken);
        if (thing == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load thing with Guid '{selected.Guid}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.THING_LOAD_ERROR;
        }

        var propValue = settings.Value;

        static PossibleNameMatch ChooserHandler(string title, IEnumerable<PossibleNameMatch> choices)
        {
            var which = AnsiConsole.Prompt(
                new SelectionPrompt<PossibleNameMatch>()
                    .Title(title)
                    .PageSize(5)
                    .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
                    .AddChoices(choices));
            return which;
        }

        var saved = await thing.Set(propName, propValue, cancellationToken,
            AnsiConsole.Profile.Capabilities.Interactive ? ChooserHandler : null);

        if (!saved.Success)
        {
            AmbientErrorContext.Provider.LogError($"Unable to edit thing with Guid '{selected.Guid}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.THING_SAVE_ERROR;
        }

        AmbientErrorContext.Provider.LogDone($"{thing.Name} saved.");
        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}
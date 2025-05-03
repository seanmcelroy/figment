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
        Reference thingReference;
        var thingResolution = settings.ResolveThingName(cancellationToken);
        switch (thingResolution.Item1)
        {
            case Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR:
                AmbientErrorContext.Provider.LogError("To modify a thing, you must first 'select' one.");
                return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
            case Globals.GLOBAL_ERROR_CODES.NOT_FOUND:
                AmbientErrorContext.Provider.LogError($"No thing found named '{settings.ThingName}'");
                return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
            case Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH:
                AmbientErrorContext.Provider.LogError("Ambiguous match; more than one thing matches this name.");
                return (int)Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH;
            case Globals.GLOBAL_ERROR_CODES.SUCCESS:
                thingReference = thingResolution.thing;
                break;
            default:
                throw new NotImplementedException($"Unexpected return code {Enum.GetName(thingResolution.Item1)}");
        }

        if (thingReference.Type != Reference.ReferenceType.Thing)
        {
            AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(thingReference.Type)}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE;
        }

        var propName = settings.PropertyName;
        if (string.IsNullOrWhiteSpace(propName))
        {
            AmbientErrorContext.Provider.LogError("To change a property on a thing, specify the property's name.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var tsp = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        if (tsp == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load thing storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var thing = await tsp.LoadAsync(thingReference.Guid, cancellationToken);
        if (thing == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load thing with Guid '{thingReference.Guid}'.");
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
            AmbientErrorContext.Provider.LogError($"Unable to edit thing with Guid '{thingReference.Guid}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.THING_SAVE_ERROR;
        }

        AmbientErrorContext.Provider.LogDone($"{thing.Name} saved.");
        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}
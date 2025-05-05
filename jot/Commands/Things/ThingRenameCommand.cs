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
using Spectre.Console.Cli;

namespace jot.Commands.Things;

/// <summary>
/// Changes the name of a <see cref="Thing"/>.
/// </summary>
public class ThingRenameCommand : CancellableAsyncCommand<ThingRenameCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, ThingRenameCommandSettings settings, CancellationToken cancellationToken)
    {
        Reference thingReference;
        var thingResolution = settings.ResolveThingName(cancellationToken);
        switch (thingResolution.Item1)
        {
            case Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR:
                AmbientErrorContext.Provider.LogError("To rename a thing, you must first 'select' one.");
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

        var tsp = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (tsp == null)
        {
            AmbientErrorContext.Provider.LogError("Unable to load thing storage provider.");
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var thing = await tsp.LoadAsync(thingReference.Guid, cancellationToken);
        if (thing == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load thing with Guid '{thingReference.Guid}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.SCHEMA_LOAD_ERROR;
        }

        if (string.IsNullOrWhiteSpace(settings.NewName))
        {
            AmbientErrorContext.Provider.LogError("Name of a thing cannot be empty.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        if (!Thing.IsThingNameValid(settings.NewName))
        {
            AmbientErrorContext.Provider.LogError($"Name '{settings.NewName}' is not valid for things.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var oldName = thing.Name;
        thing.Name = settings.NewName.Trim();
        var saved = await thing.SaveAsync(cancellationToken);
        if (!saved)
        {
            AmbientErrorContext.Provider.LogError($"Unable to save thing with Guid '{thingReference.Guid}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.SCHEMA_SAVE_ERROR;
        }

        // For 'name', we know we should rebuild indexes.
        await tsp.RebuildIndexes(cancellationToken);
        AmbientErrorContext.Provider.LogDone($"'{oldName}' renamed to '{thing.Name}'.");

        if (Program.SelectedEntity.Guid.Equals(thing.Guid, StringComparison.Ordinal))
        {
            Program.SelectedEntityName = thing.Name;
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}
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
/// Permanently deletes a <see cref="Thing"/>.
/// </summary>
public class DeleteThingCommand : CancellableAsyncCommand<ThingCommandSettings>
{
    private enum ERROR_CODES : int
    {
        THING_DELETE_ERROR = -2004,
    }

    /// <summary>
    /// Attempts to delete the <see cref="Thing"/> by its name or identifier.
    /// </summary>
    /// <param name="guidOrNamePart">The <see cref="Guid"/> or <see cref="Name"/> of things to match and return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An integer indicating whether or not the command executed successfully.</returns>
    /// <remarks>This can be used by <see cref="DeleteThingCommand"/> and <see cref="DeleteCommand"/>.</remarks>
    internal static async Task<int> TryDeleteThing(string guidOrNamePart, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (selected.Equals(Reference.EMPTY))
        {
            if (string.IsNullOrWhiteSpace(guidOrNamePart))
            {
                AmbientErrorContext.Provider.LogError("To delete a thing, you must first 'select' a thing.");
                return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
            }

            var possibilities = Thing.ResolveAsync(guidOrNamePart, cancellationToken)
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
                    AmbientErrorContext.Provider.LogError("Ambiguous match; more than one thing matches this name.");
                    return (int)Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH;
            }
        }

        if (selected.Type != Reference.ReferenceType.Thing)
        {
            AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(selected.Type)}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE;
        }

        var thingProvider = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (thingProvider == null)
        {
            AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_THING_STORAGE_PROVIDER);
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var thing = await thingProvider.LoadAsync(selected.Guid, cancellationToken);
        if (thing == null)
        {
            AmbientErrorContext.Provider.LogError($"Unable to load thing with Guid '{selected.Guid}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.THING_LOAD_ERROR;
        }

        // TODO: Check links
        var deleted = await thing.DeleteAsync(cancellationToken);
        if (deleted)
        {
            AmbientErrorContext.Provider.LogDone($"{thing.Name} ({thing.Guid}) deleted.");
            if (string.Equals(Program.SelectedEntity.Guid, thing.Guid, StringComparison.OrdinalIgnoreCase))
            {
                Program.SelectedEntity = Reference.EMPTY;
                Program.SelectedEntityName = string.Empty;
            }

            return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
        }
        else
        {
            return (int)ERROR_CODES.THING_DELETE_ERROR;
        }
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, ThingCommandSettings settings, CancellationToken cancellationToken)
    {
        return await TryDeleteThing(settings.ThingName, cancellationToken);
    }
}
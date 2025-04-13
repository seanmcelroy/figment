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
using jot.Commands.Schemas;
using jot.Commands.Things;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Interactive;

/// <summary>
/// Deletes an entity by name or ID.
/// </summary>
public class DeleteCommand : CancellableAsyncCommand<DeleteCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, DeleteCommandSettings settings, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;

        // If something is selected already and no 'name' argument is provided, we're deleting the selected one.
        if (!selected.Equals(Reference.EMPTY) && string.IsNullOrWhiteSpace(settings.Name))
        {
            switch (selected.Type)
            {
                case Reference.ReferenceType.Schema:
                    {
                        var cmd = new DeleteSchemaCommand();
                        return await cmd.ExecuteAsync(context, new SchemaCommandSettings { SchemaName = selected.Guid, Verbose = settings.Verbose }, cancellationToken);
                    }

                case Reference.ReferenceType.Thing:
                    {
                        var cmd = new DeleteThingCommand();
                        return await cmd.ExecuteAsync(context, new ThingCommandSettings { ThingName = selected.Guid, Verbose = settings.Verbose }, cancellationToken);
                    }

                default:
                    AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(selected.Type)}'.");
                    return (int)Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE;
            }
        }

        // This means nothing is selected AND no 'name' argument was provided.
        if (string.IsNullOrWhiteSpace(settings.Name))
        {
            AmbientErrorContext.Provider.LogError("You must specify the entity to delete using the [NAME] argument.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var possibilities =
            Schema.ResolveAsync(settings.Name, cancellationToken)
                .ToBlockingEnumerable(cancellationToken)
                .Concat([.. Thing.ResolveAsync(settings.Name, cancellationToken).ToBlockingEnumerable(cancellationToken)])
                .Distinct()
                .ToArray();

        switch (possibilities.Length)
        {
            case 0:
                AmbientErrorContext.Provider.LogError("Nothing found with that name");
                return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
            case 1:
                switch (possibilities[0].Type)
                {
                    case Reference.ReferenceType.Schema:
                        return await DeleteSchemaCommand.TryDeleteSchema(possibilities[0].Guid, cancellationToken);

                    case Reference.ReferenceType.Thing:
                        return await DeleteThingCommand.TryDeleteThing(possibilities[0].Guid, cancellationToken);

                    default:
                        AmbientErrorContext.Provider.LogError($"This command does not support type '{Enum.GetName(possibilities[0].Type)}'.");
                        Program.SelectedEntity = Reference.EMPTY; // On any non-success, clear the selected entity for clarity.
                        Program.SelectedEntityName = string.Empty;
                        return (int)Globals.GLOBAL_ERROR_CODES.UNKNOWN_TYPE;
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
                    AmbientErrorContext.Provider.LogError("Ambiguous match; more than one entity matches this name.");
                    return (int)Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH;
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
                return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
        }
    }
}
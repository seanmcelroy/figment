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

namespace jot.Commands.Schemas;

/// <summary>
/// Command that permanently deletes a <see cref="Schema"/>.
/// </summary>
public class DeleteSchemaCommand : SchemaCancellableAsyncCommand<SchemaCommandSettings>
{
    private enum ERROR_CODES : int
    {
        SCHEMA_DELETE_ERROR = -2002,
    }

    /// <summary>
    /// Attempts to delete the schem by its name or identifier.
    /// </summary>
    /// <param name="guidOrNamePart">The <see cref="Guid"/> or <see cref="Name"/> of schemas to match and return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An integer indicating whether or not the command executed successfully.</returns>
    /// <remarks>This can be used by <see cref="DeleteSchemaCommand"/> and <see cref="DeleteCommand"/>.</remarks>
    internal static async Task<int> TryDeleteSchema(string guidOrNamePart, CancellationToken cancellationToken)
    {
        var (tgs, schema, _) = await TryGetSchema(guidOrNamePart, cancellationToken);
        if (tgs != Globals.GLOBAL_ERROR_CODES.SUCCESS)
        {
            return (int)tgs;
        }

        var tsp = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (tsp == null)
        {
            AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_THING_STORAGE_PROVIDER);
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        Thing? anyRandomThing = null;
        await foreach (var any in tsp.GetBySchemaAsync(schema!.Guid, cancellationToken))
        {
            anyRandomThing = await tsp.LoadAsync(any.Guid, cancellationToken);
            if (anyRandomThing != null)
            {
                break;
            }
        }

        if (anyRandomThing != null)
        {
            AmbientErrorContext.Provider.LogWarning($"Unable to delete a schema because things exist that use it, such as '{anyRandomThing.Name}'.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var deleted = await schema.DeleteAsync(cancellationToken);
        if (deleted)
        {
            AmbientErrorContext.Provider.LogDone($"{schema.Name} ({schema.Name}) deleted.");
            if (string.Equals(Program.SelectedEntity.Guid, schema.Guid, StringComparison.OrdinalIgnoreCase))
            {
                Program.SelectedEntity = Reference.EMPTY;
                Program.SelectedEntityName = string.Empty;
            }

            return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
        }
        else
        {
            return (int)ERROR_CODES.SCHEMA_DELETE_ERROR;
        }
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, SchemaCommandSettings settings, CancellationToken cancellationToken)
    {
        return await TryDeleteSchema(settings.SchemaName, cancellationToken);
    }
}
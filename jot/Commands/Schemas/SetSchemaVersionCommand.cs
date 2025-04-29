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
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Schemas;

/// <summary>
/// Sets the versioning plan for the schema.
/// </summary>
public class SetSchemaVersionCommand : SchemaCancellableAsyncCommand<SetSchemaVersionCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, SetSchemaVersionCommandSettings settings, CancellationToken cancellationToken)
    {
        var (tgs, schema, _) = await TryGetSchema(settings, cancellationToken);
        if (tgs != Globals.GLOBAL_ERROR_CODES.SUCCESS)
        {
            return (int)tgs;
        }

        var oldVersion = schema!.VersionGuid;

        if (string.IsNullOrWhiteSpace(settings.VersionGuidOrName))
        {
            schema!.VersionGuid = null;
        }
        else
        {
            Reference versionPlan;

            var possibleThings = Thing.ResolveAsync(settings.VersionGuidOrName, cancellationToken)
                    .ToBlockingEnumerable(cancellationToken)
                    .ToArray();
            switch (possibleThings.Length)
            {
                case 0:
                    AmbientErrorContext.Provider.LogError("Nothing found with that name.");
                    return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
                case 1:
                    versionPlan = possibleThings[0];
                    break;
                default:
                    AmbientErrorContext.Provider.LogError("Ambiguous match; more than one thing matches this name.");
                    return (int)Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH;
            }

            schema!.VersionGuid = versionPlan.Guid;
        }

        if (string.Equals(oldVersion, schema.VersionGuid, StringComparison.InvariantCultureIgnoreCase))
        {
            AmbientErrorContext.Provider.LogWarning($"Version plan for {schema.Name} is already '{schema.VersionGuid}'. Nothing to do.");
            return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
        }

        var saved = await schema.SaveAsync(cancellationToken);
        if (!saved)
        {
            if (settings.Verbose ?? false)
            {
                AmbientErrorContext.Provider.LogError($"Unable to save schema '{schema.Name}' ({schema.Guid}).");
            }
            else
            {
                AmbientErrorContext.Provider.LogError($"Unable to save schema '{schema.Name}'.");
            }

            return (int)Globals.GLOBAL_ERROR_CODES.SCHEMA_SAVE_ERROR;
        }

        if (schema.VersionGuid == null)
        {
            AmbientErrorContext.Provider.LogDone($"{schema.Name} saved.  Version plan was '{oldVersion}' but is now removed.");
        }
        else
        {
            AmbientErrorContext.Provider.LogDone($"{schema.Name} saved.  Version plan was '{oldVersion}' but is now '{schema.VersionGuid}'.");
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}
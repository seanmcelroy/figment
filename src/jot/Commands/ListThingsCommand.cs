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
using Figment.Common.Calculations.Parsing;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

/// <summary>
/// Lists all the things in the database.
/// </summary>
public class ListThingsCommand : CancellableAsyncCommand<ListThingsCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, ListThingsCommandSettings settings, CancellationToken cancellationToken)
    {
        var thingProvider = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (thingProvider == null)
        {
            AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_THING_STORAGE_PROVIDER);
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        NodeBase? filterExpression = null;
        if (!string.IsNullOrWhiteSpace(settings.Filter))
        {
            if (!ExpressionParser.TryParse(settings.Filter, out filterExpression))
            {
                AmbientErrorContext.Provider.LogError($"Invalid filter expression: {settings.Filter}");
                return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
            }
        }

        if (settings.AsTable ?? false)
        {
            Table t = new();
            t.AddColumn("Name");
            t.AddColumn("GUID");

            await foreach (var (reference, name) in thingProvider.GetAll(cancellationToken))
            {
                if (await ShouldIncludeThing(thingProvider, reference, name, settings, filterExpression, cancellationToken))
                {
                    t.AddRow(name ?? string.Empty, reference.Guid);
                }
            }

            AnsiConsole.Write(t);
        }
        else
        {
            await foreach (var (reference, name) in thingProvider.GetAll(cancellationToken))
            {
                if (await ShouldIncludeThing(thingProvider, reference, name, settings, filterExpression, cancellationToken))
                {
                    Console.WriteLine(name);
                }
            }
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }

    /// <summary>
    /// Client-side filtering of the listing.
    /// </summary>
    /// <param name="thingProvider">The thing provider.</param>
    /// <param name="reference">The unique identifier of the thing.</param>
    /// <param name="name">The name of the thing.</param>
    /// <param name="settings">The settings passed into the command.</param>
    /// <param name="filterExpression">An optional complex filter expression.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value indicating whether or not the thing referenced by <paramref name="reference"/> should be included.</returns>
    private static async Task<bool> ShouldIncludeThing(
        IThingStorageProvider thingProvider,
        Reference reference,
        string? name,
        ListThingsCommandSettings settings,
        NodeBase? filterExpression,
        CancellationToken cancellationToken)
    {
        // If there is no filtering, then yes.
        var partialNameFilterSpecified = !string.IsNullOrWhiteSpace(settings.PartialNameMatch);
        var complexFilterSpecified = !string.IsNullOrWhiteSpace(settings.Filter) && filterExpression != null;
        if (!partialNameFilterSpecified && !complexFilterSpecified)
        {
            return true;
        }

        // Does it pass the simple name filter, if one is specified?
        if (partialNameFilterSpecified
            && !string.IsNullOrWhiteSpace(name)
            && !string.IsNullOrWhiteSpace(settings.PartialNameMatch)
            && name.Contains(settings.PartialNameMatch, StringComparison.CurrentCultureIgnoreCase))
        {
            return true;
        }

        if (!complexFilterSpecified)
        {
            return false;
        }

        try
        {
            var thing = await thingProvider.LoadAsync(reference.Guid, cancellationToken);
            if (thing == null)
            {
                return false;
            }

            var evaluationContext = new EvaluationContext(thing);
            var result = filterExpression!.Evaluate(evaluationContext);

            if (result.IsSuccess && result.Result is bool boolResult)
            {
                return boolResult;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
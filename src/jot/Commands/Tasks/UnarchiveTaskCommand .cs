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

using System.Collections;
using Figment.Common;
using Figment.Common.Data;
using Figment.Common.Errors;
using Spectre.Console.Cli;

namespace jot.Commands.Tasks;

/// <summary>
/// Unarchives a task.  This is the reverse of <see cref="ArchiveTaskCommand"/>.
/// </summary>
public class UnarchiveTaskCommand : CancellableAsyncCommand<UnarchiveTaskCommandSettings>
{
    /// <summary>
    /// This is a comparer that will treat a field in the data store as 'true' if it is null.
    /// This is useful for the 'complete' field on a Task, which is a nullable date.  If the date
    /// in complete is null, it will evaluate to true with this comparer.
    /// </summary>
    private class BooleanComparerTrueIfNull : IComparer
    {
        public int Compare(object? x, object? y)
        {
            if (x == null && y == null)
            {
                return 0;
            }

            bool xx, yy;

            if (x == null)
            {
                xx = true;
            }
            else if (x is bool xb)
            {
                xx = xb;
            }
            else if (SchemaBooleanField.TryParseBoolean(x.ToString(), out bool xtpb))
            {
                xx = xtpb;
            }
            else
            {
                xx = false; // False if not null.
            }

            if (y == null)
            {
                yy = true; // True if null.
            }
            else if (y is bool yb)
            {
                yy = yb;
            }
            else if (SchemaBooleanField.TryParseBoolean(y.ToString(), out bool ytpb))
            {
                yy = ytpb;
            }
            else
            {
                yy = false; // False if not null.
            }

            return xx.CompareTo(yy);
        }
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, UnarchiveTaskCommandSettings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.TaskNumber))
        {
            AmbientErrorContext.Provider.LogError("Task number not provided.");
            return (int)Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR;
        }

        var tsp = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        if (tsp == null)
        {
            AmbientErrorContext.Provider.LogError(AmbientStorageContext.RESOURCE_ERR_UNABLE_TO_LOAD_THING_STORAGE_PROVIDER);
            return (int)Globals.GLOBAL_ERROR_CODES.GENERAL_IO_ERROR;
        }

        var foundCount = 0;
        var isNumber = false;

        if (ulong.TryParse(settings.TaskNumber, out ulong taskNumber))
        {
            // settings.TaskNumber is actually a number.
            isNumber = true;
            await foreach (var thing in tsp.FindBySchemaAndPropertyValue(
                Figment.Common.Tasks.Task.WellKnownSchemaGuid,
                Figment.Common.Tasks.Task.TrueNameId,
                taskNumber,
                UnsignedNumberComparer.Default,
                cancellationToken))
            {
                if (!(await thing.GetPropertyByTrueNameAsync(Figment.Common.Tasks.Task.TrueNameArchived, cancellationToken))?.AsBoolean() ?? false)
                {
                    foundCount++;
                    AmbientErrorContext.Provider.LogDone($"Task #{taskNumber} is already unarchived.");
                    break; // Only one can match.
                }

                var tsr = await thing.Set("archived", false, cancellationToken);
                if (tsr.Success)
                {
                    var (saveSuccess, saveMessage) = await thing.SaveAsync(cancellationToken);
                    if (saveSuccess)
                    {
                        foundCount++;
                        AmbientErrorContext.Provider.LogDone($"Task #{taskNumber} unarchived.");
                        break; // Only one can match.
                    }
                    else
                    {
                        AmbientErrorContext.Provider.LogError($"Unable to save changes to Task #{taskNumber}: {saveMessage}");
                        return (int)Globals.GLOBAL_ERROR_CODES.THING_SAVE_ERROR;
                    }
                }
            }
        }
        else if (settings.TaskNumber.Equals("*", StringComparison.CurrentCultureIgnoreCase))
        {
            // Mark ALL tasks as archived.
            await foreach (var thing in tsp.LoadAllForSchema(
                    Figment.Common.Tasks.Task.WellKnownSchemaGuid,
                    cancellationToken))
            {
                if (!(await thing.GetPropertyByTrueNameAsync(Figment.Common.Tasks.Task.TrueNameArchived, cancellationToken))?.AsBoolean() ?? false)
                {
                    continue;
                }

                var tsr = await thing.Set("archived", false, cancellationToken);
                if (tsr.Success)
                {
                    var id = await thing.GetPropertyByTrueNameAsync(Figment.Common.Tasks.Task.TrueNameId, cancellationToken);
                    var (saveSuccess, saveMessage) = await thing.SaveAsync(cancellationToken);
                    if (saveSuccess)
                    {
                        foundCount++;
                    }
                    else
                    {
                        AmbientErrorContext.Provider.LogError($"Unable to save changes to Task #{id.Value.Value}: {saveMessage}");
                        return (int)Globals.GLOBAL_ERROR_CODES.THING_SAVE_ERROR;
                    }
                }
            }

            AmbientErrorContext.Provider.LogDone($"Unarchived {foundCount} tasks.");
        }
        else if ("uc".Equals(settings.TaskNumber, StringComparison.CurrentCultureIgnoreCase))
        {
            // Archive every completed task.
            // settings.TaskNumber is actually a number.
            await foreach (var thing in tsp.FindBySchemaAndPropertyValue(
                Figment.Common.Tasks.Task.WellKnownSchemaGuid,
                Figment.Common.Tasks.Task.TrueNameComplete,
                null,
                new BooleanComparerTrueIfNull(),
                cancellationToken))
            {
                var tsr = await thing.Set("archived", false, cancellationToken);
                if (tsr.Success)
                {
                    var id = await thing.GetPropertyByTrueNameAsync(Figment.Common.Tasks.Task.TrueNameId, cancellationToken);
                    var (saveSuccess, saveMessage) = await thing.SaveAsync(cancellationToken);
                    if (saveSuccess)
                    {
                        foundCount++;
                    }
                    else
                    {
                        AmbientErrorContext.Provider.LogError($"Unable to save changes to Task #{id.Value.Value}: {saveMessage}");
                        return (int)Globals.GLOBAL_ERROR_CODES.THING_SAVE_ERROR;
                    }
                }
            }

            AmbientErrorContext.Provider.LogDone($"Unarchived {foundCount} incomplete tasks.");
        }

        if (foundCount == 0 && isNumber)
        {
            AmbientErrorContext.Provider.LogError($"Unable to find Task #{settings.TaskNumber}");
            return (int)Globals.GLOBAL_ERROR_CODES.NOT_FOUND;
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}
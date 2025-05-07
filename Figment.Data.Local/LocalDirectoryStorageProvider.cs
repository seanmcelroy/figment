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

using System.Runtime.CompilerServices;
using Figment.Common.Data;
using Figment.Common.Errors;

namespace Figment.Data.Local;

/// <summary>
/// A storage provider implementation that stores objects in files on a local file system.
/// </summary>
/// <param name="LocalDatabasePath">The path to the root of the file system database.</param>
public class LocalDirectoryStorageProvider(string LocalDatabasePath) : IStorageProvider
{
    private string DB_PATH { get; init; } = LocalDatabasePath;

    /// <inheritdoc/>
    public ISchemaStorageProvider? GetSchemaStorageProvider()
    {
        var schemaDir = Path.Combine(DB_PATH, "schemas");
        {
            var ready = EnsureDirectoryReady(schemaDir);
            if (!ready)
                return null;
        }

        var thingDir = Path.Combine(DB_PATH, "things");
        {
            var ready = EnsureDirectoryReady(thingDir);
            if (!ready)
                return null;
        }

        return new LocalDirectorySchemaStorageProvider(schemaDir, thingDir);
    }

    /// <inheritdoc/>
    public IThingStorageProvider? GetThingStorageProvider()
    {
        var thingDir = Path.Combine(DB_PATH, "things");
        var ready = EnsureDirectoryReady(thingDir);
        if (!ready)
            return null;

        return new LocalDirectoryThingStorageProvider(thingDir);
    }

    /// <inheritdoc/>
    public Task<bool> InitializeAsync(CancellationToken cancellationToken)
    {
        var ready = EnsureDirectoryReady(DB_PATH);
        return Task.FromResult(ready);
    }

    private static bool EnsureDirectoryReady(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (Directory.Exists(path))
            return true;

        AmbientErrorContext.Provider.LogWarning($"Local directory does not exist at {path}");
        try
        {
            Directory.CreateDirectory(path);
            return true;
        }
        catch (Exception ex)
        {
            AmbientErrorContext.Provider.LogException(ex, $"Cannot create directory at: {path}");
            return false;
        }
    }

    internal static async IAsyncEnumerable<string> ResolveGuidFromExactNameAsync(string indexFile, string name, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexFile);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        // Load index
        if (!File.Exists(indexFile))
            yield break; // Happens on new install if no items, nothing in index, and so no file

        await foreach (var entry in IndexManager.LookupAsync(
            indexFile
            , e => string.Equals(e.Key, name, StringComparison.CurrentCultureIgnoreCase), cancellationToken
        ))
        {
            var fileName = entry.Value;
            if (Path.IsPathFullyQualified(fileName))
            {
                // Full file path
                var guid = Path.GetFileName(fileName).Split('.')[0];
                yield return guid;
            }
            else
            {
                // Filename only
                var guid = fileName.Split('.')[0];
                yield return guid;
            }
        }
    }
}
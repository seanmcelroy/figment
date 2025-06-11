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
using Figment.Common;
using System.Text.Json;

namespace Figment.Data.Local;

/// <summary>
/// A storage provider implementation that stores objects in files on a local file system.
/// </summary>
public class LocalDirectoryStorageProvider() : IStorageProvider
{
    /// <summary>
    /// The type of this provider, used to specify it in configurations.
    /// </summary>
    public const string PROVIDER_TYPE = "Local";

    /// <summary>
    /// The settings key for the path to the local directory where the database is located.
    /// </summary>
    public const string SETTINGS_KEY_DB_PATH = "DatabasePath";

    /// <summary>
    /// The path to the root of the file system database.
    /// </summary>
    public string? DatabasePath { get; private set; }

    internal static readonly JsonSerializerOptions SerializerOptions = new()
    {
        // Required for $ref properties with type descriminator
        AllowOutOfOrderMetadataProperties = true,
        TypeInfoResolver = JsonSchemaDefinitionSourceGenerationContext.Default,
#if DEBUG
        WriteIndented = true,
#endif
    };

    /// <inheritdoc/>
    public ISchemaStorageProvider? GetSchemaStorageProvider()
    {
        if (string.IsNullOrWhiteSpace(DatabasePath))
        {
            throw new InvalidOperationException("Provider is not initialized.");
        }

        var schemaDir = Path.Combine(DatabasePath, "schemas");
        {
            var ready = EnsureDirectoryReady(schemaDir);
            if (!ready)
                return null;
        }

        var thingDir = Path.Combine(DatabasePath, "things");
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
        if (string.IsNullOrWhiteSpace(DatabasePath))
        {
            throw new InvalidOperationException("Provider is not initialized.");
        }

        var thingDir = Path.Combine(DatabasePath, "things");
        var ready = EnsureDirectoryReady(thingDir);
        if (!ready)
            return null;

        return new LocalDirectoryThingStorageProvider(thingDir);
    }

    /// <inheritdoc/>
    public Task<bool> InitializeAsync(IDictionary<string, string> settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.TryGetValue(SETTINGS_KEY_DB_PATH, out string? tentative))
        {
            throw new ArgumentException($"Settings do not contain required key {SETTINGS_KEY_DB_PATH}", nameof(settings));
        }

        if (string.IsNullOrWhiteSpace(tentative))
        {
            throw new ArgumentException($"Setting {SETTINGS_KEY_DB_PATH} must be specified.", nameof(settings));
        }

        DatabasePath = tentative
            .ExpandRelativePaths("~/", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
            .ExpandRelativePaths("[APPDATA]/", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

        var ready = EnsureDirectoryReady(DatabasePath);
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
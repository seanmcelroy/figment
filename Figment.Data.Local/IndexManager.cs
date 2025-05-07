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

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Figment.Common.Errors;

namespace Figment.Data.Local;

/// <summary>
/// A utility class for managing indexes for a local file system database.
/// </summary>
public static class IndexManager
{
    /// <summary>
    /// An entry in a local file system index.
    /// </summary>
    /// <param name="Key">The key of the index.</param>
    /// <param name="Value">The value at the index key.</param>
    public readonly record struct Entry(string Key, string Value)
    {
        /// <summary>
        /// The index key.
        /// </summary>
        public string Key { get; init; } = Key;

        /// <summary>
        /// The index value.
        /// </summary>
        public string Value { get; init; } = Value;
    }

    /// <summary>
    /// Finds entries in an index using a <paramref name="selector"/> match.
    /// </summary>
    /// <param name="indexFilePath">The file system path to the index to search.</param>
    /// <param name="selector">The selector used to identify <see cref="Entry"/> instances to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous enumeration of <see cref="Entry"/> instances in the index that the <paramref name="selector"/> matched.</returns>
    public static async IAsyncEnumerable<Entry> LookupAsync(string indexFilePath, Func<Entry, bool> selector, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexFilePath);
        ArgumentNullException.ThrowIfNull(selector);

        if (!File.Exists(indexFilePath))
            yield break;

        using var sr = new StreamReader(indexFilePath, Encoding.UTF8);
        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
        };
        using var csvReader = new CsvReader(sr, csvConfig);

        await foreach (var entry in csvReader.GetRecordsAsync<Entry>(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;
            if (selector.Invoke(entry))
                yield return entry;
        }
    }

    /// <summary>
    /// Adds an entry to an index.
    /// </summary>
    /// <param name="indexFilePath">The file system path to the index to edit.</param>
    /// <param name="key">The index <see cref="Entry.Key"/>.</param>
    /// <param name="value">The index <see cref="Entry.Value"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value indicating whether or not the operation was successful.</returns>
    public static async Task<bool> AddAsync(string indexFilePath, string key, string value, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        try
        {
            using var sw = new StreamWriter(indexFilePath, true, Encoding.UTF8);
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                NewLine = "\r\n"
            };
            using var csv = new CsvWriter(sw, csvConfig);
            await csv.WriteRecordsAsync([new Entry(key, value)], cancellationToken);
            await sw.FlushAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            AmbientErrorContext.Provider.LogException(ex, $"Unable to add entry to index at '{indexFilePath}'");
            return false;
        }
    }

    /// <summary>
    /// Adds an entry to an index.
    /// </summary>
    /// <param name="fs">The new file stream open for the index edit.</param>
    /// <param name="entries">The entries to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value indicating whether or not the operation was successful.</returns>
    public static async Task<bool> AddAsync(FileStream fs, IEnumerable<KeyValuePair<string, string>> entries, CancellationToken cancellationToken)
    {
        try
        {
            using var sw = new StreamWriter(fs, Encoding.UTF8);
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                NewLine = "\r\n"
            };
            using var csv = new CsvWriter(sw, csvConfig);
            await csv.WriteRecordsAsync(entries.Select(e => new Entry(e.Key, e.Value)), cancellationToken);
            await sw.FlushAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            AmbientErrorContext.Provider.LogException(ex, $"Unable to add entry to index at '{fs.Name}'");
            return false;
        }
    }

    /// <summary>
    /// Removes an entry from an index by its <see cref="Entry.Key"/>
    /// </summary>
    /// <param name="indexFilePath">The file system path to the index to edit.</param>
    /// <param name="keyToRemove">The index <see cref="Entry.Key"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value indicating whether or not the operation was successful.</returns>
    public static async Task<bool> RemoveByKeyAsync(string indexFilePath, string keyToRemove, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyToRemove);

        return await RemoveAsync(indexFilePath, entry => string.Equals(entry.Key, keyToRemove, StringComparison.Ordinal), cancellationToken);
    }

    /// <summary>
    /// Removes an entry from an index by its <see cref="Entry.Value"/>
    /// </summary>
    /// <param name="indexFilePath">The file system path to the index to edit.</param>
    /// <param name="valueToRemove">The index <see cref="Entry.Value"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value indicating whether or not the operation was successful.</returns>
    public static async Task<bool> RemoveByValueAsync(string indexFilePath, string valueToRemove, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(valueToRemove);

        return await RemoveAsync(indexFilePath, entry => string.Equals(entry.Value, valueToRemove, StringComparison.Ordinal), cancellationToken);
    }

    private static async Task<bool> RemoveAsync(string indexFilePath, Func<Entry, bool> deleteSelector, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexFilePath);
        ArgumentNullException.ThrowIfNull(deleteSelector);

        if (!File.Exists(indexFilePath))
            return true; // No index, nothing to do.

        var backupPath = $"{indexFilePath}.new";

        {
            // Exclusively read this index.
            using var fsRead = new FileStream(indexFilePath, FileMode.Open, FileAccess.Read, FileShare.None);
            using var sr = new StreamReader(fsRead, Encoding.UTF8);
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false
            };
            using var csvRead = new CsvReader(sr, csvConfig);
            using var sw = new StreamWriter(backupPath, false, Encoding.UTF8);
            var csvConfigWriter = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                NewLine = "\r\n"
            };
            using var csvWriter = new CsvWriter(sw, csvConfigWriter);

            await foreach (var entry in csvRead.GetRecordsAsync<Entry>(cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    return false;
                if (deleteSelector.Invoke(entry))
                    continue;
                await csvWriter.WriteRecordsAsync([entry], cancellationToken);
            }

            await sw.FlushAsync(cancellationToken);
        }

        try
        {
            File.Move(backupPath, indexFilePath, true);
        }
        catch (Exception ex)
        {
            AmbientErrorContext.Provider.LogException(ex, $"Unable to remove entry from index at '{indexFilePath}'");
            return false;
        }

        return true;
    }

    internal static async IAsyncEnumerable<string> ResolveGuidFromPartialNameAsync(
        string indexFile,
        string namePart,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Load index
        if (!File.Exists(indexFile))
            yield break; // Happens on new install if no items, nothing in index, and so no file

        await foreach (var entry in LookupAsync(
            indexFile
            , e => e.Key.StartsWith(namePart, StringComparison.CurrentCultureIgnoreCase)
            , cancellationToken
        ))
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            var fileName = entry.Value;
            if (Path.IsPathFullyQualified(fileName))
                yield return Path.GetFileName(fileName).Split('.')[0];
            else
                yield return fileName.Split('.')[0];
        }
    }
}
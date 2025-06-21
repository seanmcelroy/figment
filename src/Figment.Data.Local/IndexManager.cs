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

using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Figment.Common.Errors;
using nietras.SeparatedValues;

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

    private readonly static ConcurrentDictionary<string, SemaphoreSlim> indexSemaphores = new(StringComparer.InvariantCultureIgnoreCase);

    private static SemaphoreSlim GetSemaphore(string indexFilePath)
    {
        SemaphoreSlim? slim = null;
        while (slim == null)
        {
            if (!indexSemaphores.TryGetValue(indexFilePath, out slim))
            {
                var newSlim = new SemaphoreSlim(2);
                if (indexSemaphores.TryAdd(indexFilePath, newSlim))
                {
                    slim = newSlim;
                }
            }
        }

        return slim;
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

        var slim = GetSemaphore(indexFilePath);
        await slim.WaitAsync(cancellationToken); // Once here for the read
        try
        {
            await using var fs = File.OpenRead(indexFilePath);
            using var sr = new StreamReader(fs, Encoding.UTF8);
            using var csvReader = await Sep.Reader(o => o with { HasHeader = false, Unescape = true, Sep = Sep.New(',') }).FromAsync(sr, cancellationToken);
            await foreach (var row in csvReader)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                if (row.ColCount == 2)
                {
                    var entry = new Entry
                    {
                        Key = row[0].ToString(),
                        Value = row[1].ToString()
                    };

                    if (selector.Invoke(entry))
                        yield return entry;
                }
            }
        }
        finally
        {
            slim.Release(1);
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

        var slim = GetSemaphore(indexFilePath);
        await slim.WaitAsync(cancellationToken);
        await slim.WaitAsync(cancellationToken); // Twice since we're going to write.
        try
        {
            await using var fs = new FileStream(indexFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
            using var sw = new StreamWriter(fs, Encoding.UTF8);
            await using var csvWriter = Sep
                .Writer(o => o with { WriteHeader = false, CultureInfo = CultureInfo.InvariantCulture })
                .To(sw);

            {
                var row = csvWriter.NewRow(cancellationToken);
                row[0].Set(key);
                row[1].Set(value);
                await row.DisposeAsync();
                await csvWriter.FlushAsync(cancellationToken);
            }

            await fs.FlushAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            AmbientErrorContext.Provider.LogException(ex, $"Unable to add entry to index at '{indexFilePath}'");
            return false;
        }
        finally
        {
            slim.Release(2);
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
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(entries);

        var slim = GetSemaphore(fs.Name);
        await slim.WaitAsync(cancellationToken);
        await slim.WaitAsync(cancellationToken); // Twice since we're going to write.
        try
        {
            var sw = new StreamWriter(fs, Encoding.UTF8);
            var csvWriter = Sep.Writer(o => o with { WriteHeader = false, CultureInfo = CultureInfo.InvariantCulture, Escape = true, Sep = Sep.New(',') }).To(sw);
            foreach (var entry in entries)
            {
                await using var row = csvWriter.NewRow(cancellationToken);
                row[0].Set(entry.Key);
                row[1].Set(entry.Value);
            }
            await csvWriter.FlushAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            AmbientErrorContext.Provider.LogException(ex, $"Unable to add entry to index at '{fs.Name}'");
            return false;
        }
        finally
        {
            slim.Release(2);
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

        return await RemoveAsync(indexFilePath, row => string.Equals(row[0].ToString(), keyToRemove, StringComparison.Ordinal), cancellationToken);
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

        return await RemoveAsync(indexFilePath, row =>
        {
            if (row.ColCount != 2)
                return false;

            return valueToRemove.Equals(row[1].ToString(), StringComparison.Ordinal);
        }, cancellationToken);
    }

    private static async Task<bool> RemoveAsync(string indexFilePath, Func<SepReader.Row, bool> deleteSelector, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexFilePath);
        ArgumentNullException.ThrowIfNull(deleteSelector);

        if (!File.Exists(indexFilePath))
            return true; // No index, nothing to do.

        var backupPath = $"{indexFilePath}.new";

        var slim = GetSemaphore(indexFilePath);
        await slim.WaitAsync(cancellationToken); // Once here for the read.
        try
        {
            // Exclusively read this index.
            await using var fsRead = new FileStream(indexFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var sr = new StreamReader(fsRead, Encoding.UTF8);
            using var csvReader = await Sep.Reader(o => o with { HasHeader = false, Unescape = true, Sep = Sep.New(',') }).FromAsync(sr, cancellationToken);
            await using var sw = new StreamWriter(backupPath, false, Encoding.UTF8);
            using var csvWriter = Sep.Writer(o => o with { WriteHeader = false, Escape = true, Sep = Sep.New(','), CultureInfo = CultureInfo.InvariantCulture }).To(sw);

            await foreach (var row in csvReader)
            {
                if (cancellationToken.IsCancellationRequested)
                    return false;
                if (deleteSelector.Invoke(row))
                    continue;

                await using var _ = csvWriter.NewRow(row, cancellationToken);
            }

            await csvWriter.FlushAsync(cancellationToken);
        }
        finally
        {
            slim.Release();
        }

        await slim.WaitAsync(cancellationToken);
        await slim.WaitAsync(cancellationToken); // Twice here for the write (move).
        try
        {
            File.Move(backupPath, indexFilePath, true);
        }
        catch (Exception ex)
        {
            AmbientErrorContext.Provider.LogException(ex, $"Unable to remove entry from index at '{indexFilePath}'");
            return false;
        }
        finally
        {
            slim.Release(2);
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
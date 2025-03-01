using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Spectre.Console;

namespace Figment.Data.Local;

public static class IndexManager
{
    public readonly record struct Entry(string Key, string Value)
    {
        public string Key { get; init; } = Key;
        public string Value { get; init; } = Value;
    }

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
            AnsiConsole.WriteException(ex);
            return false;
        }
    }

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
            AnsiConsole.WriteException(ex);
            return false;
        }
    }

    public static async Task<bool> RemoveByKeyAsync(string indexFilePath, string keyToRemove, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyToRemove);

        return await RemoveAsync(indexFilePath, entry => string.CompareOrdinal(entry.Key, keyToRemove) == 0, cancellationToken);
    }

    public static async Task<bool> RemoveByValueAsync(string indexFilePath, string valueToRemove, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(valueToRemove);

        return await RemoveAsync(indexFilePath, entry => string.CompareOrdinal(entry.Value, valueToRemove) == 0, cancellationToken);
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
            AnsiConsole.WriteException(ex);
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

        await foreach (var entry in IndexManager.LookupAsync(
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
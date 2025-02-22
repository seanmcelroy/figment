using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Spectre.Console;

namespace Figment;

public static class IndexManager
{
    public readonly record struct Entry(string Key, string Value)
    {
        public string Key { get; init; } = Key;
        public string Value { get; init; } = Value;
    }

    public static async IAsyncEnumerable<Entry> LookupAsync(string indexFilePath, Func<Entry, bool> selector, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
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
            if (selector.Invoke(entry))
                yield return entry;
        }
    }

    public static async Task<bool> AddAsync(string indexFilePath, string key, string value, CancellationToken cancellationToken)
    {
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
        return await RemoveAsync(indexFilePath, entry => string.CompareOrdinal(entry.Key, keyToRemove) == 0, cancellationToken);
    }

    public static async Task<bool> RemoveByValueAsync(string indexFilePath, string valueToRemove, CancellationToken cancellationToken)
    {
        return await RemoveAsync(indexFilePath, entry => string.CompareOrdinal(entry.Value, valueToRemove) == 0, cancellationToken);
    }

    private static async Task<bool> RemoveAsync(string indexFilePath, Func<Entry, bool> deleteSelector, CancellationToken cancellationToken)
    {
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
}
using Figment.Common.Data;
using Figment.Data.Local;

namespace Figment.Test.Data.Local;

[TestClass]
public static class LocalSetup
{
    internal static string? TemporaryDirectory;

    [AssemblyInitialize]
    public static void AssemblyInit(TestContext context)
    {
        var dir = Directory.CreateTempSubdirectory();
        TemporaryDirectory = dir.FullName;

        AmbientStorageContext.StorageProvider = new LocalDirectoryStorageProvider();
        _ = AmbientStorageContext.StorageProvider.InitializeAsync(
            new Dictionary<string, string>() { { LocalDirectoryStorageProvider.SETTINGS_KEY_DB_PATH, TemporaryDirectory } },
            CancellationToken.None).Result;
    }

    [AssemblyCleanup]
    public static void AssemblyCleanup()
    {
        if (!string.IsNullOrWhiteSpace(TemporaryDirectory) && Directory.Exists(TemporaryDirectory))
        {
            Directory.Delete(TemporaryDirectory, true);
        }
    }
}
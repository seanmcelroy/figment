using Figment.Common.Data;
using Figment.Data.Local;

namespace Figment.Test.Data.Local;

[TestClass]
public sealed class Schema : SchemaBase
{
    [TestInitialize]
    public override void Initialize()
    {
        Assert.IsNotNull(LocalSetup.TemporaryDirectory);

        AmbientStorageContext.StorageProvider = new LocalDirectoryStorageProvider();
        _ = AmbientStorageContext.StorageProvider.InitializeAsync(
            new Dictionary<string, string>() { { LocalDirectoryStorageProvider.SETTINGS_KEY_DB_PATH, LocalSetup.TemporaryDirectory } },
            CancellationToken.None).Result;
    }
}
using Figment.Common.Data;
using Figment.Data.Memory;

namespace Figment.Test.Data.Memory;

[TestClass]
public sealed class Schema : SchemaBase
{
    [TestInitialize]
    public override void Initialize()
    {
        AmbientStorageContext.StorageProvider = new MemoryStorageProvider();
        _ = AmbientStorageContext.StorageProvider.InitializeAsync(new Dictionary<string, string>(), CancellationToken.None).Result;
    }
}
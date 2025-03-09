using Figment.Common.Data;
using Figment.Data.Memory;

namespace Figment.Test.Data.Memory;

[TestClass]
public sealed class Thing
{
    [TestInitialize]
    public void Initialize()
    {
        AmbientStorageContext.StorageProvider = new MemoryStorageProvider();
    }

    [TestCleanup]
    public void Cleanup()
    {
        AmbientStorageContext.StorageProvider = null;
    }

    [TestMethod]
    public async Task ThingCrud()
    {
        var ssp = AmbientStorageContext.StorageProvider.GetSchemaStorageProvider();
        Assert.IsNotNull(ssp);
        var csr = await ssp.CreateAsync($"{nameof(ThingCrud)}Schema", CancellationToken.None);
        Assert.IsTrue(csr.Success);
        Assert.IsNotNull(csr.NewGuid);
        Assert.IsTrue(await ssp.GuidExists(csr.NewGuid, CancellationToken.None));

        var tsp = AmbientStorageContext.StorageProvider.GetThingStorageProvider();
        Assert.IsNotNull(tsp);
        var thing = await tsp.CreateAsync(csr.NewGuid, nameof(ThingCrud), CancellationToken.None);
        Assert.IsNotNull(thing);
        Assert.IsTrue(await tsp.GuidExists(thing.Guid, CancellationToken.None));

        var tsr = await thing.Set("random", "value", CancellationToken.None);
        Assert.IsTrue(tsr.Success);

    }
}
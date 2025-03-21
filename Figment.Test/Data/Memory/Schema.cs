using Figment.Data.Memory;

namespace Figment.Test.Data.Memory;

[TestClass]
public sealed class Schema
{
    [TestMethod]
    [Ignore]
    public async Task SchemaCrud()
    {
        var storageProvider = new MemoryStorageProvider();

        var ssp = storageProvider.GetSchemaStorageProvider();
        Assert.IsNotNull(ssp);

        var allSchemas = ssp.GetAll(CancellationToken.None).ToBlockingEnumerable();
        Assert.IsNotNull(allSchemas);
        Assert.AreEqual(0, allSchemas.Count());

        var csr = await ssp.CreateAsync($"{nameof(SchemaCrud)}Schema", CancellationToken.None);
        Assert.IsTrue(csr.Success);
        Assert.IsNotNull(csr.NewGuid);
        Assert.IsTrue(await ssp.GuidExists(csr.NewGuid, CancellationToken.None));

        allSchemas = ssp.GetAll(CancellationToken.None).ToBlockingEnumerable();
        Assert.IsNotNull(allSchemas);
        Assert.AreEqual(1, allSchemas.Count());

    }
}
using Figment.Common;
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
        _ = AmbientStorageContext.StorageProvider.InitializeAsync(new Dictionary<string, string>(), CancellationToken.None).Result;
    }

    [TestMethod]
    public async Task ThingCrud()
    {
        var ssp = AmbientStorageContext.StorageProvider?.GetSchemaStorageProvider();
        Assert.IsNotNull(ssp);
        var csr = await ssp.CreateAsync($"{nameof(ThingCrud)}Schema", CancellationToken.None);
        Assert.IsTrue(csr.Success);
        Assert.IsNotNull(csr.NewGuid);
        Assert.IsTrue(await ssp.GuidExists(csr.NewGuid, CancellationToken.None));

        var newSchema = await ssp.LoadAsync(csr.NewGuid, CancellationToken.None);
        Assert.IsNotNull(newSchema);

        var tsp = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        Assert.IsNotNull(tsp);

        var allThings = tsp.GetAll(CancellationToken.None).ToBlockingEnumerable();
        Assert.IsNotNull(allThings);
        var beginThingsCount = allThings.Count();

        var thing = await tsp.CreateAsync(newSchema, nameof(ThingCrud), CancellationToken.None);
        Assert.IsNotNull(thing);
        Assert.IsTrue(await tsp.GuidExists(thing.Guid, CancellationToken.None));

        allThings = tsp.GetAll(CancellationToken.None).ToBlockingEnumerable();
        Assert.IsNotNull(allThings);
        Assert.AreEqual(beginThingsCount + 1, allThings.Count());

        // Set
        var tsr = await thing.Set("random", "value", CancellationToken.None);
        Assert.IsTrue(tsr.Success);

        var props = thing.GetProperties(CancellationToken.None).ToBlockingEnumerable().ToArray();
        Assert.IsTrue(props.Any(p => string.Equals("random", p.SimpleDisplayName, StringComparison.Ordinal)));

        // Clear
        await thing.Set("random", null, CancellationToken.None);
        Assert.AreEqual(0, thing.GetPropertyByName("random", CancellationToken.None).ToBlockingEnumerable().Count());
        props = [.. thing.GetProperties(CancellationToken.None).ToBlockingEnumerable()];
        Assert.IsFalse(props.Any(p => string.Equals("random", p.SimpleDisplayName, StringComparison.Ordinal)));

        // Still clear after reload
        thing = await tsp.LoadAsync(thing.Guid, CancellationToken.None);
        Assert.IsNotNull(thing);
        Assert.IsFalse(props.Any(p => string.Equals("random", p.SimpleDisplayName, StringComparison.Ordinal)));

        var thing2 = await tsp.FindByNameAsync(nameof(ThingCrud), CancellationToken.None);
        Assert.AreNotEqual(Reference.EMPTY, thing2);
        Assert.AreEqual(thing.Guid, thing2.Guid);
        Assert.AreEqual(Reference.ReferenceType.Thing, thing2.Type);

        var partialThings = tsp.FindByPartialNameAsync(csr.NewGuid, nameof(ThingCrud), CancellationToken.None).ToBlockingEnumerable();
        Assert.IsNotNull(partialThings);
        Assert.AreEqual(1, partialThings.Count());
        Assert.AreEqual(thing.Name, partialThings.First().Name);
        Assert.AreEqual(thing.Guid, partialThings.First().Reference.Guid);
        Assert.AreEqual(Reference.ReferenceType.Thing, partialThings.First().Reference.Type);

        var deleted = await thing.DeleteAsync(CancellationToken.None);
        Assert.IsTrue(deleted);

        allThings = tsp.GetAll(CancellationToken.None).ToBlockingEnumerable();
        Assert.IsNotNull(allThings);
        Assert.AreEqual(beginThingsCount, allThings.Count());

        thing2 = await tsp.FindByNameAsync(nameof(ThingCrud), CancellationToken.None);
        Assert.AreEqual(Reference.EMPTY, thing2);

        partialThings = tsp.FindByPartialNameAsync(csr.NewGuid, nameof(ThingCrud), CancellationToken.None).ToBlockingEnumerable();
        Assert.IsNotNull(partialThings);
        Assert.AreEqual(0, partialThings.Count());
    }
}
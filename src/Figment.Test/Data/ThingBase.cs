using Figment.Common;
using Figment.Common.Data;

namespace Figment.Test.Data;

public abstract class ThingBase
{
    public abstract void Initialize();

    [TestCleanup]
    public virtual void Cleanup() { }

    [TestMethod]
    public async Task LoadAsync_NullGuid()
    {
        var ssp = AmbientStorageContext.StorageProvider?.GetSchemaStorageProvider();
        Assert.IsNotNull(ssp);

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await ssp.LoadAsync(null, CancellationToken.None));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }

    [TestMethod]
    public async Task LoadAsync_EmptyStringGuid()
    {
        var ssp = AmbientStorageContext.StorageProvider?.GetSchemaStorageProvider();
        Assert.IsNotNull(ssp);

        await Assert.ThrowsExactlyAsync<ArgumentException>(async () => await ssp.LoadAsync(string.Empty, CancellationToken.None));
    }

    [TestMethod]
    public async Task AssociateWithSchemaAsync_NonExistantThing()
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

        // Here is the nonsense GUID.
        var (success, thing) = await tsp.AssociateWithSchemaAsync(Guid.NewGuid().ToString(), newSchema, CancellationToken.None);
        Assert.IsFalse(success);
        Assert.IsNull(thing);
    }

    [TestMethod]
    public async Task ThingCrud()
    {
        Schema? newSchema;
        {
            var ssp = AmbientStorageContext.StorageProvider?.GetSchemaStorageProvider();
            Assert.IsNotNull(ssp);
            var csr = await ssp.CreateAsync($"{nameof(ThingCrud)}Schema", CancellationToken.None);
            Assert.IsTrue(csr.Success);
            Assert.IsNotNull(csr.NewGuid);
            Assert.IsTrue(await ssp.GuidExists(csr.NewGuid, CancellationToken.None));

            newSchema = await ssp.LoadAsync(csr.NewGuid, CancellationToken.None);
            Assert.IsNotNull(newSchema);
            Assert.AreEqual(csr.NewGuid, newSchema.Guid);

            // Add date field.
            var dt = newSchema.AddDateField("date");
            Assert.IsNotNull(dt);
            Assert.AreEqual("date", dt.Name);

            // Add date field.
            var inc = newSchema.AddIncrementField("inc");
            Assert.IsNotNull(inc);
            Assert.AreEqual("inc", inc.Name);
        }

        var tsp = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        Assert.IsNotNull(tsp);

        List<(Reference reference, string? name)> allThings = [];
        await foreach (var t in tsp.GetAll(CancellationToken.None))
        {
            allThings.Add(t);
        }
        var beginThingsCount = allThings.Count;

        var tcr = await tsp.CreateAsync(newSchema, nameof(ThingCrud), [], CancellationToken.None);
        Assert.IsTrue(tcr.Success);
        Assert.IsNotNull(tcr.NewThing);
        Assert.IsTrue(await tsp.GuidExists(tcr.NewThing.Guid, CancellationToken.None));

        allThings = [];
        await foreach (var t in tsp.GetAll(CancellationToken.None))
        {
            allThings.Add(t);
        }
        Assert.AreEqual(beginThingsCount + 1, allThings.Count);

        var thing = tcr.NewThing;

        var getBySchemaCount = 0;
        await foreach (var t in tsp.GetBySchemaAsync(newSchema.Guid, CancellationToken.None))
        {
            getBySchemaCount++;
            Assert.AreEqual(Reference.ReferenceType.Thing, t.Type);
            Assert.AreEqual(tcr.NewThing.Guid, t.Guid);
        }
        Assert.AreEqual(1, getBySchemaCount);

        // Set
        var tsr = await thing.Set("random", "value", CancellationToken.None);
        Assert.IsTrue(tsr.Success);

        List<ThingProperty> props = [];
        await foreach (var prop in thing.GetProperties(CancellationToken.None))
        {
            props.Add(prop);
        }
        Assert.AreEqual(2, props.Count); // Random + Increment field
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

        HashSet<PossibleNameMatch> partialThings = [];
        await foreach (var pt in tsp.FindByPartialNameAsync(newSchema.Guid, nameof(ThingCrud), CancellationToken.None))
        {
            partialThings.Add(pt);
        }
        Assert.AreEqual(1, partialThings.Count);
        Assert.AreEqual(thing.Name, partialThings.First().Name);
        Assert.AreEqual(thing.Guid, partialThings.First().Reference.Guid);
        Assert.AreEqual(Reference.ReferenceType.Thing, partialThings.First().Reference.Type);

        var deleted = await thing.DeleteAsync(CancellationToken.None);
        Assert.IsTrue(deleted);

        allThings = [];
        await foreach (var t in tsp.GetAll(CancellationToken.None))
        {
            allThings.Add(t);
        }
        Assert.AreEqual(beginThingsCount, allThings.Count);

        thing2 = await tsp.FindByNameAsync(nameof(ThingCrud), CancellationToken.None);
        Assert.AreEqual(Reference.EMPTY, thing2);

        partialThings = [];
        await foreach (var pt in tsp.FindByPartialNameAsync(newSchema.Guid, nameof(ThingCrud), CancellationToken.None))
        {
            partialThings.Add(pt);
        }
        Assert.AreEqual(0, partialThings.Count);
    }

    [TestMethod]
    public async Task RebuildIndexes()
    {
        var tsp = AmbientStorageContext.StorageProvider?.GetThingStorageProvider();
        Assert.IsNotNull(tsp);

        var result = await tsp.RebuildIndexes(CancellationToken.None);
        Assert.IsTrue(result);
    }
}
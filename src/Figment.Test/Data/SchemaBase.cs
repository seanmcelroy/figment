using Figment.Common;
using Figment.Common.Data;

namespace Figment.Test.Data;

public abstract class SchemaBase
{
    public abstract void Initialize();

    [TestMethod]
    public async Task SchemaCrud()
    {
        var ssp = AmbientStorageContext.StorageProvider?.GetSchemaStorageProvider();
        Assert.IsNotNull(ssp);

        var allSchemas = new List<PossibleNameMatch>();
        await foreach (var sch in ssp.GetAll(CancellationToken.None))
        {
            allSchemas.Add(sch);
        }
        Assert.IsNotNull(allSchemas);
        var beginSchemasCount = allSchemas.Count();

        var csr = await ssp.CreateAsync($"{nameof(SchemaCrud)}Schema", CancellationToken.None);
        Assert.IsTrue(csr.Success);
        Assert.IsNotNull(csr.NewGuid);
        Assert.IsTrue(await ssp.GuidExists(csr.NewGuid, CancellationToken.None));

        allSchemas = [];
        await foreach (var sch in ssp.GetAll(CancellationToken.None))
        {
            allSchemas.Add(sch);
        }
        Assert.IsNotNull(allSchemas);
        Assert.AreEqual(beginSchemasCount + 1, allSchemas.Count());

        var schema = await ssp.LoadAsync(csr.NewGuid, CancellationToken.None);
        Assert.IsNotNull(schema);

        // Set
        var stf = schema.AddTextField("random");
        Assert.IsNotNull(stf);

        var deleted = await schema.DeleteAsync(CancellationToken.None);
        Assert.IsTrue(deleted);

        allSchemas = [];
        await foreach (var sch in ssp.GetAll(CancellationToken.None))
        {
            allSchemas.Add(sch);
        }
        Assert.IsNotNull(allSchemas);
        Assert.AreEqual(beginSchemasCount, allSchemas.Count());

        var schema2 = await ssp.FindByNameAsync($"{nameof(SchemaCrud)}Schema", CancellationToken.None);
        Assert.AreEqual(Reference.EMPTY, schema2);
    }
}
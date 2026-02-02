/*
Figment
Copyright (C) 2025  Sean McElroy

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using jot.Commands.Schemas;
using jot.Test.TestUtilities;
using Spectre.Console.Cli;

namespace jot.Test.Commands.Schemas;

[TestClass]
public class AssociateSchemaWithThingCommandTests : CommandTestBase
{
    protected override void ConfigureCommands(CommandApp app)
    {
        app.Configure(config =>
        {
            config.AddCommand<AssociateSchemaWithThingCommand>("associate-schema");
        });
    }

    [TestMethod]
    public async Task ExecuteAsync_WithValidParameters_ShouldAssociateSchema()
    {
        // Arrange
        var (schema, things) = await TestStorageSetup.CreateTestDataSetAsync(StorageProvider);
        var productSchema = TestSchemaFactory.CreateProductSchema();
        var schemaStorage = StorageProvider.GetSchemaStorageProvider();
        await schemaStorage.SaveAsync(productSchema, CancellationToken.None);

        var thing = things.First();

        // Act
        var result = await ExecuteCommandAsync("associate-schema", "--schema", "Product", "--thing", thing.Name);

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        
        var thingStorage = StorageProvider.GetThingStorageProvider();
        var updatedThing = await thingStorage.LoadAsync(thing.Guid, CancellationToken.None);
        Assert.IsNotNull(updatedThing);
        
        Assert.IsTrue(updatedThing.SchemaGuids.Contains(productSchema.Guid), 
            "Thing should be associated with Product schema");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithNonExistentSchema_ShouldReturnError()
    {
        // Arrange
        var (schema, things) = await TestStorageSetup.CreateTestDataSetAsync(StorageProvider);
        var thing = things.First();

        // Act
        var result = await ExecuteCommandAsync("associate-schema", "--schema", "NonExistentSchema", "--thing", thing.Name);

        // Assert
        Assert.AreNotEqual(0, result.ExitCode);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithNonExistentThing_ShouldReturnError()
    {
        // Arrange
        var schema = await TestStorageSetup.CreateAndSaveSchemaAsync(StorageProvider, "TestSchema");

        // Act
        var result = await ExecuteCommandAsync("associate-schema", "--schema", "TestSchema", "--thing", "NonExistentThing");

        // Assert
        Assert.AreNotEqual(0, result.ExitCode);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithAlreadyAssociatedSchema_ShouldNotDuplicate()
    {
        // Arrange
        var (schema, things) = await TestStorageSetup.CreateTestDataSetAsync(StorageProvider);
        var thing = things.First();

        // Act - Associate the same schema again
        var result = await ExecuteCommandAsync("associate-schema", "--schema", "Person", "--thing", thing.Name);

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        
        var thingStorage = StorageProvider.GetThingStorageProvider();
        var updatedThing = await thingStorage.LoadAsync(thing.Guid, CancellationToken.None);
        Assert.IsNotNull(updatedThing);
        
        var schemaCount = updatedThing.SchemaGuids.Count(guid => guid == schema.Guid);
        Assert.AreEqual(1, schemaCount, "Schema should not be duplicated");
    }
}
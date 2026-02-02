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

using jot.Commands.Schemas.ImportMaps;
using jot.Test.TestUtilities;
using Spectre.Console.Cli;

namespace jot.Test.Commands.Schemas.ImportMaps;

[TestClass]
public class NewImportMapCommandTests : CommandTestBase
{
    protected override void ConfigureCommands(CommandApp app)
    {
        app.Configure(config =>
        {
            config.AddCommand<NewImportMapCommand>("new-import-map");
        });
    }

    [TestMethod]
    public async Task ExecuteAsync_WithValidParameters_ShouldCreateImportMap()
    {
        // Arrange
        var schema = await TestStorageSetup.CreateAndSaveSchemaAsync(StorageProvider, "TestSchema");

        // Act
        var result = await ExecuteCommandAsync("new-import-map", "--schema", "TestSchema", "--name", "CSV Import", "--format", "csv");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        
        var schemaStorage = StorageProvider.GetSchemaStorageProvider();
        var updatedSchema = await schemaStorage.LoadAsync(schema.Guid, CancellationToken.None);
        Assert.IsNotNull(updatedSchema);
        
        var importMap = updatedSchema.ImportMaps.FirstOrDefault(im => im.Name == "CSV Import");
        Assert.IsNotNull(importMap, "Import map should be created");
        Assert.AreEqual("csv", importMap.Format);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithNonExistentSchema_ShouldReturnError()
    {
        // Act
        var result = await ExecuteCommandAsync("new-import-map", "--schema", "NonExistentSchema", "--name", "Test Import", "--format", "csv");

        // Assert
        Assert.AreNotEqual(0, result.ExitCode);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithDuplicateImportMapName_ShouldReturnError()
    {
        // Arrange
        var schema = TestSchemaFactory.CreateTestSchema("TestSchema");
        schema.ImportMaps.Add(new Figment.Common.SchemaImportMap { Name = "Existing Import", Format = "csv" });
        var schemaStorage = StorageProvider.GetSchemaStorageProvider();
        await schemaStorage.SaveAsync(schema, CancellationToken.None);

        // Act
        var result = await ExecuteCommandAsync("new-import-map", "--schema", "TestSchema", "--name", "Existing Import", "--format", "csv");

        // Assert
        Assert.AreNotEqual(0, result.ExitCode);
    }
}
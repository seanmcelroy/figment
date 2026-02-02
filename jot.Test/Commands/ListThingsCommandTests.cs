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

using jot.Commands;
using jot.Test.TestUtilities;
using Spectre.Console.Cli;

namespace jot.Test.Commands;

[TestClass]
public class ListThingsCommandTests : CommandTestBase
{
    protected override void ConfigureCommands(CommandApp app)
    {
        app.Configure(config =>
        {
            config.AddCommand<ListThingsCommand>("list-things");
        });
    }

    [TestMethod]
    public async Task ExecuteAsync_WithNoThings_ShouldReturnSuccess()
    {
        // Act
        var result = await ExecuteCommandAsync("list-things");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithThings_ShouldListThem()
    {
        // Arrange
        var (schema, things) = await TestStorageSetup.CreateTestDataSetAsync(StorageProvider);

        // Act
        var result = await ExecuteCommandAsync("list-things");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        var output = TestConsole.Output;
        Assert.IsTrue(output.Contains("John Doe") || output.Contains("Jane Smith"), 
            "Output should contain thing names");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithSchemaFilter_ShouldFilterBySchema()
    {
        // Arrange
        var (personSchema, persons) = await TestStorageSetup.CreateTestDataSetAsync(StorageProvider);
        var productSchema = TestSchemaFactory.CreateProductSchema();
        var schemaStorage = StorageProvider.GetSchemaStorageProvider();
        await schemaStorage.SaveAsync(productSchema, CancellationToken.None);

        var product = TestSchemaFactory.CreateTestThing(productSchema, "Test Product");
        var thingStorage = StorageProvider.GetThingStorageProvider();
        await thingStorage.SaveAsync(product, CancellationToken.None);

        // Act
        var result = await ExecuteCommandAsync("list-things", "--schema", "Person");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        var output = TestConsole.Output;
        Assert.IsTrue(output.Contains("John Doe") || output.Contains("Jane Smith"), 
            "Output should contain person names");
        Assert.IsFalse(output.Contains("Test Product"), 
            "Output should not contain product names when filtered by Person schema");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithNameFilter_ShouldFilterByName()
    {
        // Arrange
        var (schema, things) = await TestStorageSetup.CreateTestDataSetAsync(StorageProvider);

        // Act
        var result = await ExecuteCommandAsync("list-things", "--filter", "John*");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        var output = TestConsole.Output;
        Assert.IsTrue(output.Contains("John"), "Output should contain John");
        Assert.IsFalse(output.Contains("Jane"), "Output should not contain Jane when filtered");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithVerboseFlag_ShouldShowDetailedInfo()
    {
        // Arrange
        var (schema, things) = await TestStorageSetup.CreateTestDataSetAsync(StorageProvider);

        // Act
        var result = await ExecuteCommandAsync("list-things", "--verbose");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
    }
}
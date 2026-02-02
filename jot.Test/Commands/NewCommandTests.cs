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
public class NewCommandTests : CommandTestBase
{
    protected override void ConfigureCommands(CommandApp app)
    {
        app.Configure(config =>
        {
            config.AddCommand<NewCommand>("new");
        });
    }

    [TestMethod]
    public async Task ExecuteAsync_WithValidSchemaName_ShouldCreateNewSchema()
    {
        // Act
        var result = await ExecuteCommandAsync("new", "TestSchema");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        
        var schemaStorage = StorageProvider.GetSchemaStorageProvider();
        var schemas = await schemaStorage.ListAsync(CancellationToken.None).ToListAsync();
        
        Assert.IsTrue(schemas.Any(s => s.Name.Equals("TestSchema", StringComparison.InvariantCultureIgnoreCase)),
            "New schema should be created");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithDescription_ShouldSetDescription()
    {
        // Act
        var result = await ExecuteCommandAsync("new", "TestSchema", "--description", "Test description");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        
        var schemaStorage = StorageProvider.GetSchemaStorageProvider();
        var schemas = await schemaStorage.ListAsync(CancellationToken.None).ToListAsync();
        var schema = schemas.First(s => s.Name.Equals("TestSchema", StringComparison.InvariantCultureIgnoreCase));
        
        Assert.AreEqual("Test description", schema.Description);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithDuplicateName_ShouldReturnError()
    {
        // Arrange
        await TestStorageSetup.CreateAndSaveSchemaAsync(StorageProvider, "ExistingSchema");

        // Act
        var result = await ExecuteCommandAsync("new", "ExistingSchema");

        // Assert
        Assert.AreNotEqual(0, result.ExitCode);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithInvalidName_ShouldReturnError()
    {
        // Act
        var result = await ExecuteCommandAsync("new", "");

        // Assert
        Assert.AreNotEqual(0, result.ExitCode);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithVersion_ShouldSetVersion()
    {
        // Act
        var result = await ExecuteCommandAsync("new", "TestSchema", "--version", "2.0.0");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        
        var schemaStorage = StorageProvider.GetSchemaStorageProvider();
        var schemas = await schemaStorage.ListAsync(CancellationToken.None).ToListAsync();
        var schema = schemas.First(s => s.Name.Equals("TestSchema", StringComparison.InvariantCultureIgnoreCase));
        
        Assert.AreEqual("2.0.0", schema.Version);
    }
}
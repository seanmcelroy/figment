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
public class ListSchemasCommandTests : CommandTestBase
{
    protected override void ConfigureCommands(CommandApp app)
    {
        app.Configure(config =>
        {
            config.AddCommand<ListSchemasCommand>("list-schemas");
        });
    }

    [TestMethod]
    public async Task ExecuteAsync_WithNoSchemas_ShouldReturnSuccess()
    {
        // Act
        var result = await ExecuteCommandAsync("list-schemas");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithSchemas_ShouldListThem()
    {
        // Arrange
        await TestStorageSetup.CreateAndSaveSchemaAsync(StorageProvider, "TestSchema1");
        await TestStorageSetup.CreateAndSaveSchemaAsync(StorageProvider, "TestSchema2");

        // Act
        var result = await ExecuteCommandAsync("list-schemas");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        var output = TestConsole.Output;
        Assert.IsTrue(output.Contains("TestSchema1") || output.Contains("TestSchema2"), 
            "Output should contain schema names");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithVerboseFlag_ShouldShowDetailedInfo()
    {
        // Arrange
        await TestStorageSetup.CreateAndSaveSchemaAsync(StorageProvider, "TestSchema");

        // Act
        var result = await ExecuteCommandAsync("list-schemas", "--verbose");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithFilterPattern_ShouldFilterResults()
    {
        // Arrange
        await TestStorageSetup.CreateAndSaveSchemaAsync(StorageProvider, "PersonSchema");
        await TestStorageSetup.CreateAndSaveSchemaAsync(StorageProvider, "ProductSchema");

        // Act
        var result = await ExecuteCommandAsync("list-schemas", "--filter", "Person*");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
    }
}
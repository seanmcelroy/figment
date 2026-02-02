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
public class PrintSchemaCommandTests : CommandTestBase
{
    protected override void ConfigureCommands(CommandApp app)
    {
        app.Configure(config =>
        {
            config.AddCommand<PrintSchemaCommand>("print-schema");
        });
    }

    [TestMethod]
    public async Task ExecuteAsync_WithValidSchemaName_ShouldPrintSchema()
    {
        // Arrange
        var schema = await TestStorageSetup.CreateAndSaveSchemaAsync(StorageProvider, "TestSchema");

        // Act
        var result = await ExecuteCommandAsync("print-schema", "TestSchema");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        var output = TestConsole.Output;
        Assert.IsTrue(output.Contains("TestSchema"), "Output should contain schema name");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithNonExistentSchema_ShouldReturnError()
    {
        // Act
        var result = await ExecuteCommandAsync("print-schema", "NonExistentSchema");

        // Assert
        Assert.AreNotEqual(0, result.ExitCode);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithAmbiguousSchemaName_ShouldReturnError()
    {
        // Arrange
        await TestStorageSetup.CreateAndSaveSchemaAsync(StorageProvider, "TestSchema1");
        await TestStorageSetup.CreateAndSaveSchemaAsync(StorageProvider, "TestSchema2");

        // Act
        var result = await ExecuteCommandAsync("print-schema", "Test*");

        // Assert
        Assert.AreNotEqual(0, result.ExitCode);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithJsonFormat_ShouldOutputJson()
    {
        // Arrange
        var schema = await TestStorageSetup.CreateAndSaveSchemaAsync(StorageProvider, "TestSchema");

        // Act
        var result = await ExecuteCommandAsync("print-schema", "TestSchema", "--format", "json");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        var output = TestConsole.Output;
        Assert.IsTrue(output.Contains("{") && output.Contains("}"), "Output should be JSON format");
    }
}
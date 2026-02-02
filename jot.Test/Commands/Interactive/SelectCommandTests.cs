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

using jot.Commands.Interactive;
using jot.Test.TestUtilities;
using Spectre.Console.Cli;

namespace jot.Test.Commands.Interactive;

[TestClass]
public class SelectCommandTests : CommandTestBase
{
    protected override void ConfigureCommands(CommandApp app)
    {
        app.Configure(config =>
        {
            config.AddCommand<SelectCommand>("select");
        });
    }

    [TestMethod]
    public async Task ExecuteAsync_WithValidSchemaName_ShouldSelectSchema()
    {
        // Arrange
        var schema = await TestStorageSetup.CreateAndSaveSchemaAsync(StorageProvider, "TestSchema");

        // Act
        var result = await ExecuteCommandAsync("select", "--schema", "TestSchema");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        // Note: In real implementation, this would set the selected schema in global state
    }

    [TestMethod]
    public async Task ExecuteAsync_WithValidThingName_ShouldSelectThing()
    {
        // Arrange
        var (schema, things) = await TestStorageSetup.CreateTestDataSetAsync(StorageProvider);
        var thing = things.First();

        // Act
        var result = await ExecuteCommandAsync("select", "--thing", thing.Name);

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        // Note: In real implementation, this would set the selected thing in global state
    }

    [TestMethod]
    public async Task ExecuteAsync_WithNonExistentSchema_ShouldReturnError()
    {
        // Act
        var result = await ExecuteCommandAsync("select", "--schema", "NonExistentSchema");

        // Assert
        Assert.AreNotEqual(0, result.ExitCode);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithNonExistentThing_ShouldReturnError()
    {
        // Act
        var result = await ExecuteCommandAsync("select", "--thing", "NonExistentThing");

        // Assert
        Assert.AreNotEqual(0, result.ExitCode);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithNoParameters_ShouldShowCurrentSelection()
    {
        // Act
        var result = await ExecuteCommandAsync("select");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
    }
}
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

using jot.Commands.Things;
using jot.Test.TestUtilities;
using Spectre.Console.Cli;

namespace jot.Test.Commands.Things;

[TestClass]
public class PrintThingCommandTests : CommandTestBase
{
    protected override void ConfigureCommands(CommandApp app)
    {
        app.Configure(config =>
        {
            config.AddCommand<PrintThingCommand>("print-thing");
        });
    }

    [TestMethod]
    public async Task ExecuteAsync_WithValidThingName_ShouldPrintThing()
    {
        // Arrange
        var (schema, things) = await TestStorageSetup.CreateTestDataSetAsync(StorageProvider);
        var thing = things.First();

        // Act
        var result = await ExecuteCommandAsync("print-thing", thing.Name);

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        var output = TestConsole.Output;
        Assert.IsTrue(output.Contains(thing.Name), "Output should contain thing name");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithNonExistentThing_ShouldReturnError()
    {
        // Act
        var result = await ExecuteCommandAsync("print-thing", "NonExistentThing");

        // Assert
        Assert.AreNotEqual(0, result.ExitCode);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithJsonFormat_ShouldOutputJson()
    {
        // Arrange
        var (schema, things) = await TestStorageSetup.CreateTestDataSetAsync(StorageProvider);
        var thing = things.First();

        // Act
        var result = await ExecuteCommandAsync("print-thing", thing.Name, "--format", "json");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        var output = TestConsole.Output;
        Assert.IsTrue(output.Contains("{") && output.Contains("}"), "Output should be JSON format");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithAmbiguousThingName_ShouldReturnError()
    {
        // Arrange
        var (schema, things) = await TestStorageSetup.CreateTestDataSetAsync(StorageProvider);
        
        // Create another thing with similar name
        var similarThing = TestSchemaFactory.CreateTestThing(schema, "John Smith");
        var thingStorage = StorageProvider.GetThingStorageProvider();
        await thingStorage.SaveAsync(similarThing, CancellationToken.None);

        // Act
        var result = await ExecuteCommandAsync("print-thing", "John*");

        // Assert
        Assert.AreNotEqual(0, result.ExitCode);
    }
}
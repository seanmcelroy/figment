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
public class ValidateSchemaCommandTests : CommandTestBase
{
    protected override void ConfigureCommands(CommandApp app)
    {
        app.Configure(config =>
        {
            config.AddCommand<ValidateSchemaCommand>("validate-schema");
        });
    }

    [TestMethod]
    public async Task ExecuteAsync_WithValidSchema_ShouldReturnSuccess()
    {
        // Arrange
        var schema = TestSchemaFactory.CreatePersonSchema();
        var schemaStorage = StorageProvider.GetSchemaStorageProvider();
        await schemaStorage.SaveAsync(schema, CancellationToken.None);

        // Act
        var result = await ExecuteCommandAsync("validate-schema", schema.Name);

        // Assert
        Assert.AreEqual(0, result.ExitCode);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithNonExistentSchema_ShouldReturnError()
    {
        // Act
        var result = await ExecuteCommandAsync("validate-schema", "NonExistentSchema");

        // Assert
        Assert.AreNotEqual(0, result.ExitCode);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithInvalidSchema_ShouldReturnError()
    {
        // Arrange - Create schema with invalid configuration
        var schema = TestSchemaFactory.CreateTestSchema("InvalidSchema");
        schema.Name = ""; // Invalid: empty name
        var schemaStorage = StorageProvider.GetSchemaStorageProvider();
        await schemaStorage.SaveAsync(schema, CancellationToken.None);

        // Act
        var result = await ExecuteCommandAsync("validate-schema", "InvalidSchema");

        // Assert
        Assert.AreNotEqual(0, result.ExitCode);
    }
}
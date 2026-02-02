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
public class SetSchemaPropertyTypeCommandTests : CommandTestBase
{
    protected override void ConfigureCommands(CommandApp app)
    {
        app.Configure(config =>
        {
            config.AddCommand<SetSchemaPropertyTypeCommand>("set-property-type");
        });
    }

    [TestMethod]
    public async Task ExecuteAsync_WithValidParameters_ShouldSetPropertyType()
    {
        // Arrange
        var schema = TestSchemaFactory.CreatePersonSchema();
        var schemaStorage = StorageProvider.GetSchemaStorageProvider();
        await schemaStorage.SaveAsync(schema, CancellationToken.None);

        // Act
        var result = await ExecuteCommandAsync("set-property-type", "--schema", "Person", "--property", "Age", "--type", "integer");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        
        var updatedSchema = await schemaStorage.LoadAsync(schema.Guid, CancellationToken.None);
        Assert.IsNotNull(updatedSchema);
        
        var ageField = updatedSchema.Fields.FirstOrDefault(f => f.Name == "Age");
        Assert.IsNotNull(ageField, "Age field should be added");
        Assert.IsInstanceOfType(ageField, typeof(SchemaIntegerField), "Age field should be integer type");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithNonExistentSchema_ShouldReturnError()
    {
        // Act
        var result = await ExecuteCommandAsync("set-property-type", "--schema", "NonExistentSchema", "--property", "TestProperty", "--type", "text");

        // Assert
        Assert.AreNotEqual(0, result.ExitCode);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithInvalidType_ShouldReturnError()
    {
        // Arrange
        var schema = await TestStorageSetup.CreateAndSaveSchemaAsync(StorageProvider, "TestSchema");

        // Act
        var result = await ExecuteCommandAsync("set-property-type", "--schema", "TestSchema", "--property", "TestProperty", "--type", "invalidtype");

        // Assert
        Assert.AreNotEqual(0, result.ExitCode);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithExistingProperty_ShouldUpdatePropertyType()
    {
        // Arrange
        var schema = TestSchemaFactory.CreatePersonSchema();
        var schemaStorage = StorageProvider.GetSchemaStorageProvider();
        await schemaStorage.SaveAsync(schema, CancellationToken.None);

        // Act - Change FirstName from text to email (invalid but for testing)
        var result = await ExecuteCommandAsync("set-property-type", "--schema", "Person", "--property", "FirstName", "--type", "email");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        
        var updatedSchema = await schemaStorage.LoadAsync(schema.Guid, CancellationToken.None);
        Assert.IsNotNull(updatedSchema);
        
        var firstNameField = updatedSchema.Fields.FirstOrDefault(f => f.Name == "FirstName");
        Assert.IsNotNull(firstNameField);
        Assert.IsInstanceOfType(firstNameField, typeof(SchemaEmailField), "FirstName field should be changed to email type");
    }
}
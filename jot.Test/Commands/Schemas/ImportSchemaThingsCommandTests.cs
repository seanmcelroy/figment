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

using Figment.Common;
using jot.Commands.Schemas;
using jot.Test.TestUtilities;
using Spectre.Console.Cli;

namespace jot.Test.Commands.Schemas;

[TestClass]
public class ImportSchemaThingsCommandTests : CommandTestBase
{
    protected override void ConfigureCommands(CommandApp app)
    {
        app.Configure(config =>
        {
            config.AddCommand<ImportSchemaThingsCommand>("import");
        });
    }

    [TestMethod]
    public async Task ExecuteAsync_WithValidCsvFile_ShouldImportThings()
    {
        // Arrange
        var schema = TestSchemaFactory.CreatePersonSchema();
        
        // Add import map to schema
        var importMap = new SchemaImportMap
        {
            Name = "CSV Import",
            Format = "csv",
            FieldConfiguration = new List<SchemaImportField>
            {
                new() { ImportFieldName = "Name", SchemaPropertyName = "$Name" },
                new() { ImportFieldName = "FirstName", SchemaPropertyName = "FirstName" },
                new() { ImportFieldName = "LastName", SchemaPropertyName = "LastName" },
                new() { ImportFieldName = "Email", SchemaPropertyName = "Email" }
            }
        };
        schema.ImportMaps.Add(importMap);
        
        var schemaStorage = StorageProvider.GetSchemaStorageProvider();
        await schemaStorage.SaveAsync(schema, CancellationToken.None);

        var csvContent = """
            Name,FirstName,LastName,Email
            John Doe,John,Doe,john@example.com
            Jane Smith,Jane,Smith,jane@example.com
            """;
        var csvFile = CreateTestCsvFile(csvContent);

        // Act
        var result = await ExecuteCommandAsync("import", "--schema", "Person", "--file", csvFile);

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        
        var thingStorage = StorageProvider.GetThingStorageProvider();
        var things = await thingStorage.ListAsync(CancellationToken.None).ToListAsync();
        
        Assert.AreEqual(2, things.Count, "Should import 2 things");
        Assert.IsTrue(things.Any(t => t.Name == "John Doe"), "Should import John Doe");
        Assert.IsTrue(things.Any(t => t.Name == "Jane Smith"), "Should import Jane Smith");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithMissingFile_ShouldReturnError()
    {
        // Arrange
        var schema = await TestStorageSetup.CreateAndSaveSchemaAsync(StorageProvider, "TestSchema");

        // Act
        var result = await ExecuteCommandAsync("import", "--schema", "TestSchema", "--file", "nonexistent.csv");

        // Assert
        Assert.AreNotEqual(0, result.ExitCode);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithNonExistentSchema_ShouldReturnError()
    {
        // Arrange
        var csvFile = CreateTestCsvFile("Name\nTest");

        // Act
        var result = await ExecuteCommandAsync("import", "--schema", "NonExistentSchema", "--file", csvFile);

        // Assert
        Assert.AreNotEqual(0, result.ExitCode);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithDryRun_ShouldNotSaveThings()
    {
        // Arrange
        var schema = TestSchemaFactory.CreatePersonSchema();
        var importMap = new SchemaImportMap
        {
            Name = "CSV Import",
            Format = "csv",
            FieldConfiguration = new List<SchemaImportField>
            {
                new() { ImportFieldName = "Name", SchemaPropertyName = "$Name" }
            }
        };
        schema.ImportMaps.Add(importMap);
        
        var schemaStorage = StorageProvider.GetSchemaStorageProvider();
        await schemaStorage.SaveAsync(schema, CancellationToken.None);

        var csvContent = "Name\nTest Person";
        var csvFile = CreateTestCsvFile(csvContent);

        // Act
        var result = await ExecuteCommandAsync("import", "--schema", "Person", "--file", csvFile, "--dry-run");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        
        var thingStorage = StorageProvider.GetThingStorageProvider();
        var things = await thingStorage.ListAsync(CancellationToken.None).ToListAsync();
        
        Assert.AreEqual(0, things.Count, "Dry run should not save things");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithDuplicateHandling_ShouldHandleDuplicates()
    {
        // Arrange
        var schema = TestSchemaFactory.CreatePersonSchema();
        var importMap = new SchemaImportMap
        {
            Name = "CSV Import",
            Format = "csv",
            FieldConfiguration = new List<SchemaImportField>
            {
                new() { ImportFieldName = "Name", SchemaPropertyName = "$Name" }
            }
        };
        schema.ImportMaps.Add(importMap);
        
        var schemaStorage = StorageProvider.GetSchemaStorageProvider();
        await schemaStorage.SaveAsync(schema, CancellationToken.None);

        var csvContent = """
            Name
            Test Person
            Test Person
            """;
        var csvFile = CreateTestCsvFile(csvContent);

        // Act
        var result = await ExecuteCommandAsync("import", "--schema", "Person", "--file", csvFile, "--dupe-strategy", "skip");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        
        var thingStorage = StorageProvider.GetThingStorageProvider();
        var things = await thingStorage.ListAsync(CancellationToken.None).ToListAsync();
        
        Assert.AreEqual(1, things.Count, "Should only import one unique thing");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithRowLimits_ShouldRespectLimits()
    {
        // Arrange
        var schema = TestSchemaFactory.CreatePersonSchema();
        var importMap = new SchemaImportMap
        {
            Name = "CSV Import",
            Format = "csv",
            FieldConfiguration = new List<SchemaImportField>
            {
                new() { ImportFieldName = "Name", SchemaPropertyName = "$Name" }
            }
        };
        schema.ImportMaps.Add(importMap);
        
        var schemaStorage = StorageProvider.GetSchemaStorageProvider();
        await schemaStorage.SaveAsync(schema, CancellationToken.None);

        var csvContent = """
            Name
            Person 1
            Person 2
            Person 3
            """;
        var csvFile = CreateTestCsvFile(csvContent);

        // Act
        var result = await ExecuteCommandAsync("import", "--schema", "Person", "--file", csvFile, "--records-to-import", "2");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        
        var thingStorage = StorageProvider.GetThingStorageProvider();
        var things = await thingStorage.ListAsync(CancellationToken.None).ToListAsync();
        
        Assert.AreEqual(2, things.Count, "Should only import 2 things due to limit");
    }
}
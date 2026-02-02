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
using jot.Commands;
using jot.Commands.Schemas;
using jot.Commands.Schemas.ImportMaps;
using jot.Test.TestUtilities;
using Spectre.Console.Cli;

namespace jot.Test.Integration;

[TestClass]
public class ImportWorkflowTests : CommandTestBase
{
    protected override void ConfigureCommands(CommandApp app)
    {
        app.Configure(config =>
        {
            config.AddCommand<NewCommand>("new");
            config.AddCommand<SetSchemaPropertyTypeCommand>("set-property-type");
            config.AddCommand<NewImportMapCommand>("new-import-map");
            config.AddCommand<ImportSchemaThingsCommand>("import");
            config.AddCommand<ListThingsCommand>("list-things");
        });
    }

    [TestMethod]
    public async Task CompleteImportWorkflow_ShouldWorkEndToEnd()
    {
        // Step 1: Create a schema
        var result1 = await ExecuteCommandAsync("new", "ProductSchema", "--description", "Product schema for import testing");
        Assert.AreEqual(0, result1.ExitCode, "Schema creation should succeed");

        // Step 2: Add properties
        var result2 = await ExecuteCommandAsync("set-property-type", "--schema", "ProductSchema", "--property", "Name", "--type", "text");
        Assert.AreEqual(0, result2.ExitCode, "Adding Name property should succeed");

        var result3 = await ExecuteCommandAsync("set-property-type", "--schema", "ProductSchema", "--property", "Price", "--type", "number");
        Assert.AreEqual(0, result3.ExitCode, "Adding Price property should succeed");

        var result4 = await ExecuteCommandAsync("set-property-type", "--schema", "ProductSchema", "--property", "Description", "--type", "text");
        Assert.AreEqual(0, result4.ExitCode, "Adding Description property should succeed");

        // Step 3: Create import map (this needs to be done manually since the command might not support all fields)
        var schemaStorage = StorageProvider.GetSchemaStorageProvider();
        var schemas = await schemaStorage.ListAsync(CancellationToken.None).ToListAsync();
        var productSchema = schemas.First(s => s.Name == "ProductSchema");
        
        var importMap = new SchemaImportMap
        {
            Name = "CSV Product Import",
            Format = "csv",
            FieldConfiguration = new List<SchemaImportField>
            {
                new() { ImportFieldName = "ProductName", SchemaPropertyName = "$Name" },
                new() { ImportFieldName = "ProductPrice", SchemaPropertyName = "Price" },
                new() { ImportFieldName = "ProductDescription", SchemaPropertyName = "Description" }
            }
        };
        productSchema.ImportMaps.Add(importMap);
        await schemaStorage.SaveAsync(productSchema, CancellationToken.None);

        // Step 4: Create test CSV file
        var csvContent = """
            ProductName,ProductPrice,ProductDescription
            Widget A,19.99,A useful widget
            Widget B,29.99,An even better widget
            Gadget X,49.99,The ultimate gadget
            """;
        var csvFile = CreateTestCsvFile(csvContent);

        // Step 5: Import the data
        var result5 = await ExecuteCommandAsync("import", "--schema", "ProductSchema", "--file", csvFile);
        Assert.AreEqual(0, result5.ExitCode, "Import should succeed");

        // Step 6: Verify import results
        var result6 = await ExecuteCommandAsync("list-things", "--schema", "ProductSchema");
        Assert.AreEqual(0, result6.ExitCode, "Listing things should succeed");

        // Verify the data was imported
        var thingStorage = StorageProvider.GetThingStorageProvider();
        var things = await thingStorage.ListAsync(CancellationToken.None).ToListAsync();
        var productThings = things.Where(t => t.SchemaGuids.Contains(productSchema.Guid)).ToList();
        
        Assert.AreEqual(3, productThings.Count, "Should have imported 3 products");
        
        var widgetA = productThings.FirstOrDefault(t => t.Name == "Widget A");
        Assert.IsNotNull(widgetA, "Widget A should be imported");
        
        var priceProperty = widgetA.Properties.FirstOrDefault(p => p.Name == "Price");
        Assert.IsNotNull(priceProperty, "Widget A should have Price property");
        Assert.AreEqual("19.99", priceProperty.Value, "Price should be correctly imported");
    }
}
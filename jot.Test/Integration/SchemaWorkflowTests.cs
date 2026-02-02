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
using jot.Commands.Schemas;
using jot.Test.TestUtilities;
using Spectre.Console.Cli;

namespace jot.Test.Integration;

[TestClass]
public class SchemaWorkflowTests : CommandTestBase
{
    protected override void ConfigureCommands(CommandApp app)
    {
        app.Configure(config =>
        {
            config.AddCommand<NewCommand>("new");
            config.AddCommand<ListSchemasCommand>("list-schemas");
            config.AddCommand<SetSchemaPropertyTypeCommand>("set-property-type");
            config.AddCommand<ValidateSchemaCommand>("validate-schema");
            config.AddCommand<PrintSchemaCommand>("print-schema");
        });
    }

    [TestMethod]
    public async Task CompleteSchemaWorkflow_ShouldWorkEndToEnd()
    {
        // Step 1: Create a new schema
        var result1 = await ExecuteCommandAsync("new", "PersonSchema", "--description", "A person schema for testing");
        Assert.AreEqual(0, result1.ExitCode, "Schema creation should succeed");

        // Step 2: Add properties to the schema
        var result2 = await ExecuteCommandAsync("set-property-type", "--schema", "PersonSchema", "--property", "FirstName", "--type", "text");
        Assert.AreEqual(0, result2.ExitCode, "Adding FirstName property should succeed");

        var result3 = await ExecuteCommandAsync("set-property-type", "--schema", "PersonSchema", "--property", "LastName", "--type", "text");
        Assert.AreEqual(0, result3.ExitCode, "Adding LastName property should succeed");

        var result4 = await ExecuteCommandAsync("set-property-type", "--schema", "PersonSchema", "--property", "Email", "--type", "email");
        Assert.AreEqual(0, result4.ExitCode, "Adding Email property should succeed");

        var result5 = await ExecuteCommandAsync("set-property-type", "--schema", "PersonSchema", "--property", "Age", "--type", "integer");
        Assert.AreEqual(0, result5.ExitCode, "Adding Age property should succeed");

        // Step 3: Validate the schema
        var result6 = await ExecuteCommandAsync("validate-schema", "PersonSchema");
        Assert.AreEqual(0, result6.ExitCode, "Schema validation should succeed");

        // Step 4: Print the schema to verify structure
        var result7 = await ExecuteCommandAsync("print-schema", "PersonSchema");
        Assert.AreEqual(0, result7.ExitCode, "Printing schema should succeed");

        // Step 5: List schemas to verify it appears
        var result8 = await ExecuteCommandAsync("list-schemas");
        Assert.AreEqual(0, result8.ExitCode, "Listing schemas should succeed");
        var output = TestConsole.Output;
        Assert.IsTrue(output.Contains("PersonSchema"), "Schema should appear in the list");

        // Verify the schema was created correctly
        var schemaStorage = StorageProvider.GetSchemaStorageProvider();
        var schemas = await schemaStorage.ListAsync(CancellationToken.None).ToListAsync();
        var personSchema = schemas.FirstOrDefault(s => s.Name == "PersonSchema");
        
        Assert.IsNotNull(personSchema, "PersonSchema should exist");
        Assert.AreEqual("A person schema for testing", personSchema.Description);
        Assert.AreEqual(4, personSchema.Fields.Count, "Schema should have 4 fields");
        
        var fieldNames = personSchema.Fields.Select(f => f.Name).ToList();
        Assert.IsTrue(fieldNames.Contains("FirstName"), "Should have FirstName field");
        Assert.IsTrue(fieldNames.Contains("LastName"), "Should have LastName field");
        Assert.IsTrue(fieldNames.Contains("Email"), "Should have Email field");
        Assert.IsTrue(fieldNames.Contains("Age"), "Should have Age field");
    }
}
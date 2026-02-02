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

namespace jot.Test.TestUtilities;

public static class TestSchemaFactory
{
    public static Schema CreateTestSchema(string name = "TestSchema", string? description = null)
    {
        var schema = new Schema
        {
            Guid = Guid.NewGuid().ToString(),
            Name = name,
            Description = description ?? $"Test schema: {name}",
            Version = "1.0.0",
            Plural = $"{name}s"
        };

        return schema;
    }

    public static Schema CreatePersonSchema()
    {
        var schema = CreateTestSchema("Person", "A test person schema");

        schema.Fields.Add(new SchemaTextField
        {
            Name = "FirstName",
            Display = "First Name",
            Required = true
        });

        schema.Fields.Add(new SchemaTextField
        {
            Name = "LastName",
            Display = "Last Name",
            Required = true
        });

        schema.Fields.Add(new SchemaEmailField
        {
            Name = "Email",
            Display = "Email Address",
            Required = false
        });

        schema.Fields.Add(new SchemaDateField
        {
            Name = "BirthDate",
            Display = "Birth Date",
            Required = false
        });

        return schema;
    }

    public static Schema CreateProductSchema()
    {
        var schema = CreateTestSchema("Product", "A test product schema");

        schema.Fields.Add(new SchemaTextField
        {
            Name = "Name",
            Display = "Product Name",
            Required = true
        });

        schema.Fields.Add(new SchemaTextField
        {
            Name = "Description",
            Display = "Description",
            Required = false
        });

        schema.Fields.Add(new SchemaNumberField
        {
            Name = "Price",
            Display = "Price",
            Required = true
        });

        schema.Fields.Add(new SchemaIntegerField
        {
            Name = "Quantity",
            Display = "Quantity in Stock",
            Required = false
        });

        return schema;
    }

    public static Thing CreateTestThing(Schema schema, string name, Dictionary<string, object>? properties = null)
    {
        var thing = new Thing(Guid.NewGuid().ToString(), name);
        thing.SchemaGuids.Add(schema.Guid);

        if (properties != null)
        {
            thing.Set(properties, CancellationToken.None).Wait();
        }

        return thing;
    }
}
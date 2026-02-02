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
using Figment.Data.Memory;

namespace jot.Test.TestUtilities;

public static class TestStorageSetup
{
    public static async Task<Schema> CreateAndSaveSchemaAsync(MemoryStorageProvider storageProvider, string name = "TestSchema")
    {
        var schema = TestSchemaFactory.CreateTestSchema(name);
        var schemaStorage = storageProvider.GetSchemaStorageProvider();
        await schemaStorage.SaveAsync(schema, CancellationToken.None);
        return schema;
    }

    public static async Task<Thing> CreateAndSaveThingAsync(MemoryStorageProvider storageProvider, Schema schema, string name, Dictionary<string, object>? properties = null)
    {
        var thing = TestSchemaFactory.CreateTestThing(schema, name, properties);
        var thingStorage = storageProvider.GetThingStorageProvider();
        await thingStorage.SaveAsync(thing, CancellationToken.None);
        return thing;
    }

    public static async Task<(Schema schema, List<Thing> things)> CreateTestDataSetAsync(MemoryStorageProvider storageProvider)
    {
        var schema = TestSchemaFactory.CreatePersonSchema();
        var schemaStorage = storageProvider.GetSchemaStorageProvider();
        await schemaStorage.SaveAsync(schema, CancellationToken.None);

        var things = new List<Thing>();
        var thingStorage = storageProvider.GetThingStorageProvider();

        // Create test persons
        var person1 = TestSchemaFactory.CreateTestThing(schema, "John Doe", new Dictionary<string, object>
        {
            ["FirstName"] = "John",
            ["LastName"] = "Doe",
            ["Email"] = "john.doe@example.com",
            ["BirthDate"] = "1990-01-15"
        });
        await thingStorage.SaveAsync(person1, CancellationToken.None);
        things.Add(person1);

        var person2 = TestSchemaFactory.CreateTestThing(schema, "Jane Smith", new Dictionary<string, object>
        {
            ["FirstName"] = "Jane",
            ["LastName"] = "Smith",
            ["Email"] = "jane.smith@example.com",
            ["BirthDate"] = "1985-03-22"
        });
        await thingStorage.SaveAsync(person2, CancellationToken.None);
        things.Add(person2);

        return (schema, things);
    }
}
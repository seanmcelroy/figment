# Import Maps

## Concept

Import Maps are a collection of mappings between fields in a file and fields on a schema.  By specifying which fields in a file should be imported to what fields on a schema, this configuration can be reused to import records into Figment.

Each import map field configuration has for key attributes:

1. Name of the field from the imported file format
2. Name of the field on the schema to which data from the imported file will be placed
3. Whether the entire record should be skipped of this imported file field is missing
4. Whether the entire record should be skipped if the data in this field cannot be coerced into the value for the schema into which it is mapped
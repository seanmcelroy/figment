# Import Maps
Any schema can be configured with an import map, which is a field level mapping that defines how to bring data in from a file into Figment.  Import maps have a name and define field mappings for a given file type to specific properties on a schema.

For example, to create a new import map that allows Google Contacts CSV files to be imported into things on the `person` schema, one would type a `jot` command like the following:

`schema person import-map google new csv "/home/sean/Downloads/contacts.csv"`

In this example, we create a new import map named "google" of type "csv" and provide a sample file.  Figment then reads the file with the CSV file reader to determine what fields are available in the CSV column headers.  This command does not actually import anything - it reads the metadata from the sample file to create a field mapping that must first be customized.  The field-level mappings inferred using the command above can then be viewed using:

``schema person import-map google view`

The output might look similar to the following (trimmed for length and clarity of this example):

```
Import Map            google                                                                        
Schema                person                                                                        
Format                csv                                                                           
Field Configurations  ╭──────────────────────────────┬───────────────┬──────────────┬──────────────╮
                      │ File Field                   │ Property Name │ Skip Missing │ Skip Invalid │
                      ├──────────────────────────────┼───────────────┼──────────────┼──────────────┤
                      │ First Name                   │ <UNSET>       │ ❌           │ ❌           │
                      ├──────────────────────────────┼───────────────┼──────────────┼──────────────┤
                      │ Middle Name                  │ <UNSET>       │ ❌           │ ❌           │
                      ├──────────────────────────────┼───────────────┼──────────────┼──────────────┤
                      │ Last Name                    │ <UNSET>       │ ❌           │ ❌           │
                      ├──────────────────────────────┼───────────────┼──────────────┼──────────────┤
                      │ Name Prefix                  │ <UNSET>       │ ❌           │ ❌           │
                      ├──────────────────────────────┼───────────────┼──────────────┼──────────────┤
                      │ Name Suffix                  │ <UNSET>       │ ❌           │ ❌           │
                      ├──────────────────────────────┼───────────────┼──────────────┼──────────────┤
                      │ Nickname                     │ <UNSET>       │ ❌           │ ❌           │
                      ├──────────────────────────────┼───────────────┼──────────────┼──────────────┤
                      │ File As                      │ <UNSET>       │ ❌           │ ❌           │
                      ├──────────────────────────────┼───────────────┼──────────────┼──────────────┤
                      │ Organization Name            │ <UNSET>       │ ❌           │ ❌           │
                      ├──────────────────────────────┼───────────────┼──────────────┼──────────────┤
                      │ Organization Title           │ <UNSET>       │ ❌           │ ❌           │
                      ├──────────────────────────────┼───────────────┼──────────────┼──────────────┤
                      │ Organization Department      │ <UNSET>       │ ❌           │ ❌           │
                      ├──────────────────────────────┼───────────────┼──────────────┼──────────────┤
                      │ Birthday                     │ <UNSET>       │ ❌           │ ❌           │
                      ├──────────────────────────────┼───────────────┼──────────────┼──────────────┤
                      │ Notes                        │ <UNSET>       │ ❌           │ ❌           │
                      ├──────────────────────────────┼───────────────┼──────────────┼──────────────┤
                      │ E-mail 1 - Label             │ <UNSET>       │ ❌           │ ❌           │
                      ├──────────────────────────────┼───────────────┼──────────────┼──────────────┤
                      │ E-mail 1 - Value             │ <UNSET>       │ ❌           │ ❌           │
                      ├──────────────────────────────┼───────────────┼──────────────┼──────────────┤
                      │ <UNSET>                      │ $Name         │ ✅           │ ✅           │
                      ├──────────────────────────────┼───────────────┼──────────────┼──────────────┤
                      │ <UNSET>                      │ $CreatedOn    │ ❌           │ ❌           │
                      ╰──────────────────────────────┴───────────────┴──────────────┴──────────────╯

```

Each field from the file is read into a "File Field", and fields can be mapped to properties on the `person` schema using the `schema import-map link` command.  This command associates a file field with a schema property.  In addition to the fields read from the file, some metadata properties present on all things, regardless of schema, are shown at the end with a `$` suffix, such as `$Name`.  In this eample, `$Name` is marked as 'Skip Missing' and 'Skip Invaild', meaning that if a name is not provided or is invalid, the record from the CSV file will not be imported.  These attributes can be variously configured for each file field, but are automatically required for `$Name`, as all things must have a name.

The import engine supports simple function notations similar to how Microsoft Excel, LibreOffice Calc, and other popular spreadsheet programs use them.  In this case, we will use the `link` command to set `=TRIM([First Name]&" "&[Last Name])` as the import for the `Name` property on person things when using this import map.  To do this, we use this full command:

`schema person import-map google link "=TRIM([First Name]&' '&[Last Name])" $Name`


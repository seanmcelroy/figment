# Figment

`Figment` is a creative solution for iteratively building knowledge through quick ideation.

The purpose of figment is to let individuals build repositories and graphs of knowledge that help them go about their daily business.  At its core, Figment is a personal information manager (PIM) tool.

`jot` is a command-line interpreter (CLI) for using Figment.

jot runs in two modes, one that takes command line arguments, like most other CLI tools.  Running jot with no arguments on an interactive console runs it in an interactive REPL-style mode.

# Features

* Command line interface (CLI) with an operational interactive mode
* Fully customizable data model for both entities and attributes

# Concepts

## Schemas

There are two major entities in Figment: schemas and things.  Schemas define a set of fields for things of a type, and every thing belongs to at least one schema.  Things can belong to multiple schemas, but they do not have to belong to more than one.

To see an example in practice, you can create a new thing of a new type with the `new` command.  To learn about what commands you can use, you can find details in the CLI help, such as by typing `jot -h` on the command line, or for help specifically with the `new` command, you can type `jot new -h`.

An example usage of the `new` command would be:

```
new restaurant "Golden Corral"
```

If you run `jot` without any command line arguments, you will invoke the interactive mode.  The interactive mode makes it easier to issue multiple successive commands and includes additional functionality, such as menus to select an item when multiple matches exist, rather than simply provide an error in the non-interactive CLI use.

The output from this cmomand in interactive mode is as follows:

```
> new restaurant "Golden Corral"
DONE: Schema restaurant created, and new instance Golden Corral created.
```

Two entities are created: the schema 'restaurant' and the instance 'Golden Corral' which is a type of restaurant.  To view the details of the new restaurant schema in interactive mode, you can "select" the schema and then "view" it.

```
> select restaurant
DONE: Schema restaurant selected.

(restaurant)> view
Instance    : restaurant
GUID        : b08d5e82-6a71-437d-9fd5-95915428d5d4
Type        : Schema

Description : 
Plural      : 

Properties  : (None)
```

Notice when you select an item in jot's interactive mode, the prompt reflects what you have selected.

Schemas have four primary attributes: the name, a randomly assigned GUID, an optional description, and a plural form.  The plural form allows you to view things of the type in interactive mode.  So, if we set that Plural for restaurant to 'restaurants with this command:

```
(restaurant)> set plural restaurants
DONE: restaurant saved.
(restaurant)> ?
Instance    : restaurant
GUID        : b08d5e82-6a71-437d-9fd5-95915428d5d4
Type        : Schema

Description : 
Plural      : restaurants

Properties  : (None)
```

We can now see we've changed that attribute on the schema.  You might notice this time we used the "?" alias for the "view" command to print the details of the selected entity.  It's a handy shortcut, but 'view' would have worked just the same.

Let's create a second restaurant and then list all the restaurants by specifying the plural "restaurants" we set for the restaurant schema:

```
(restaurant)> new restaurant "Riveras Mexican Cafe"
DONE: Riveras Mexican Cafe, a type of restaurant, created.

(Riveras Mexican Cafe)> restaurants
Golden Corral
Riveras Mexican Cafe
```

As an aside, the same functionality is available in the non-interactive mode with the command:

```
$ jot schema restaurant members 
Golden Corral
Riveras Mexican Cafe
```

Or, some CLI commands have an `--as-table` command option, which lets you get a prettier formatted output, such as when issuing `jot schema restaurant members --as-table`:

```
┌──────────────────────┬──────────────────────────────────────┐
│ Name                 │ GUID                                 │
├──────────────────────┼──────────────────────────────────────┤
│ Golden Corral        │ 24878231-4a2e-4e1a-8e41-0a4b3286709b │
│ Riveras Mexican Cafe │ e7495bc8-fd9c-4797-a320-213b54bded7d │
└──────────────────────┴──────────────────────────────────────┘
```

One last thing: You can use the `schemas` command in CLI or interactive modes to get a listing of all schemas.

## Schema Fields

If you wanted to specify restaurants have common attributes, you could select the 'restaurant' schema and then set the attribute for the schema.  Schemas can have any number of fields for types that are supported by Figment.  The supported field types are:

| Field Type | Description                                                   |
| ---------- | ------------------------------------------------------------- |
| array      | multiple text values                                          |
| bool       | yes/no or true/false                                          |
| calculated | A value calculated using a formula defined on the schema.  More on this in a later section. |
| date       | Date and time                                                 |
| email      | Email address                                                 |
| enum       | A single value from a defined list of possible values         |
| integer    | Whole number with no fractional part                          |
| month+day  | Date with only a month and day component                      |
| number     | Floating point number with a decimal point                    |
| phone      | Phone number                                                  |
| ref        | A single reference to a thing of a type defined on this field |
| schema     | A single reference to a schema                                |
| text       | Text                                                          |
| uri        | A uri, which could include a web address, like a URL          |

### Arrays

An `array` field holds any number of individual text lines.  We can select a schema and create an array field on it schema like so in jot's interactive mode;

```
> s person
DONE: Schema person selected
(person)> set address array
DONE: person saved.
```

Then, we can set that value on an instance of a person using these continuing statements:

```
s sean
DONE: Thing Sean selected
(Sean)> set address "[1000 Main Street,Anytown,MA,01583]"
DONE: Sean saved.
```
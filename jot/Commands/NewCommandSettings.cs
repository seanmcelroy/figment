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

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands;

/// <summary>
/// The settings supplied to the <see cref="NewCommand"/>.
/// </summary>
public class NewCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets the name of the <see cref="Schema"/> for which to create a new entity.  If this schema does not exist, it will be created.
    /// </summary>
    [Description("Type of entity to create.  If this schema does not exist, it will be created")]
    [CommandArgument(0, "<SCHEMA>")]
    public string? SchemaName { get; init; }

    /// <summary>
    /// Gets the name of the new <see cref="Thing"/>.  If omitted, only a <see cref="Schema"/> will be created but no thing of that schema's type.
    /// </summary>
    [Description("Name of the new entity.  If omitted, only a schema will be created but no thing of that schema's type")]
    [CommandArgument(1, "[NAME]")]
    public string? ThingName { get; init; }

    /// <summary>
    /// Gets a value indicating whether a <see cref="Schema"/> should be created by default if one with a matching <see cref="SchemaName"/> does not exist.
    /// </summary>
    [Description("Value indicating whether a schema should be created by default if one with a matching <SCHEMA> name does not exist")]
    [CommandOption("--auto-create-schema")]
    [DefaultValue(true)]
    public bool? AutoCreateSchema { get; init; } = true;

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(SchemaName)
            ? ValidationResult.Error("Schema must either be the GUID or a name that resolves to just one")
            : ValidationResult.Success();
    }
}
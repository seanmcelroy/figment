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

namespace jot.Commands.Schemas;

/// <summary>
/// The settings supplied to the <see cref="SchemaRenameCommand"/>.
/// </summary>
public class SchemaRenameCommandSettings : SchemaCommandSettings
{
    /// <summary>
    /// Gets the new name for the <see cref="Figment.Common.Schema"/>.  This is usually one singular term.
    /// </summary>
    [Description("New name for the schema.  This is usually one singular term")]
    [CommandArgument(0, "<NEW_NAME>")]
    required public string NewName { get; init; }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(NewName)
            ? ValidationResult.Error("New schema name must be provided and cannot be blank or only whitespaces")
            : ValidationResult.Success();
    }
}

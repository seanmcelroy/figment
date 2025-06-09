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
/// The settings supplied to the <see cref="AssociateSchemaWithThingCommand"/>.
/// </summary>
public class AssociateSchemaWithThingCommandSettings : SchemaCommandSettings
{
    /// <summary>
    /// Gets the name of the thing to associate with the schema.
    /// </summary>
    [Description("Name of the thing to associate with the schema")]
    [CommandArgument(0, "<THING_NAME>")]
    required public string ThingName { get; init; }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(ThingName)
            ? ValidationResult.Error("Thing name must be set")
            : ValidationResult.Success();
    }

    /// <summary>
    /// Attempts to resolve the specified <see cref="ThingName"/>.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tuple with a indicating whether the resolution was successful, and if so, what the reference to the <see cref="Figment.Common.Thing"/> is.</returns>
    public (Globals.GLOBAL_ERROR_CODES, Figment.Common.Reference thing) ResolveThingName(CancellationToken cancellationToken)
    {
        return Things.ThingCommandSettings.ResolveThingName(ThingName, cancellationToken);
    }
}
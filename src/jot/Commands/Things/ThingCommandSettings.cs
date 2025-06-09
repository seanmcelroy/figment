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
using Figment.Common;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Things;

/// <summary>
/// The settings supplied to commands that only need to target a <see cref="Thing"/>
/// and have no other arguments.
/// </summary>
/// <seealso cref="DeleteThingCommand"/>
/// <seealso cref="ValidateThingCommand"/>
public class ThingCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets the name of the thing to select.
    /// </summary>
    [Description("Name of the thing to select")]
    [CommandArgument(0, "<NAME>")]
    required public string ThingName { get; init; }

    /// <summary>
    /// Gets whether to provide verbose detail, if available, for any outputs.
    /// </summary>
    [Description("Provides verbose detail, if available, for any outputs")]
    [CommandOption("-v|--verbose")]
    required public bool? Verbose { get; init; } = Program.Verbose;

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(ThingName)
            ? ValidationResult.Error("Name must either be the GUID of a thing or a name that resolves to just one")
            : ValidationResult.Success();
    }

    /// <summary>
    /// Attempts to resolve the specified <see cref="ThingName"/>.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tuple with a indicating whether the resolution was successful, and if so, what the reference to the <see cref="Thing"/> is.</returns>
    public (Globals.GLOBAL_ERROR_CODES, Reference thing) ResolveThingName(CancellationToken cancellationToken)
    {
        return ResolveThingName(ThingName, cancellationToken);
    }

    /// <summary>
    /// Attempts to resolve the specified <paramref name="thingName"/>.
    /// </summary>
    /// <param name="thingName">Name of the thing to resolve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tuple with a indicating whether the resolution was successful, and if so, what the reference to the <see cref="Thing"/> is.</returns>
    internal static (Globals.GLOBAL_ERROR_CODES, Reference thing) ResolveThingName(string thingName, CancellationToken cancellationToken)
    {
        var selected = Program.SelectedEntity;
        if (!selected.Equals(Reference.EMPTY) && selected.Type == Reference.ReferenceType.Thing)
        {
            return (Globals.GLOBAL_ERROR_CODES.SUCCESS, selected);
        }

        if (string.IsNullOrWhiteSpace(thingName))
        {
            return (Globals.GLOBAL_ERROR_CODES.ARGUMENT_ERROR, Reference.EMPTY);
        }

        var possibilities = Thing.ResolveAsync(thingName, cancellationToken)
            .ToBlockingEnumerable(cancellationToken)
            .ToArray();

        return possibilities.Length switch
        {
            0 => (Globals.GLOBAL_ERROR_CODES.NOT_FOUND, Reference.EMPTY),
            1 => (Globals.GLOBAL_ERROR_CODES.SUCCESS, possibilities[0]),
            _ => (Globals.GLOBAL_ERROR_CODES.AMBIGUOUS_MATCH, Reference.EMPTY),
        };
    }
}
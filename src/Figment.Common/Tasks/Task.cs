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

namespace Figment.Common.Tasks;

/// <summary>
/// A task is a special type of <see cref="Thing"/> that is used to track planned work.
/// </summary>
public class Task(string guid, string newName) : Thing(guid, newName)
{
    /// <summary>
    /// Tasks.
    /// </summary>
    public const string WellKnownSchemaGuid = "00000000-0000-0000-0000-000000000004";

    /// <summary>
    /// The <see cref="ThingProperty.TruePropertyName"/> of the built-in Task entity type's "id" field.
    /// </summary>
    public const string TrueNameId = $"{WellKnownSchemaGuid}.id";

    /// <summary>
    /// The <see cref="ThingProperty.TruePropertyName"/> of the built-in Task entity type's "complete" field.
    /// </summary>
    public const string TrueNameComplete = $"{WellKnownSchemaGuid}.complete";

    /// <summary>
    /// The due date of this planned work.
    /// </summary>
    public const string TrueNameDue = $"{WellKnownSchemaGuid}.due";

    /// <summary>
    /// The <see cref="ThingProperty.TruePropertyName"/> of the built-in Task entity type's "priority" field.
    /// </summary>
    public const string TrueNamePriority = $"{WellKnownSchemaGuid}.priority";

    /// <summary>
    /// The <see cref="ThingProperty.TruePropertyName"/> of the built-in Task entity type's "archived" field.
    /// </summary>
    public const string TrueNameArchived = $"{WellKnownSchemaGuid}.archived";

    /// <summary>
    /// The user-defined status of this task.
    /// </summary>
    public const string TrueNameStatus = $"{WellKnownSchemaGuid}.status";

    /// <summary>
    /// The <see cref="ThingProperty.TruePropertyName"/> of the built-in Task entity type's "notes" field.
    /// </summary>
    public const string TrueNameNotes = $"{WellKnownSchemaGuid}.notes";
}
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

using System.Text.Json.Serialization;
using Figment.Common;

namespace Figment.Data.Local;

/// <summary>
/// Source generation context for the <see cref="Thing"/> record type,
/// which is required to support trimming.
/// </summary>
/// <remarks>
/// See: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation.
/// </remarks>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)] // Rerquired for polymorphism
[JsonSerializable(typeof(Thing))]
[JsonSerializable(typeof(ulong))]
public partial class ThingSourceGenerationContext : JsonSerializerContext
{
}
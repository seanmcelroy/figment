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
using Figment.Common.Calculations.Parsing;

namespace Figment.Test.Common.Calculations.Parsing;

[TestClass]
public sealed class EvaluationContextTest
{
    [TestMethod]
    public void NullSchemaException()
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CA1806 // Do not ignore method results
        Assert.ThrowsExactly<ArgumentNullException>(static () => new EvaluationContext((Schema?)null));
#pragma warning restore CA1806 // Do not ignore method results
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }

    [TestMethod]
    public void NullThingException()
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CA1806 // Do not ignore method results
        Assert.ThrowsExactly<ArgumentNullException>(static () => new EvaluationContext((Thing?)null));
#pragma warning restore CA1806 // Do not ignore method results
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }

    [TestMethod]
    public void DupeKeys()
    {
#pragma warning disable CA1806 // Do not ignore method results
        Assert.ThrowsExactly<ArgumentException>(static () => new EvaluationContext(new Dictionary<string, object?> {
            { "a", null },
            { "a", null } // Duplicate.
        }));
#pragma warning restore CA1806 // Do not ignore method results
    }
}
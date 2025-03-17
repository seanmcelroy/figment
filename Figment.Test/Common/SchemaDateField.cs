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

namespace Figment.Test.Common;

[TestClass]
public sealed class SchemaDateField
{
    [TestMethod]
    public async Task IsValidAsync()
    {
        var f = new Figment.Common.SchemaDateField(nameof(IsValidAsync));

        var s = await f.GetReadableFieldTypeAsync(false, CancellationToken.None);

        Assert.IsNotNull(s);
        Assert.AreEqual("date", s, StringComparer.Ordinal);
        Assert.AreEqual("string", f.Type, StringComparer.Ordinal);

        f.Required = false;
        Assert.IsTrue(await f.IsValidAsync(null, CancellationToken.None));

        f.Required = true;
        Assert.IsFalse(await f.IsValidAsync(null, CancellationToken.None));

        f.Required = false;
        Assert.IsTrue(await f.IsValidAsync(DateTime.UtcNow, CancellationToken.None));

        f.Required = true;
        Assert.IsTrue(await f.IsValidAsync(DateTime.UtcNow, CancellationToken.None));

        f.Required = false;
        Assert.IsTrue(await f.IsValidAsync(DateTimeOffset.UtcNow, CancellationToken.None));

        f.Required = true;
        Assert.IsTrue(await f.IsValidAsync(DateTimeOffset.UtcNow, CancellationToken.None));

        f.Required = true;
        Assert.IsTrue(await f.IsValidAsync("1981-01-26", CancellationToken.None), "Date values may not be native dates, but string dates are still valid");

        f.Required = false;
        Assert.IsFalse(await f.IsValidAsync(string.Empty, CancellationToken.None));
        Assert.IsFalse(await f.IsValidAsync("whatever", CancellationToken.None));
        Assert.IsFalse(await f.IsValidAsync("true", CancellationToken.None));
        Assert.IsFalse(await f.IsValidAsync("false", CancellationToken.None));
    }

    [TestMethod]
    public void TryParseDate()
    {
        Assert.IsTrue(Figment.Common.SchemaDateField.TryParseDate("1981-01-26", out DateTimeOffset d));
        Assert.AreEqual(1981, d.Year);
        Assert.AreEqual(01, d.Month);
        Assert.AreEqual(26, d.Day);
    }

    [TestMethod]
    public void TryMassageInput()
    {
        var f = new Figment.Common.SchemaDateField(nameof(TryMassageInput));

        Assert.IsTrue(f.TryMassageInput(null, out object? output));
        Assert.IsNull(output);

        DateTimeOffset reference = new(1981, 01, 26, 0, 0, 0, TimeSpan.Zero);

        Assert.IsFalse(f.TryMassageInput("February March", out output));
        Assert.IsNull(output);

        Assert.IsTrue(f.TryMassageInput("Jan 26 1981", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<DateTimeOffset>(output);
        Assert.AreEqual(reference.Year, ((DateTimeOffset)output).Year);
        Assert.AreEqual(reference.Month, ((DateTimeOffset)output).Month);
        Assert.AreEqual(reference.Day, ((DateTimeOffset)output).Day);

        Assert.IsTrue(f.TryMassageInput("Jan 26, 1981", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<DateTimeOffset>(output);
        Assert.AreEqual(reference.Year, ((DateTimeOffset)output).Year);
        Assert.AreEqual(reference.Month, ((DateTimeOffset)output).Month);
        Assert.AreEqual(reference.Day, ((DateTimeOffset)output).Day);

        Assert.IsTrue(f.TryMassageInput("1981-01-26", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<DateTimeOffset>(output);
        Assert.AreEqual(reference.Year, ((DateTimeOffset)output).Year);
        Assert.AreEqual(reference.Month, ((DateTimeOffset)output).Month);
        Assert.AreEqual(reference.Day, ((DateTimeOffset)output).Day);

        Assert.IsTrue(f.TryMassageInput("1/26/1981", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<DateTimeOffset>(output);
        Assert.AreEqual(reference.Year, ((DateTimeOffset)output).Year);
        Assert.AreEqual(reference.Month, ((DateTimeOffset)output).Month);
        Assert.AreEqual(reference.Day, ((DateTimeOffset)output).Day);

        Assert.IsTrue(f.TryMassageInput("1/26/81", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<DateTimeOffset>(output);
        Assert.AreEqual(reference.Year, ((DateTimeOffset)output).Year);
        Assert.AreEqual(reference.Month, ((DateTimeOffset)output).Month);
        Assert.AreEqual(reference.Day, ((DateTimeOffset)output).Day);

        Assert.IsTrue(f.TryMassageInput("January 26, 1981", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<DateTimeOffset>(output);
        Assert.AreEqual(reference.Year, ((DateTimeOffset)output).Year);
        Assert.AreEqual(reference.Month, ((DateTimeOffset)output).Month);
        Assert.AreEqual(reference.Day, ((DateTimeOffset)output).Day);

        Assert.IsFalse(f.TryMassageInput(Math.PI, out output));
    }

}
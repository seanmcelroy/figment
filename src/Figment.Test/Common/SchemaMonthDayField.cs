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
public sealed class SchemaMonthDayField
{
    [TestMethod]
    public async Task IsValidAsync()
    {
        var f = new Figment.Common.SchemaMonthDayField(nameof(IsValidAsync));

        var s = await f.GetReadableFieldTypeAsync(false, CancellationToken.None);

        Assert.IsNotNull(s);
        Assert.AreEqual("month+day", s, StringComparer.Ordinal);
        Assert.AreEqual("integer", f.Type, StringComparer.Ordinal);

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
        Assert.IsFalse(await f.IsValidAsync("1-26", CancellationToken.None), "Month+day values must be native integers");

        f.Required = true;
        Assert.IsTrue(await f.IsValidAsync(0126, CancellationToken.None), "January 26 is a valid month+day");

        f.Required = true;
        Assert.IsFalse(await f.IsValidAsync(0, CancellationToken.None), "0 is not a valid month+day");

        f.Required = true;
        Assert.IsFalse(await f.IsValidAsync(int.MaxValue, CancellationToken.None), "int.MaxValue is not a valid month+day");

        f.Required = true;
        Assert.IsTrue(await f.IsValidAsync(0126U, CancellationToken.None), "January 26 is a valid month+day");

        f.Required = true;
        Assert.IsTrue(await f.IsValidAsync(0126L, CancellationToken.None), "January 26 is a valid month+day");

        f.Required = true;
        Assert.IsTrue(await f.IsValidAsync(0126UL, CancellationToken.None), "January 26 is a valid month+day");

        f.Required = true;
        Assert.IsFalse(await f.IsValidAsync(0200, CancellationToken.None), "Days cannot be zero in a month+day");
        Assert.IsFalse(await f.IsValidAsync(0231, CancellationToken.None), "February 31 is not a valid month+day");
        Assert.IsFalse(await f.IsValidAsync(0001, CancellationToken.None), "Months cannot be zero in a month+day");
        Assert.IsFalse(await f.IsValidAsync(1301, CancellationToken.None), "There are not 13 months");
        Assert.IsFalse(await f.IsValidAsync(-0126, CancellationToken.None), "Negative numbers are not month+day");
        Assert.IsFalse(await f.IsValidAsync(Math.PI, CancellationToken.None), "Floating point numbers are not month+day values");
        Assert.IsFalse(await f.IsValidAsync(string.Empty, CancellationToken.None));
        Assert.IsFalse(await f.IsValidAsync("whatever", CancellationToken.None));
        Assert.IsFalse(await f.IsValidAsync("true", CancellationToken.None));
        Assert.IsFalse(await f.IsValidAsync("false", CancellationToken.None));
    }

    [TestMethod]
    public void TryParseMonthDay()
    {
        Assert.IsTrue(Figment.Common.SchemaMonthDayField.TryParseMonthDay("126", out int i));
        Assert.AreEqual(0126, i);

        Assert.IsTrue(Figment.Common.SchemaMonthDayField.TryParseMonthDay("1/26", out i));
        Assert.AreEqual(0126, i);

        Assert.IsTrue(Figment.Common.SchemaMonthDayField.TryParseMonthDay("01-26", out i));
        Assert.AreEqual(0126, i);

        Assert.IsTrue(Figment.Common.SchemaMonthDayField.TryParseMonthDay("Jan 26", out i));
        Assert.AreEqual(0126, i);

        Assert.IsTrue(Figment.Common.SchemaMonthDayField.TryParseMonthDay("January 26", out i));
        Assert.AreEqual(0126, i);
    }

    [TestMethod]
    public void TryMassageInput()
    {
        var f = new Figment.Common.SchemaMonthDayField(nameof(TryMassageInput));

        Assert.IsTrue(f.TryMassageInput(null, out object? output));
        Assert.IsNull(output);

        DateTimeOffset reference = new(1981, 01, 26, 0, 0, 0, TimeSpan.Zero);

        Assert.IsFalse(f.TryMassageInput("February March", out output));
        Assert.IsNull(output);

        Assert.IsTrue(f.TryMassageInput("Jan 26 1981", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<int>(output);
        Assert.AreEqual(126, (int)output);

        Assert.IsTrue(f.TryMassageInput("Jan 26, 1981", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<int>(output);
        Assert.AreEqual(126, (int)output);

        Assert.IsTrue(f.TryMassageInput("1981-01-26", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<int>(output);
        Assert.AreEqual(126, (int)output);

        Assert.IsTrue(f.TryMassageInput("1/26/1981", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<int>(output);
        Assert.AreEqual(126, (int)output);

        Assert.IsTrue(f.TryMassageInput("1/26/81", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<int>(output);
        Assert.AreEqual(126, (int)output);

        Assert.IsTrue(f.TryMassageInput("January 26, 1981", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<int>(output);
        Assert.AreEqual(126, (int)output);

        Assert.IsTrue(f.TryMassageInput(0126, out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<int>(output);
        Assert.AreEqual(126, (int)output);

        Assert.IsFalse(f.TryMassageInput(Math.PI, out output));
    }

}
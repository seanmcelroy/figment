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
        Assert.AreEqual("date", f.Format);
        Assert.AreEqual("string", f.Type);

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
    public void TryParseDateYYYYMMDD()
    {
        Assert.IsTrue(Figment.Common.SchemaDateField.TryParseDate("1981-01-26", out DateTimeOffset d));
        Assert.AreEqual(1981, d.Year);
        Assert.AreEqual(01, d.Month);
        Assert.AreEqual(26, d.Day);
    }

    [TestMethod]
    public void TryParseDateImpossibleLeapDate()
    {
        Assert.IsFalse(Figment.Common.SchemaDateField.TryParseDate("February 29, 2001", out DateTimeOffset _));
    }

    [TestMethod]
    public void TryParseDatePossibleLeapDate()
    {
        Assert.IsTrue(Figment.Common.SchemaDateField.TryParseDate("February 29, 2024", out DateTimeOffset d));
        Assert.AreEqual(2024, d.Year);
        Assert.AreEqual(02, d.Month);
        Assert.AreEqual(29, d.Day);
    }

    [TestMethod]
    public void TryParseDateMMMDDYesterday()
    {
        var yesterday = DateTime.Now.AddDays(-1);
        var yesterdayText = yesterday.ToString("MMMM dd");
        Assert.IsTrue(Figment.Common.SchemaDateField.TryParseDate(yesterdayText, out DateTimeOffset d));
        Assert.AreEqual(yesterday.Year + 1, d.Year); // Future.
        Assert.AreEqual(yesterday.Month, d.Month);
        Assert.AreEqual(yesterday.Day, d.Day);
    }

    [TestMethod]
    public void TryParseDateMMMDDToday()
    {
        var today = DateTime.Now;
        var todayText = today.ToString("MMMM dd");
        Assert.IsTrue(Figment.Common.SchemaDateField.TryParseDate(todayText, out DateTimeOffset d));
        Assert.AreEqual(today.Year, d.Year);
        Assert.AreEqual(today.Month, d.Month);
        Assert.AreEqual(today.Day, d.Day);
    }

    [TestMethod]
    public void TryParseDateMMMDDTomorrow()
    {
        var tomorrow = DateTime.Now.AddDays(1);
        var tomorrowText = tomorrow.ToString("MMMM dd");
        Assert.IsTrue(Figment.Common.SchemaDateField.TryParseDate(tomorrowText, out DateTimeOffset d));
        Assert.AreEqual(tomorrow.Year, d.Year);
        Assert.AreEqual(tomorrow.Month, d.Month);
        Assert.AreEqual(tomorrow.Day, d.Day);
    }

    [TestMethod]
    public void TryParseDateToday()
    {
        var target = DateTimeOffset.Now.Date;

        Assert.IsTrue(Figment.Common.SchemaDateField.TryParseDate("today", out DateTimeOffset d));
        Assert.AreEqual(target.Year, d.Year);
        Assert.AreEqual(target.Month, d.Month);
        Assert.AreEqual(target.Day, d.Day);
    }

    [TestMethod]
    public void TryParseDateYesterday()
    {
        var target = DateTimeOffset.Now.Date.AddDays(-1);

        Assert.IsTrue(Figment.Common.SchemaDateField.TryParseDate("yesterday", out DateTimeOffset d));
        Assert.AreEqual(target.Year, d.Year);
        Assert.AreEqual(target.Month, d.Month);
        Assert.AreEqual(target.Day, d.Day);
    }

    [TestMethod]
    public void TryParseDateTomorrow()
    {
        var target = DateTimeOffset.Now.Date.AddDays(1);

        Assert.IsTrue(Figment.Common.SchemaDateField.TryParseDate("tomorrow", out DateTimeOffset d));
        Assert.AreEqual(target.Year, d.Year);
        Assert.AreEqual(target.Month, d.Month);
        Assert.AreEqual(target.Day, d.Day);
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
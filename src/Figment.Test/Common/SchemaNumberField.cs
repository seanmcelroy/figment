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
public sealed class SchemaNumberField
{
    [TestMethod]
    public async Task IsValidAsync()
    {
        var f = new Figment.Common.SchemaNumberField(nameof(IsValidAsync));

        var s = await f.GetReadableFieldTypeAsync(false, CancellationToken.None);

        Assert.IsNotNull(s);
        Assert.AreEqual("number", s, StringComparer.Ordinal);
        Assert.AreEqual("number", f.Type, StringComparer.Ordinal);

        f.Required = false;
        Assert.IsTrue(await f.IsValidAsync(null, CancellationToken.None));

        f.Required = true;
        Assert.IsFalse(await f.IsValidAsync(null, CancellationToken.None));

        f.Required = false;
        Assert.IsTrue(await f.IsValidAsync(-5, CancellationToken.None));

        f.Required = true;
        Assert.IsTrue(await f.IsValidAsync(-5, CancellationToken.None));

        f.Required = false;
        Assert.IsTrue(await f.IsValidAsync(5, CancellationToken.None));

        f.Required = true;
        Assert.IsTrue(await f.IsValidAsync(5, CancellationToken.None));

        f.Required = false;
        Assert.IsTrue(await f.IsValidAsync(byte.MinValue, CancellationToken.None));

        f.Required = true;
        Assert.IsTrue(await f.IsValidAsync(byte.MinValue, CancellationToken.None));

        f.Required = false;
        Assert.IsTrue(await f.IsValidAsync(byte.MaxValue, CancellationToken.None));

        f.Required = true;
        Assert.IsTrue(await f.IsValidAsync(byte.MaxValue, CancellationToken.None));

        f.Required = false;
        Assert.IsTrue(await f.IsValidAsync(float.MinValue, CancellationToken.None));

        f.Required = true;
        Assert.IsTrue(await f.IsValidAsync(float.MinValue, CancellationToken.None));

        f.Required = false;
        Assert.IsTrue(await f.IsValidAsync(float.MaxValue, CancellationToken.None));

        f.Required = true;
        Assert.IsTrue(await f.IsValidAsync(float.MaxValue, CancellationToken.None));

        f.Required = false;
        Assert.IsTrue(await f.IsValidAsync(int.MinValue, CancellationToken.None));

        f.Required = true;
        Assert.IsTrue(await f.IsValidAsync(int.MinValue, CancellationToken.None));

        f.Required = false;
        Assert.IsTrue(await f.IsValidAsync(int.MaxValue, CancellationToken.None));

        f.Required = true;
        Assert.IsTrue(await f.IsValidAsync(int.MaxValue, CancellationToken.None));

        f.Required = false;
        Assert.IsTrue(await f.IsValidAsync(double.MinValue, CancellationToken.None));

        f.Required = true;
        Assert.IsTrue(await f.IsValidAsync(double.MinValue, CancellationToken.None));

        f.Required = false;
        Assert.IsTrue(await f.IsValidAsync(double.MaxValue, CancellationToken.None));

        f.Required = true;
        Assert.IsTrue(await f.IsValidAsync(double.MaxValue, CancellationToken.None));

        f.Required = false;
        Assert.IsTrue(await f.IsValidAsync(long.MinValue, CancellationToken.None));

        f.Required = true;
        Assert.IsTrue(await f.IsValidAsync(long.MinValue, CancellationToken.None));

        f.Required = false;
        Assert.IsTrue(await f.IsValidAsync(long.MaxValue, CancellationToken.None));

        f.Required = true;
        Assert.IsTrue(await f.IsValidAsync(long.MaxValue, CancellationToken.None));

        f.Required = false;
        Assert.IsTrue(await f.IsValidAsync(ulong.MinValue, CancellationToken.None));

        f.Required = true;
        Assert.IsTrue(await f.IsValidAsync(ulong.MinValue, CancellationToken.None));

        f.Required = false;
        Assert.IsTrue(await f.IsValidAsync(ulong.MaxValue, CancellationToken.None));

        f.Required = true;
        Assert.IsTrue(await f.IsValidAsync(ulong.MaxValue, CancellationToken.None));

        f.Required = false;
        Assert.IsTrue(await f.IsValidAsync("12345", CancellationToken.None), "Number values must be native number types");

        Assert.IsTrue(await f.IsValidAsync(Math.PI, CancellationToken.None), "PI is a number");
        Assert.IsFalse(await f.IsValidAsync(string.Empty, CancellationToken.None));
        Assert.IsFalse(await f.IsValidAsync("whatever", CancellationToken.None));
        Assert.IsFalse(await f.IsValidAsync("true", CancellationToken.None));
        Assert.IsFalse(await f.IsValidAsync("false", CancellationToken.None));
    }

    [TestMethod]
    public void TryMassageInput()
    {
        var f = new Figment.Common.SchemaNumberField(nameof(TryMassageInput));

        Assert.IsFalse(f.TryMassageInput(null, out object? output));
        Assert.IsNull(output);

        Assert.IsFalse(f.TryMassageInput("string", out output));
        Assert.IsNull(output);

        Assert.IsTrue(f.TryMassageInput(byte.MinValue, out output), "Native byte can be parsed");
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<byte>(output);
        Assert.AreEqual(byte.MinValue, (byte)output);

        Assert.IsTrue(f.TryMassageInput($"{byte.MinValue}", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<ulong>(output);
        Assert.AreEqual(byte.MinValue, (ulong)output);

        Assert.IsTrue(f.TryMassageInput($"{byte.MaxValue}", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<ulong>(output);
        Assert.AreEqual(byte.MaxValue, (ulong)output);

        Assert.IsTrue(f.TryMassageInput(int.MinValue, out output), "Native int can be parsed");
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<int>(output, "Negative integers are ultimately treated as signed longs");
        Assert.AreEqual(int.MinValue, (int)output);

        Assert.IsTrue(f.TryMassageInput($"{int.MinValue}", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<long>(output, "Negative integers are ultimately treated as signed longs");
        Assert.AreEqual(int.MinValue, (long)output);

        Assert.IsTrue(f.TryMassageInput($"{int.MaxValue}", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<ulong>(output, "Positive integers are ultimately treated as unsigned longs");
        Assert.AreEqual((ulong)int.MaxValue, (ulong)output);

        Assert.IsTrue(f.TryMassageInput(long.MinValue, out output), "Native long can be parsed");
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<long>(output);
        Assert.AreEqual(long.MinValue, (long)output);

        Assert.IsTrue(f.TryMassageInput($"{long.MinValue}", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<long>(output);
        Assert.AreEqual(long.MinValue, (long)output);

        Assert.IsTrue(f.TryMassageInput(ulong.MinValue, out output), "Native ulong can be parsed");
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<ulong>(output);
        Assert.AreEqual(ulong.MinValue, (ulong)output);

        Assert.IsTrue(f.TryMassageInput($"{ulong.MinValue}", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<ulong>(output);
        Assert.AreEqual(ulong.MinValue, (ulong)output);

        Assert.IsTrue(f.TryMassageInput(float.MinValue, out output), "Native float can be parsed");
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<float>(output);
        Assert.IsTrue((float)output - float.MinValue <= float.Epsilon);

        Assert.IsTrue(f.TryMassageInput($"{float.MinValue}", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<double>(output);
        Assert.IsTrue((double)output - float.MinValue <= double.Epsilon);

        Assert.IsTrue(f.TryMassageInput(double.MinValue, out output), "Native double can be parsed");
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<double>(output);
        Assert.IsTrue((double)output - double.MinValue <= double.Epsilon);

        Assert.IsTrue(f.TryMassageInput($"{double.MinValue}", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<double>(output);
        Assert.IsTrue((double)output - double.MinValue <= double.Epsilon);

        Assert.IsTrue(f.TryMassageInput(Math.PI, out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<double>(output);
        Assert.IsTrue((double)output - Math.PI <= double.Epsilon);

        Assert.IsFalse(f.TryMassageInput('c', out output));
        Assert.IsNull(output);
    }
}
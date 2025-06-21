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
public sealed class SchemaPhoneField
{
    [TestMethod]
    public async Task IsValidAsync()
    {
        var f = new Figment.Common.SchemaPhoneField(nameof(IsValidAsync));

        var s = await f.GetReadableFieldTypeAsync(false, CancellationToken.None);

        Assert.IsNotNull(s);
        Assert.AreEqual("phone", s, StringComparer.Ordinal);
        Assert.AreEqual("string", f.Type, StringComparer.Ordinal);

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
        Assert.IsTrue(await f.IsValidAsync(string.Empty, CancellationToken.None));
        Assert.IsTrue(await f.IsValidAsync("whatever", CancellationToken.None));
        Assert.IsTrue(await f.IsValidAsync("true", CancellationToken.None));
        Assert.IsTrue(await f.IsValidAsync("false", CancellationToken.None));
    }

    [TestMethod]
    public void TryMassageInput()
    {
        var f = new Figment.Common.SchemaPhoneField(nameof(TryMassageInput));

        Assert.IsTrue(f.TryMassageInput("string", out object? output));
        Assert.IsNotNull(output);

        Assert.IsTrue(f.TryMassageInput(byte.MinValue, out output), "Native byte can be parsed");
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<string>(output);
        Assert.AreEqual(byte.MinValue.ToString(), (string)output);

        Assert.IsTrue(f.TryMassageInput($"{byte.MinValue}", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<string>(output);
        Assert.AreEqual($"{byte.MinValue}", (string)output);

        Assert.IsTrue(f.TryMassageInput($"{byte.MaxValue}", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<string>(output);
        Assert.AreEqual($"{byte.MaxValue}", (string)output);

        Assert.IsTrue(f.TryMassageInput(int.MinValue, out output), "Native int can be parsed");
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<string>(output);
        Assert.AreEqual(int.MinValue.ToString(), (string)output);

        Assert.IsTrue(f.TryMassageInput($"{int.MinValue}", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<string>(output);
        Assert.AreEqual(int.MinValue.ToString(), (string)output);

        Assert.IsTrue(f.TryMassageInput($"{int.MaxValue}", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<string>(output);
        Assert.AreEqual(int.MaxValue.ToString(), (string)output);

        Assert.IsTrue(f.TryMassageInput(long.MinValue, out output), "Native long can be parsed");
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<string>(output);
        Assert.AreEqual(long.MinValue.ToString(), (string)output);

        Assert.IsTrue(f.TryMassageInput($"{long.MinValue}", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<string>(output);
        Assert.AreEqual(long.MinValue.ToString(), (string)output);

        Assert.IsTrue(f.TryMassageInput(ulong.MinValue, out output), "Native ulong can be parsed");
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<string>(output);
        Assert.AreEqual(ulong.MinValue.ToString(), (string)output);

        Assert.IsTrue(f.TryMassageInput($"{ulong.MinValue}", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<string>(output);
        Assert.AreEqual(ulong.MinValue.ToString(), (string)output);

        Assert.IsTrue(f.TryMassageInput(float.MinValue, out output), "Native float can be parsed");
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<string>(output);

        Assert.IsTrue(f.TryMassageInput($"{float.MinValue}", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<string>(output);

        Assert.IsTrue(f.TryMassageInput(double.MinValue, out output), "Native double can be parsed");
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<string>(output);

        Assert.IsTrue(f.TryMassageInput($"{double.MinValue}", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<string>(output);

        Assert.IsTrue(f.TryMassageInput(Math.PI, out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<string>(output);

        Assert.IsTrue(f.TryMassageInput('c', out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<string>(output);
        Assert.AreEqual("c", (string)output);
    }
}
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
public sealed class SchemaBooleanField
{
    [TestMethod]
    public async Task IsValidAsync()
    {
        var f = new Figment.Common.SchemaBooleanField(nameof(IsValidAsync));

        var s = await f.GetReadableFieldTypeAsync(CancellationToken.None);

        Assert.IsNotNull(s);
        Assert.AreEqual("bool", s, StringComparer.Ordinal);
        Assert.AreEqual("bool", f.Type, StringComparer.Ordinal);

        f.Required = false;
        Assert.IsTrue(await f.IsValidAsync(null, CancellationToken.None));

        f.Required = true;
        Assert.IsFalse(await f.IsValidAsync(null, CancellationToken.None));

        f.Required = false;
        Assert.IsTrue(await f.IsValidAsync(true, CancellationToken.None));

        f.Required = true;
        Assert.IsTrue(await f.IsValidAsync(true, CancellationToken.None));

        f.Required = false;
        Assert.IsTrue(await f.IsValidAsync(false, CancellationToken.None));

        f.Required = true;
        Assert.IsTrue(await f.IsValidAsync(false, CancellationToken.None));

        f.Required = false;
        Assert.IsFalse(await f.IsValidAsync(string.Empty, CancellationToken.None), "Empty string cannot be coerced into a boolean value");
        Assert.IsFalse(await f.IsValidAsync("whatever", CancellationToken.None));
        Assert.IsFalse(await f.IsValidAsync("true", CancellationToken.None), "Boolean values should be native booleans, not strings");
        Assert.IsFalse(await f.IsValidAsync("false", CancellationToken.None), "Boolean values should be native booleans, not strings");
    }

    [TestMethod]
    public void TryParseBoolean()
    {
        Assert.IsTrue(Figment.Common.SchemaBooleanField.TryParseBoolean("yes", out bool r));
        Assert.IsTrue(r);
        Assert.IsTrue(Figment.Common.SchemaBooleanField.TryParseBoolean("no", out r));
        Assert.IsFalse(r);
        Assert.IsTrue(Figment.Common.SchemaBooleanField.TryParseBoolean("on", out r));
        Assert.IsTrue(r);
        Assert.IsTrue(Figment.Common.SchemaBooleanField.TryParseBoolean("off", out r));
        Assert.IsFalse(r);
        Assert.IsTrue(Figment.Common.SchemaBooleanField.TryParseBoolean("1", out r));
        Assert.IsTrue(r);
        Assert.IsTrue(Figment.Common.SchemaBooleanField.TryParseBoolean("0", out r));
        Assert.IsFalse(r);
        Assert.IsTrue(Figment.Common.SchemaBooleanField.TryParseBoolean("true", out r));
        Assert.IsTrue(r);
        Assert.IsTrue(Figment.Common.SchemaBooleanField.TryParseBoolean("false", out r));
        Assert.IsFalse(r);

        Assert.IsFalse(Figment.Common.SchemaBooleanField.TryParseBoolean(null, out r));
        Assert.IsFalse(r);
        Assert.IsFalse(Figment.Common.SchemaBooleanField.TryParseBoolean(string.Empty, out r));
        Assert.IsFalse(r);
    }

    [TestMethod]
    public void TryMassageInput()
    {
        var f = new Figment.Common.SchemaBooleanField(nameof(TryMassageInput));

        Assert.IsTrue(f.TryMassageInput(null, out object? output));
        Assert.IsNull(output);

        Assert.IsTrue(f.TryMassageInput(0, out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<bool>(output);
        Assert.IsFalse((bool)output);

        Assert.IsTrue(f.TryMassageInput(1, out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<bool>(output);
        Assert.IsTrue((bool)output);

        Assert.IsTrue(f.TryMassageInput("false", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<bool>(output);
        Assert.IsFalse((bool)output);

        Assert.IsTrue(f.TryMassageInput("true", out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<bool>(output);
        Assert.IsTrue((bool)output);

        Assert.IsTrue(f.TryMassageInput(false, out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<bool>(output);
        Assert.IsFalse((bool)output);

        Assert.IsTrue(f.TryMassageInput(true, out output));
        Assert.IsNotNull(output);
        Assert.IsInstanceOfType<bool>(output);
        Assert.IsTrue((bool)output);

        Assert.IsFalse(f.TryMassageInput(Math.PI, out output));

    }

}
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
public sealed class SchemaUriField
{
    [TestMethod]
    public async Task IsValidAsync()
    {
        var f = new Figment.Common.SchemaUriField(nameof(IsValidAsync));

        var s = await f.GetReadableFieldTypeAsync(false, CancellationToken.None);

        Assert.IsNotNull(s);
        Assert.AreEqual("uri", s, StringComparer.Ordinal);
        Assert.AreEqual("string", f.Type, StringComparer.Ordinal);

        f.Required = false;
        Assert.IsTrue(await f.IsValidAsync(null, CancellationToken.None));

        f.Required = true;
        Assert.IsFalse(await f.IsValidAsync(null, CancellationToken.None));

        f.Required = false;
        Assert.IsTrue(await f.IsValidAsync("http://example.com", CancellationToken.None));

        f.Required = true;
        Assert.IsTrue(await f.IsValidAsync("http://example.com/", CancellationToken.None));

        f.Required = false;
        Assert.IsTrue(await f.IsValidAsync("ftp://sub.example.com", CancellationToken.None));

        f.Required = true;
        Assert.IsTrue(await f.IsValidAsync("ftp://sub.example.com/", CancellationToken.None));

        f.Required = false;
        Assert.IsFalse(await f.IsValidAsync("example.com", CancellationToken.None));
        Assert.IsFalse(await f.IsValidAsync("example.com/", CancellationToken.None));
        Assert.IsFalse(await f.IsValidAsync("/example.com", CancellationToken.None));
        Assert.IsFalse(await f.IsValidAsync("/example.com/", CancellationToken.None));

        Assert.IsFalse(await f.IsValidAsync(byte.MinValue, CancellationToken.None));
        Assert.IsFalse(await f.IsValidAsync(float.MinValue, CancellationToken.None));
        Assert.IsFalse(await f.IsValidAsync(int.MinValue, CancellationToken.None));
        Assert.IsFalse(await f.IsValidAsync(double.MinValue, CancellationToken.None));
        Assert.IsFalse(await f.IsValidAsync(long.MinValue, CancellationToken.None));
        Assert.IsFalse(await f.IsValidAsync(ulong.MinValue, CancellationToken.None));
        Assert.IsFalse(await f.IsValidAsync(string.Empty, CancellationToken.None));
        Assert.IsFalse(await f.IsValidAsync(false, CancellationToken.None));
    }

    [TestMethod]
    public void TryMassageInput_Pass()
    {
        var f = new Figment.Common.SchemaUriField(nameof(TryMassageInput_Pass));

        Assert.IsTrue(f.TryMassageInput("http://example.com/", out object? output));
        Assert.IsNotNull(output);
    }

    [TestMethod]
    public void TryMassageInput_Fail_NoScheme()
    {
        var f = new Figment.Common.SchemaUriField(nameof(TryMassageInput_Fail_NoScheme));

        Assert.IsFalse(f.TryMassageInput("://example.com/", out object? output));
        Assert.IsFalse(f.TryMassageInput("//example.com/", out output));
        Assert.IsNull(output);
        Assert.IsFalse(f.TryMassageInput("example.com/", out output));
        Assert.IsNull(output);
        Assert.IsFalse(f.TryMassageInput("example.com", out output));
        Assert.IsNull(output);
    }

}
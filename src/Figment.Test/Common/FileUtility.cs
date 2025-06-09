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
public sealed class FileUtility
{
    [TestMethod]
    public void ExpandRelativePaths()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(home))
        {
            Assert.Inconclusive("User profile has no value");
        }

        Assert.AreEqual(home + "/foo", Figment.Common.FileUtility.ExpandRelativePaths("~/foo"));
        Assert.AreEqual(home, Figment.Common.FileUtility.ExpandRelativePaths("~/"));
        Assert.AreEqual(home + "/bar/sample~2.flac", Figment.Common.FileUtility.ExpandRelativePaths("~/bar/sample~2.flac"));
        Assert.AreEqual("./foo/sample~2.flac", Figment.Common.FileUtility.ExpandRelativePaths("./foo/sample~2.flac"));
        Assert.AreEqual("c:/foo/intern~1/file.txt", Figment.Common.FileUtility.ExpandRelativePaths("c:/foo/intern~1/file.txt"));
    }
}
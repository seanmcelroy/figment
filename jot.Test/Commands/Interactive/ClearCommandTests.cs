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

using jot.Commands.Interactive;
using jot.Test.TestUtilities;
using Spectre.Console.Cli;

namespace jot.Test.Commands.Interactive;

[TestClass]
public class ClearCommandTests : CommandTestBase
{
    protected override void ConfigureCommands(CommandApp app)
    {
        app.Configure(config =>
        {
            config.AddCommand<ClearCommand>("clear");
        });
    }

    [TestMethod]
    public async Task ExecuteAsync_ShouldClearConsole()
    {
        // Act
        var result = await ExecuteCommandAsync("clear");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        // Note: Clear command typically just clears the console, so not much to assert
    }
}
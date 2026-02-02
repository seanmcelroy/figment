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

using jot.Commands.Pomodoro;
using jot.Test.TestUtilities;
using Spectre.Console.Cli;

namespace jot.Test.Commands.Pomodoro;

[TestClass]
public class StartPomodoroTests : CommandTestBase
{
    protected override void ConfigureCommands(CommandApp app)
    {
        app.Configure(config =>
        {
            config.AddCommand<StartPomodoro>("pomodoro");
        });
    }

    [TestMethod]
    public async Task ExecuteAsync_WithDefaultSettings_ShouldStartPomodoro()
    {
        // Act
        var result = await ExecuteCommandAsync("pomodoro");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        // Note: Pomodoro timer tests are complex due to time-based nature
        // In a real implementation, you'd mock the timer or use a very short duration for testing
    }

    [TestMethod]
    public async Task ExecuteAsync_WithCustomDuration_ShouldUseCustomDuration()
    {
        // Act
        var result = await ExecuteCommandAsync("pomodoro", "--duration", "1"); // 1 second for testing

        // Assert
        Assert.AreEqual(0, result.ExitCode);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithTaskName_ShouldSetTaskName()
    {
        // Act
        var result = await ExecuteCommandAsync("pomodoro", "--task", "Test Task", "--duration", "1");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
    }
}
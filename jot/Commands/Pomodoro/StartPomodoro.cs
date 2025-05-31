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

using System.Diagnostics;
using Figment.Common;
using Figment.Common.Calculations.Functions;
using Figment.Common.Errors;
using Spectre.Console;
using Spectre.Console.Cli;

namespace jot.Commands.Pomodoro;

/// <summary>
/// Permanently deletes a <see cref="Thing"/>.
/// </summary>
public class StartPomodoro : CancellableAsyncCommand<StartPomodoroSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, StartPomodoroSettings settings, CancellationToken cancellationToken)
    {
        double totalWork = 0;

        Stopwatch sw = new();
        sw.Start();

        var interactive = AnsiConsole.Profile.Capabilities.Interactive;

        if (interactive)
        {
            AnsiConsole.MarkupLine("You can type a letter to E[[[red]x[/]]]it the timer, take a [[[gold1]s[/]]]hort break, a [[[orangered1]l[/]]]ong break, [[[steelblue1_1]r[/]]]esume working when on break, or mark this task fully [[[green]d[/]]]one.");
        }

        Stack<ProgressTask> taskStack = new();

        var unicode = AnsiConsole.Profile.Capabilities.Unicode;
        string tomato = unicode ? ":tomato:" : "*";
        string shortBreak = unicode ? ":restroom:" : ".";
        string longBreak = unicode ? ":person_walking:" : "!";

        await AnsiConsole.Progress()
            .Columns(
            [
                new TaskDescriptionColumn(),    // Task description
                new ProgressBarColumn(),        // Progress bar
                new PercentageColumn(),         // Percentage
                new ElapsedTimeColumn(),        // Elapsed time
            ])
            .StartAsync(
            async ctx =>
            {
                {
                    var pomoTask = ctx.AddTask($"{tomato} [green]Pomodoro timer ticking[/]", maxValue: settings.PomodoriDurationSeconds ?? 25 * 60);
                    taskStack.Push(pomoTask);
                }

                while (!ctx.IsFinished)
                {
                    // Wait one second.
                    await Task.Delay(1000);

                    // Add elapsed time to currently running task
                    var topTask = taskStack.Peek();
                    var topTaskIsWork = topTask.Description.StartsWith(tomato);

                    // Is there a key to handle, before we delay?
                    var keyPressed = false;
                    if (interactive && AnsiConsole.Console.Input.IsKeyAvailable())
                    {
                        var key = Console.ReadKey(true);
                        keyPressed = true;
                        switch (key.Key)
                        {
                            case ConsoleKey.X:
                                topTask.StopTask();
                                totalWork += topTaskIsWork ? topTask.Value : 0;
                                topTask.Value = topTask.MaxValue;
                                continue;
                            case ConsoleKey.S:
                                var shortBreakTask = ctx.AddTask($"{shortBreak} [gold1]Short break[/]", maxValue: settings.ShortBreakDurationSeconds ?? 5 * 60);
                                topTask.StopTask();
                                totalWork += topTaskIsWork ? topTask.Value : 0;
                                topTask.Value = topTask.MaxValue;
                                taskStack.Push(shortBreakTask);
                                sw.Restart();
                                continue;
                            case ConsoleKey.L:
                                var longBreakTask = ctx.AddTask($"{longBreak} [orangered1]Long break[/]", maxValue: settings.LongBreakDurationSeconds ?? 15 * 60);
                                topTask.StopTask();
                                totalWork += topTaskIsWork ? topTask.Value : 0;
                                topTask.Value = topTask.MaxValue;
                                taskStack.Push(longBreakTask);
                                sw.Restart();
                                continue;
                            case ConsoleKey.R:
                                if (topTaskIsWork && !topTask.IsFinished)
                                {
                                    // Do nothing.
                                    continue;
                                }

                                var pomoTask = ctx.AddTask($"{tomato} [green]Pomodoro timer ticking[/]", maxValue: settings.PomodoriDurationSeconds ?? 25 * 60);
                                topTask.StopTask();
                                totalWork += topTaskIsWork ? topTask.Value : 0;
                                topTask.Value = topTask.MaxValue;
                                taskStack.Push(pomoTask);
                                sw.Restart();
                                continue;
                            case ConsoleKey.D:
                                topTask.StopTask();
                                totalWork += topTaskIsWork ? topTask.Value : 0;
                                topTask.Value = topTask.MaxValue;
                                continue;
                        }
                    }

                    topTask.Value = sw.Elapsed.TotalSeconds;
                    if (topTask.Value >= topTask.MaxValue && !keyPressed)
                    {
                        // Task naturally concluded.
                        topTask.StopTask();
                        totalWork += topTaskIsWork ? topTask.Value : 0;

                        // If the console is interactive, get attention.
                        if (interactive && !(settings.Quiet ?? false))
                        {
                            Console.Beep();
                        }

                        if (settings.AutoResume ?? false)
                        {
                            topTask.Value = topTask.MaxValue;
                            var pomoTask = topTaskIsWork
                                ? ctx.AddTask($"{tomato} [green]Pomodoro timer ticking (auto-renewed)[/]", maxValue: settings.PomodoriDurationSeconds ?? 25 * 60)
                                : ctx.AddTask($"{tomato} [green]Pomodoro timer ticking (auto-resumed)[/]", maxValue: settings.PomodoriDurationSeconds ?? 25 * 60);
                            taskStack.Push(pomoTask);
                            sw.Restart();
                        }
                    }
                }
            });

        var absolute = TimeSpan.FromSeconds(Math.Ceiling(totalWork));
        var relative = DateUtility.GetRelativeTimeString(absolute);
        AmbientErrorContext.Provider.LogDone($"Completed {relative} ({absolute.Days:#0:;;\\}{absolute.Hours:#0:;;\\}{absolute.Minutes:00:}{absolute.Seconds:00}) of work.");

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}
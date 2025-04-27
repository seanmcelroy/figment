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

using System.ComponentModel;
using Spectre.Console.Cli;

namespace jot.Commands.Pomodoro;

/// <summary>
/// The settings supplied to the <see cref="StartPomodoro"/> command.
/// </summary>
public class StartPomodoroSettings : CommandSettings
{
    /// <summary>
    /// Gets a value indicating whether to automatically resume working when the current timer expires.
    /// </summary>
    [Description("Automatically resume working when the current timer expires")]
    [CommandOption("-a|--auto")]
    required public bool? AutoResume { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether to provide verbose detail, if available, for any outputs.
    /// </summary>
    [Description("Do not beep when any timer events complete")]
    [CommandOption("-q|--quiet")]
    required public bool? Quiet { get; init; } = false;

    /// <summary>
    /// Gets the number of seconds for each pomodori work interval.
    /// </summary>
    [Description("Number of seconds for each work interval")]
    [CommandArgument(1, "[POMODORI_TIME]")]
    required public int? PomodoriDurationSeconds { get; init; } = 25 * 60;

    /// <summary>
    /// Gets the number of seconds for each short break.
    /// </summary>
    [Description("Number of seconds for each short break")]
    [CommandArgument(2, "[SHORT_BREAK_TIME]")]
    required public int? ShortBreakDurationSeconds { get; init; } = 5 * 60;

    /// <summary>
    /// Gets the number of seconds for each long break.
    /// </summary>
    [Description("Number of seconds for each long break")]
    [CommandArgument(3, "[LONG_BREAK_TIME]")]
    required public int? LongBreakDurationSeconds { get; init; } = 15 * 60;
}
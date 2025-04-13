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

using System.Runtime.InteropServices;
using Spectre.Console.Cli;

namespace jot.Commands;

/// <summary>
/// A command that executes asynchronously and that supports cancellation.
/// </summary>
public abstract class CancellableAsyncCommand : AsyncCommand
{
    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An integer indicating whether or not the command executed successfully.</returns>
    public abstract Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken);

    /// <inheritdoc/>
    public sealed override async Task<int> ExecuteAsync(CommandContext context)
    {
        using var cancellationSource = new CancellationTokenSource();

        using var sigInt = PosixSignalRegistration.Create(PosixSignal.SIGINT, OnSignal);
        using var sigQuit = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, OnSignal);
        using var sigTerm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnSignal);

        var cancellable = ExecuteAsync(context, cancellationSource.Token);
        return await cancellable;

        void OnSignal(PosixSignalContext context)
        {
            context.Cancel = true;
            cancellationSource.Cancel();
        }
    }
}

/// <summary>
/// An asynchronous command that supports cancellation.
/// </summary>
/// <typeparam name="TSettings">The srttings for the command when executed.</typeparam>
public abstract class CancellableAsyncCommand<TSettings> : AsyncCommand<TSettings>
    where TSettings : CommandSettings
{
    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The settings.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An integer indicating whether or not the command executed successfully.</returns>
    public abstract Task<int> ExecuteAsync(CommandContext context, TSettings settings, CancellationToken cancellationToken);

    /// <inheritdoc/>
    public sealed override async Task<int> ExecuteAsync(CommandContext context, TSettings settings)
    {
        using var cancellationSource = new CancellationTokenSource();

        using var sigInt = PosixSignalRegistration.Create(PosixSignal.SIGINT, OnSignal);
        using var sigQuit = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, OnSignal);
        using var sigTerm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnSignal);

        var cancellable = ExecuteAsync(context, settings, cancellationSource.Token);
        return await cancellable;

        void OnSignal(PosixSignalContext context)
        {
            context.Cancel = true;
            cancellationSource.Cancel();
        }
    }
}
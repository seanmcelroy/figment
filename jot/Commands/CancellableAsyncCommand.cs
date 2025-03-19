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
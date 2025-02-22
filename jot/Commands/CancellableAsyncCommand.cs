using System.Runtime.InteropServices;
using Spectre.Console.Cli;

namespace jot.Commands;

public abstract class CancellableAsyncCommand : AsyncCommand
{
    public abstract Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken);

    public sealed override async Task<int> ExecuteAsync(CommandContext context)
    {
        using var cancellationSource = new CancellationTokenSource();

        using var sigInt = PosixSignalRegistration.Create(PosixSignal.SIGINT, onSignal);
        using var sigQuit = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, onSignal);
        using var sigTerm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, onSignal);

        var cancellable = ExecuteAsync(context, cancellationSource.Token);
        return await cancellable;

        void onSignal(PosixSignalContext context)
        {
            context.Cancel = true;
            cancellationSource.Cancel();
        }
    }
}

public abstract class CancellableAsyncCommand<TSettings> : AsyncCommand<TSettings> where TSettings : CommandSettings
{
    public abstract Task<int> ExecuteAsync(CommandContext context, TSettings settings, CancellationToken cancellation);

    public sealed override async Task<int> ExecuteAsync(CommandContext context, TSettings settings)
    {
        using var cancellationSource = new CancellationTokenSource();

        using var sigInt = PosixSignalRegistration.Create(PosixSignal.SIGINT, onSignal);
        using var sigQuit = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, onSignal);
        using var sigTerm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, onSignal);

        var cancellable = ExecuteAsync(context, settings, cancellationSource.Token);
        return await cancellable;

        void onSignal(PosixSignalContext context)
        {
            context.Cancel = true;
            cancellationSource.Cancel();
        }
    }
}
using Spectre.Console.Cli;

namespace jot.Commands;

public class QuitCommand : CancellableAsyncCommand
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        Environment.Exit((int)Globals.GLOBAL_ERROR_CODES.SUCCESS);
        return await Task.FromResult((int)Globals.GLOBAL_ERROR_CODES.SUCCESS);
    }
}
using Figment;
using Spectre.Console.Cli;

namespace jot.Commands;

public class ListThingsCommand : CancellableAsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        await foreach (var thing in Thing.GetAll(cancellationToken))
        {
            Console.WriteLine(thing.Name);
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}
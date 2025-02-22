using Figment;
using Spectre.Console.Cli;

namespace jot.Commands;

public class ListSchemasCommand : CancellableAsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        await foreach (var schema in Schema.GetAll(cancellationToken))
        {
            Console.WriteLine(schema.Name);
        }

        return (int)Globals.GLOBAL_ERROR_CODES.SUCCESS;
    }
}
namespace Figment.Common.Calculations.Parsing;

/// <summary>
/// A type of node that executes a bulit-in function registered by <see cref="FunctionRegistry"/>.
/// </summary>
/// <param name="FunctionName">The name of the function, such as UPPER.</param>
/// <param name="Arguments">The arguments to supply to the function.</param>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public class FunctionNode(string FunctionName, List<NodeBase> Arguments) : NodeBase
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    /// <inheritdoc/>
    public override ExpressionResult Evaluate(EvaluationContext context)
    {
        var evalArgs = Arguments.ConvertAll(a => a.Evaluate(context));
        if (evalArgs.Any(a => !a.IsSuccess))
        {
            return evalArgs.First(a => !a.IsSuccess);
        }

        var nodeArgs = evalArgs.ConvertAll<NodeBase>(a =>
        {
            if (a.Result == null)
            {
                return LiteralNode.NULL;
            }

            if (a.Result is LiteralNode l)
            {
                return l;
            }

            return new LiteralNode(a.Result);
        }).ToArray();

        return FunctionRegistry.Invoke(FunctionName, context, nodeArgs);
    }
}

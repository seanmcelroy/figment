namespace Figment.Common.Calculations.Parsing;

#pragma warning disable SA1600 // Elements should be documented
public class FunctionNode(string FunctionName, List<NodeBase> Arguments) : NodeBase
#pragma warning restore SA1600 // Elements should be documented
{
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

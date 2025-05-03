namespace Figment.Common.Calculations.Parsing;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public class ConcatNode(NodeBase Left, NodeBase Right) : NodeBase
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    /// <inheritdoc/>
    public override ExpressionResult Evaluate(EvaluationContext context)
    {
        // Left
        var le = Left.Evaluate(context);
        if (!le.IsSuccess)
        {
            return le;
        }

        // Right
        var re = Right.Evaluate(context);
        if (!re.IsSuccess)
        {
            return re;
        }

        var lhand = le.Result?.ToString() ?? string.Empty;
        var rhand = re.Result?.ToString() ?? string.Empty;
        return ExpressionResult.Success(new LiteralNode(lhand + rhand));
    }
}
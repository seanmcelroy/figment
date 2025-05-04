namespace Figment.Common.Calculations.Parsing;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public class LogicalEqualsNode(NodeBase Left, NodeBase Right) : NodeBase
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    /// <inheritdoc/>
    public override ExpressionResult Evaluate(EvaluationContext context)
    {
        // Left
        var le = Left.Evaluate(context);
        if (!le.IsSuccess)
        {
            return le; // Propogate errors up
        }

        // Right
        var re = Right.Evaluate(context);
        if (!re.IsSuccess)
        {
            return re; // Propogate errors up
        }

        return le.Equals(re) ? ExpressionResult.TRUE : ExpressionResult.FALSE;
    }
}
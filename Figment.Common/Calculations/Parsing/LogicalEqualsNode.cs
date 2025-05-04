namespace Figment.Common.Calculations.Parsing;

/// <summary>
/// A node that compares two other nodes and provides either <see cref="ExpressionResult.TRUE"/>
/// or <see cref="ExpressionResult.FALSE"/> depending on whether they are equal.
/// </summary>
/// <param name="Left">The lefthand side of the equality equation.</param>
/// <param name="Right">The righthand side of the equality equation.</param>
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
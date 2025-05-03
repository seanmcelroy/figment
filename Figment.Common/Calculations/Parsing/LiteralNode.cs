namespace Figment.Common.Calculations.Parsing;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public class LiteralNode(object Value) : NodeBase
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    /// <inheritdoc/>
    public override ExpressionResult Evaluate(EvaluationContext context) =>
        ExpressionResult.Success(Value);

    /// <inheritdoc/>
    public override string ToString() => Value?.ToString() ?? string.Empty;
}
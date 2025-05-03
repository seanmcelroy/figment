namespace Figment.Common.Calculations.Parsing;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public class FieldRefNode(string FieldName) : NodeBase
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    /// <inheritdoc/>
    public override ExpressionResult Evaluate(EvaluationContext context) =>
        context.GetField(FieldName);
}
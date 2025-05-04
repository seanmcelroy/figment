namespace Figment.Common.Calculations.Parsing;

/// <summary>
/// A type of node that retrieves the value of a named property from the <see cref="EvaluationContext"/>.
/// </summary>
/// <param name="FieldName">The name of the field to retrieve from the <see cref="EvaluationContext"/>.</param>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public class FieldRefNode(string FieldName) : NodeBase
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    /// <inheritdoc/>
    public override ExpressionResult Evaluate(EvaluationContext context) =>
        context.GetField(FieldName);
}
namespace Figment.Common.Calculations.Parsing;

/// <summary>
/// An abstract base class from which all nodes in the abstract syntax tree derive.
/// </summary>
public abstract class NodeBase
{
    /// <summary>
    /// Evaluates a node to produce a result from the expression given the supplied <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The context given to the node to generate its result.</param>
    /// <returns>An expression result indicating whether the evaluation was functionally successful, and if so, the result.</returns>
    public abstract ExpressionResult Evaluate(EvaluationContext context);
}
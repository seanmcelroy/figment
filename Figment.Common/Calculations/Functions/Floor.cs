using Figment.Common.Calculations.Parsing;

namespace Figment.Common.Calculations.Functions;

/// <summary>
/// The FLOOR function returns the largest integral value less than or equal to the
/// specified double-precision floating-point number.
/// </summary>
public class Floor : FunctionBase
{
    /// <summary>
    /// The identifier of this function.
    /// </summary>
    public const string IDENTIFIER = "FLOOR";

    /// <inheritdoc/>
    public override string Identifier => IDENTIFIER;

    /// <inheritdoc/>
    public override ExpressionResult Evaluate(EvaluationContext context, NodeBase[] arguments)
    {
        if (arguments.Length != 1)
        {
            return ExpressionResult.Error(CalculationErrorType.FormulaParse, "FLOOR() takes one parameter");
        }

        var argumentResult = arguments[0].Evaluate(context);
        if (!argumentResult.IsSuccess)
        {
            return argumentResult;
        }

        if (!argumentResult.TryConvertDouble(out double doubleResult))
        {
            return ExpressionResult.Error(CalculationErrorType.BadValue, $"Unable to convert '{argumentResult.Result}' into a number");
        }

        return ExpressionResult.Success(Math.Floor(doubleResult));
    }
}
using Figment.Common.Calculations.Functions;

namespace Figment.Common.Calculations.Parsing;

public static class FunctionRegistry
{
    private static readonly Dictionary<string, Func<EvaluationContext, NodeBase[], ExpressionResult>> Functions = new()
    {
        {
            "LOWER", static (ctx, args) => new Lower().Evaluate(ctx, args)
        },
        {
            "TRIM", static (ctx, args) => new Trim().Evaluate(ctx, args)
        },
        {
            "UPPER", static (ctx, args) => new Upper().Evaluate(ctx, args)
        },/*
        {
            "IF", static (ctx, args) =>
            ExpressionResult.Success(Convert.ToBoolean(args[0]) ? args[1] : args[2])
        },*/
    };

    public static ExpressionResult Invoke(string name, EvaluationContext context, NodeBase[] args)
    {
        if (!Functions.TryGetValue(name.ToUpperInvariant(), out var func))
        {
            throw new InvalidOperationException($"Unknown function '{name}'");
        }

        return func(context, args);
    }
}
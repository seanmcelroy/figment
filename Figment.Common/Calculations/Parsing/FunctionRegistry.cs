using Figment.Common.Calculations.Functions;

namespace Figment.Common.Calculations.Parsing;

public static class FunctionRegistry
{
    private static readonly Dictionary<string, Func<EvaluationContext, NodeBase[], ExpressionResult>> Functions = new()
    {
        {
            DateDiff.IDENTIFIER, static (ctx, args) => new DateDiff().Evaluate(ctx, args)
        },
        {
            False.IDENTIFIER, static (ctx, args) => new False().Evaluate(ctx, args)
        },
        {
            Floor.IDENTIFIER, static (ctx, args) => new Floor().Evaluate(ctx, args)
        },
        {
            If.IDENTIFIER, static (ctx, args) => new If().Evaluate(ctx, args)
        },
        {
            Len.IDENTIFIER, static (ctx, args) => new Len().Evaluate(ctx, args)
        },
        {
            Lower.IDENTIFIER, static (ctx, args) => new Lower().Evaluate(ctx, args)
        },
        {
            Now.IDENTIFIER, static (ctx, args) => new Now().Evaluate(ctx, args)
        },
        {
            Null.IDENTIFIER, static (ctx, args) => new Null().Evaluate(ctx, args)
        },
        {
            Today.IDENTIFIER, static (ctx, args) => new Today().Evaluate(ctx, args)
        },
        {
            Trim.IDENTIFIER, static (ctx, args) => new Trim().Evaluate(ctx, args)
        },
        {
            True.IDENTIFIER, static (ctx, args) => new True().Evaluate(ctx, args)
        },
        {
            Upper.IDENTIFIER, static (ctx, args) => new Upper().Evaluate(ctx, args)
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
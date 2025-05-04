namespace Figment.Common.Calculations.Parsing;

public readonly record struct EvaluationContext
{
    public static readonly EvaluationContext EMPTY = new();

    private Dictionary<string, ExpressionResult> RowData { get; init; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="EvaluationContext"/> class.
    /// </summary>
    public EvaluationContext()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvaluationContext"/> class.
    /// </summary>
    /// <param name="thing">A <see cref="Thing"/> object that should be injected into the context, making its properties available.</param>
    public EvaluationContext(Thing thing)
    {
        // Add set properties.
        foreach (var prop in thing.GetProperties(CancellationToken.None).ToBlockingEnumerable())
        {
            RowData.Add(prop.SimpleDisplayName, ExpressionResult.Success(prop.Value));
        }

        // Add unset properties.
        foreach (var usp in thing.GetUnsetProperties(CancellationToken.None).Result)
        {
            RowData.Add(usp.SimpleDisplayName, ExpressionResult.Error(CalculationErrorType.BadValue, "Unset property"));
        }

        // Populate meta properties always override.
        RowData["Name"] = ExpressionResult.Success(thing.Name);
        RowData["CreatedOn"] = ExpressionResult.Success(thing.CreatedOn);
        RowData["LastAccessed"] = ExpressionResult.Success(thing.LastAccessed);
        RowData["LastModified"] = ExpressionResult.Success(thing.LastModified);
    }

    public ExpressionResult GetField(string name)
    {
        if (!RowData.TryGetValue(name, out var val))
        {
            return ExpressionResult.Error(CalculationErrorType.FormulaParse, $"Field '{name}' not found");
        }

        return val;
    }
}
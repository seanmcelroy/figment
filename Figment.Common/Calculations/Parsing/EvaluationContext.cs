namespace Figment.Common.Calculations.Parsing;

public readonly record struct EvaluationContext
{
    /// <summary>
    /// A default context that contains no additional data.
    /// </summary>
    public static readonly EvaluationContext EMPTY = new();

    /// <summary>
    /// Gets the context data provided for calls to <see cref="ExpressionParser.Parse(string)"/>.
    /// </summary>
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

    /// <summary>
    /// Retrieves a value from the context map.
    /// </summary>
    /// <param name="name">Name of the value to retrieve from the context.</param>
    /// <returns>The value of the context, or an error if not found.</returns>
    public ExpressionResult GetField(string name)
    {
        if (!RowData.TryGetValue(name, out var val))
        {
            return ExpressionResult.Error(CalculationErrorType.FormulaParse, $"Field '{name}' not found");
        }

        return val;
    }
}
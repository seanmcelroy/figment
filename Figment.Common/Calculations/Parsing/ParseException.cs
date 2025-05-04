namespace Figment.Common.Calculations.Parsing;

/// <summary>
/// An exception thrown if the <see cref="ExpressionParser"/> cannot parse an expression.
/// </summary>
public class ParseException : Exception
{
    /// <summary>
    /// Gets the index of the expression where the exception occurred during parsing.
    /// </summary>
    public int Position { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ParseException"/> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="position">The index of the expression where the exception occurred during parsing.</param>
    public ParseException(string message, int position)
        : base(message)
    {
        Position = position;
    }
}
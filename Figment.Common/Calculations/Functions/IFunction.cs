namespace Figment.Common.Calculations.Functions;

public interface IFunction
{
    public CalculationResult Evaluate(CalculationResult[] parameters);
}
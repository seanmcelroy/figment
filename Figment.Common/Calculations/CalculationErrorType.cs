namespace Figment.Common.Calculations;

public enum CalculationErrorType {
    Success = 0,
    FormulaParse = 1,
    NotANumber = 2,
    DivisionByZero = 3,
    Recursion = 4,
    BadValue = 5,
}
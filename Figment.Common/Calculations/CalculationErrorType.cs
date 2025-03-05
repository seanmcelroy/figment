namespace Figment.Common.Calculations;

public enum CalculationErrorType
{
    Success = 0,
    FormulaParse = 1, // #ERR
    NotANumber = 2, // #NAN
    DivisionByZero = 3, // #DIV
    Recursion = 4,
    BadValue = 5, // #VALUE
    InternalError = 6
}
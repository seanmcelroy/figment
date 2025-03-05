namespace Figment.Common.Errors;

public static class AmbientErrorContext
{
    private static readonly AsyncLocal<IErrorProvider> _ErrorProvider = new();

    public static IErrorProvider Provider
    {
        get => _ErrorProvider.Value ?? new DefaultConsoleErrorProvider();
        set => _ErrorProvider.Value = value;
    }
}
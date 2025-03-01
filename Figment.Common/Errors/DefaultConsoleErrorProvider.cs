
namespace Figment.Common.Errors;

public class DefaultConsoleErrorProvider : IErrorProvider
{
    public void LogError(FormattableString formattableString) => Console.Error.WriteLine(formattableString);

    public void LogError(string message) => Console.Error.WriteLine(message);

    public void LogException(Exception ex, FormattableString formattableString)
    {
        Console.Error.WriteLine(formattableString);
        Console.Error.WriteLine(ex);
    }

    public void LogException(Exception ex, string message)
    {
        Console.Error.WriteLine(message);
        Console.Error.WriteLine(ex);
    }

    public void LogInfo(FormattableString formattableString) => Console.Error.WriteLine(formattableString);

    public void LogInfo(string message) => Console.Error.WriteLine(message);

    public void LogWarning(FormattableString formattableString) => Console.Error.WriteLine(formattableString);

    public void LogWarning(string message) => Console.Error.WriteLine(message);
}
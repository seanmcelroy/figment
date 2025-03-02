namespace Figment.Common.Errors;

public interface IErrorProvider
{
    public void LogException(Exception ex, FormattableString formattableString);
    public void LogError(FormattableString formattableString);
    public void LogWarning(FormattableString formattableString);
    public void LogInfo(FormattableString formattableString);
    public void LogDone(FormattableString formattableString);

    public void LogException(Exception ex, string message);
    public void LogError(string message);
    public void LogWarning(string message);
    public void LogInfo(string message);
    public void LogDone(string message);
}
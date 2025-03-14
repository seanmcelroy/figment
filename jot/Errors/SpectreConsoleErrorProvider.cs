using Figment.Common.Errors;
using Spectre.Console;

namespace jot.Errors;

public class SpectreConsoleErrorProvider : IErrorProvider
{
    public void LogException(Exception ex, FormattableString formattableString)
    {
        AnsiConsole.MarkupLine($"[red]Error[/]: {formattableString}\r\n");
        AnsiConsole.WriteException(ex);
    }

    public void LogError(FormattableString formattableString) => AnsiConsole.MarkupLine($"[red]Error[/]: {formattableString}\r\n");

    public void LogWarning(FormattableString formattableString) => AnsiConsole.MarkupLine($"[yellow]Warning[/]: {formattableString}\r\n");

    public void LogInfo(FormattableString formattableString) => AnsiConsole.MarkupLine($"[blue]INFO[/]: {formattableString}\r\n");

    public void LogException(Exception ex, string message)
    {
        AnsiConsole.MarkupInterpolated($"[red]ERROR[/]: {message}\r\n");
        AnsiConsole.WriteException(ex);
    }

    public void LogError(string message) => AnsiConsole.MarkupInterpolated($"[red]Error[/]: {message}\r\n");

    public void LogWarning(string message) => AnsiConsole.MarkupInterpolated($"[yellow]Warning[/]: {message}\r\n");

    public void LogInfo(string message) => AnsiConsole.MarkupInterpolated($"[blue]INFO[/]: {message}\r\n");

    public void LogDone(FormattableString formattableString) => AnsiConsole.MarkupLine($"[green]Done[/]: {formattableString}\r\n");

    public void LogDone(string message) => AnsiConsole.MarkupLineInterpolated($"[green]Done[/]: {message}\r\n");

    public void LogProgress(FormattableString formattableString) => AnsiConsole.MarkupLineInterpolated($"[blue]Working...[/] {formattableString}");

    public void LogProgress(string message) => AnsiConsole.MarkupLineInterpolated($"[blue]Working...[/] {message}");

    public void LogDebug(FormattableString formattableString)=> AnsiConsole.MarkupLineInterpolated($"[orange1]DEBUG[/]: {formattableString}");

    public void LogDebug(string message) => AnsiConsole.MarkupInterpolated($"[orange1]DEBUG[/]: {message}\r\n");
}
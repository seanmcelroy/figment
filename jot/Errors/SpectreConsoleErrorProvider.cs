using Figment.Common.Errors;
using Spectre.Console;

namespace jot.Errors;

public class SpectreConsoleErrorProvider : IErrorProvider
{
    public void LogException(Exception ex, FormattableString formattableString)
    {
        AnsiConsole.MarkupLine($"[red]ERROR[/]: {formattableString}\r\n");
        AnsiConsole.WriteException(ex);
    }

    public void LogError(FormattableString formattableString) => AnsiConsole.MarkupLine($"[red]ERROR[/]: {formattableString}\r\n");

    public void LogWarning(FormattableString formattableString) => AnsiConsole.MarkupLine($"[yellow]WARNING[/]: {formattableString}\r\n");

    public void LogInfo(FormattableString formattableString) => AnsiConsole.MarkupLine($"[blue]INFO[/]: {formattableString}\r\n");

    public void LogException(Exception ex, string message)
    {
        AnsiConsole.MarkupInterpolated($"[red]ERROR[/]: {message}\r\n");
        AnsiConsole.WriteException(ex);
    }

    public void LogError(string message) => AnsiConsole.MarkupInterpolated($"[red]ERROR[/]: {message}\r\n");

    public void LogWarning(string message) => AnsiConsole.MarkupInterpolated($"[yellow]WARNING[/]: {message}\r\n");

    public void LogInfo(string message) => AnsiConsole.MarkupInterpolated($"[blue]INFO[/]: {message}\r\n");

    public void LogDone(FormattableString formattableString) => AnsiConsole.MarkupLine($"[green]DONE[/]: {formattableString}\r\n");

    public void LogDone(string message) => AnsiConsole.MarkupInterpolated($"[green]DONE[/]: {message}\r\n");

    public void LogProgress(FormattableString formattableString) => AnsiConsole.MarkupLineInterpolated($"[blue]Working...[/] {formattableString}");

    public void LogProgress(string message) => AnsiConsole.MarkupLineInterpolated($"[blue]Working...[/] {message}");
}
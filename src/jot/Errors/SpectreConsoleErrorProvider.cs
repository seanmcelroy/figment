using Figment.Common.Errors;
using Spectre.Console;

namespace jot.Errors;

/// <summary>
/// An error provider that outputs errors to a <see cref="AnsiConsole"/>.
/// </summary>
public class SpectreConsoleErrorProvider : IErrorProvider
{
    /// <inheritdoc/>
    public void LogException(Exception ex, FormattableString formattableString)
    {
        AnsiConsole.MarkupLine($"[red]Error[/]: {formattableString}\r\n");
        AnsiConsole.WriteException(ex);
    }

    /// <inheritdoc/>
    public void LogError(FormattableString formattableString) => AnsiConsole.MarkupLine($"[red]Error[/]: {formattableString}\r\n");

    /// <inheritdoc/>
    public void LogWarning(FormattableString formattableString) => AnsiConsole.MarkupLine($"[yellow]Warning[/]: {formattableString}\r\n");

    /// <inheritdoc/>
    public void LogInfo(FormattableString formattableString) => AnsiConsole.MarkupLine($"[blue]INFO[/]: {formattableString}\r\n");

    /// <inheritdoc/>
    public void LogException(Exception ex, string message)
    {
        AnsiConsole.MarkupInterpolated($"[red]ERROR[/]: {message}\r\n");
        AnsiConsole.WriteException(ex);
    }

    /// <inheritdoc/>
    public void LogError(string message) => AnsiConsole.MarkupInterpolated($"[red]Error[/]: {message}\r\n");

    /// <inheritdoc/>
    public void LogWarning(string message) => AnsiConsole.MarkupInterpolated($"[yellow]Warning[/]: {message}\r\n");

    /// <inheritdoc/>
    public void LogInfo(string message) => AnsiConsole.MarkupInterpolated($"[blue]INFO[/]: {message}\r\n");

    /// <inheritdoc/>
    public void LogDone(FormattableString formattableString) => AnsiConsole.MarkupLine($"[green]Done[/]: {formattableString}\r\n");

    /// <inheritdoc/>
    public void LogDone(string message) => AnsiConsole.MarkupLineInterpolated($"[green]Done[/]: {message}\r\n");

    /// <inheritdoc/>
    public void LogProgress(FormattableString formattableString) => AnsiConsole.MarkupLineInterpolated($"[blue]Working...[/] {formattableString}");

    /// <inheritdoc/>
    public void LogProgress(string message) => AnsiConsole.MarkupLineInterpolated($"[blue]Working...[/] {message}");

    /// <inheritdoc/>
    public void LogDebug(FormattableString formattableString) => AnsiConsole.MarkupLineInterpolated($"[orange1]DEBUG[/]: {formattableString}");

    /// <inheritdoc/>
    public void LogDebug(string message) => AnsiConsole.MarkupInterpolated($"[orange1]DEBUG[/]: {message}\r\n");
}
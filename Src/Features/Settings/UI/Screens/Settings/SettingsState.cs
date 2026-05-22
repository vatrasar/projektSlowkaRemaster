namespace ProjektSlowkaRemasterd.Src.Features.Settings.UI.Screens.Settings;

/// <summary>
/// Immutable state record for the Settings screen.
/// </summary>
public record SettingsState
{
    public string BackupPath { get; init; } = string.Empty;
    public bool IsSaving { get; init; } = false;
    public bool IsBackingUp { get; init; } = false;
    public string StatusMessage { get; init; } = string.Empty;
}

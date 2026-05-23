using System;
using System.IO;
using System.Reactive;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ReactiveUI;
using Splat;
using ProjektSlowkaRemasterd.Src.Core.Mvvm;
using ProjektSlowkaRemasterd.Src.Core.Config;

namespace ProjektSlowkaRemasterd.Src.Features.Settings.UI.Screens.Settings;

/// <summary>
/// ViewModel for the Settings screen.
/// Allows setting the backup folder and executing a backup of the DB and media files.
/// </summary>
public class SettingsViewModel : ViewModelBase<SettingsState>, IRoutableViewModel
{
    public string? UrlPathSegment => "settings";
    public IScreen HostScreen { get; }

    private readonly AppConfig _config;

    public ReactiveCommand<string, Unit> SaveBackupPathCommand { get; }
    public ReactiveCommand<Unit, Unit> RunBackupCommand { get; }

    public SettingsViewModel(IScreen hostScreen) 
        : this(hostScreen, Locator.Current.GetService<IOptions<AppConfig>>()!.Value)
    {
    }

    public SettingsViewModel(IScreen hostScreen, AppConfig config) 
        : base(new SettingsState { BackupPath = config.BackupDirectoryPath })
    {
        HostScreen = hostScreen;
        _config = config ?? throw new ArgumentNullException(nameof(config));

        SaveBackupPathCommand = ReactiveCommand.CreateFromTask<string>(SaveBackupPathAsync);
        RunBackupCommand = ReactiveCommand.CreateFromTask(RunBackupAsync);
    }

    private async Task SaveBackupPathAsync(string path)
    {
        UpdateState(s => s with { IsSaving = true, StatusMessage = "Saving path..." });
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var settingsPath = Path.Combine(baseDir, "appsettings.json");

            _config.BackupDirectoryPath = path;

            var configObject = new { AppConfig = _config };
            var json = JsonSerializer.Serialize(configObject, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(settingsPath, json);

            UpdateState(s => s with { BackupPath = path, IsSaving = false, StatusMessage = "Path saved successfully!" });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsSaving = false, StatusMessage = $"Error saving path: {ex.Message}" });
        }
    }

    private async Task RunBackupAsync()
    {
        var backupPath = State.BackupPath;
        if (string.IsNullOrWhiteSpace(backupPath))
        {
            UpdateState(s => s with { StatusMessage = "Please set and save a valid backup directory path first." });
            return;
        }

        UpdateState(s => s with { IsBackingUp = true, StatusMessage = "Executing backup..." });

        try
        {
            await Task.Run(() =>
            {
                if (!Directory.Exists(backupPath))
                {
                    Directory.CreateDirectory(backupPath);
                }

                // 1. Backup SQLite database
                var dbSourcePath = _config.ResolvedDatabasePath;
                if (File.Exists(dbSourcePath))
                {
                    var dbDestPath = Path.Combine(backupPath, Path.GetFileName(dbSourcePath));
                    File.Copy(dbSourcePath, dbDestPath, overwrite: true);
                }

                // 2. Backup media files
                var mediaSourcePath = _config.ResolvedMediaDirectoryPath;
                if (Directory.Exists(mediaSourcePath))
                {
                    var mediaDestPath = Path.Combine(backupPath, "media");
                    if (!Directory.Exists(mediaDestPath))
                    {
                        Directory.CreateDirectory(mediaDestPath);
                    }

                    foreach (var file in Directory.GetFiles(mediaSourcePath))
                    {
                        var destFile = Path.Combine(mediaDestPath, Path.GetFileName(file));
                        File.Copy(file, destFile, overwrite: true);
                    }
                }
            });

            UpdateState(s => s with { IsBackingUp = false, StatusMessage = $"Backup completed successfully at {DateTime.Now:HH:mm:ss}!" });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsBackingUp = false, StatusMessage = $"Backup failed: {ex.Message}" });
        }
    }
}

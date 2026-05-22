using ReactiveUI;
using Avalonia.ReactiveUI;

using System.Reactive.Disposables;
using ProjektSlowkaRemasterd.Src.Features.Settings.UI.Screens.Settings;

namespace ProjektSlowkaRemasterd.Src.Features.Settings.UI.Screens.Settings;

/// <summary>
/// Screen: SettingsView
/// Purpose: Displays configuration settings and manual backup execution triggers.
/// Available Functionalities: Configure backup directory, execute database and media backups.
/// Key UI Elements: BackupPathTextBox, SavePathButton, BackupNowButton, StatusTextBlock.
/// Navigation:
///   - Navigate From: HostView sidebar navigation
///   - Navigate To: Remains on settings
/// </summary>
public partial class SettingsView : ReactiveUserControl<SettingsViewModel>
{
    public SettingsView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            // Initial path display
            this.OneWayBind(ViewModel,
                vm => vm.State.BackupPath,
                v => v.BackupPathTextBox.Text);

            // Bind status messages
            this.OneWayBind(ViewModel,
                vm => vm.State.StatusMessage,
                v => v.StatusTextBlock.Text);

            // Command binding to Save path (passing TextBox text as parameter)
            this.BindCommand(ViewModel,
                vm => vm.SaveBackupPathCommand,
                v => v.SavePathButton,
                this.WhenAnyValue(v => v.BackupPathTextBox.Text));

            // Command binding to run backup
            this.BindCommand(ViewModel,
                vm => vm.RunBackupCommand,
                v => v.BackupNowButton);

            // Disable buttons during actions
            this.OneWayBind(ViewModel,
                vm => vm.State.IsSaving,
                v => v.SavePathButton.IsEnabled,
                saving => !saving);

            this.OneWayBind(ViewModel,
                vm => vm.State.IsBackingUp,
                v => v.BackupNowButton.IsEnabled,
                backingUp => !backingUp);
        });
    }
}

using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ReactiveUI;
using Avalonia.ReactiveUI;
using CategoryModel = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Category;
using TopicModel = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Topic;
using SectionModel = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Section;
using ProjektSlowkaRemasterd.Src.Features.Question.Resources;

namespace ProjektSlowkaRemasterd.Src.Features.Question.UI.Screens.BulkImport;

/// <summary>
/// Screen: BulkImportView
/// Purpose: UI for importing multiple questions from a text file at once.
/// Available Functionalities: Load file, paste questions text, choose defaults, transactional import.
/// Key UI Elements: TextBox, buttons, ComboBoxes, status text.
/// Navigation:
///   - Navigate From: ManageView (via Bulk Import button)
///   - Navigate To: ManageView (via Back button / Back navigation)
/// </summary>
public partial class BulkImportView : ReactiveUserControl<BulkImportViewModel>
{
    public BulkImportView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            // Bind ComboBoxes Sources
            this.OneWayBind(ViewModel,
                vm => vm.State.Categories,
                v => v.CategoryComboBox.ItemsSource);

            this.OneWayBind(ViewModel,
                vm => vm.State.Topics,
                v => v.TopicComboBox.ItemsSource);

            this.OneWayBind(ViewModel,
                vm => vm.State.Sections,
                v => v.SectionComboBox.ItemsSource);

            // Bind Category selection
            this.WhenAnyValue(x => x.CategoryComboBox.SelectedItem)
                .Where(selected => selected is CategoryModel)
                .Select(selected => (CategoryModel)selected!)
                .Subscribe(Observer.Create<CategoryModel>(category =>
                {
                    _ = ViewModel?.SetSelectedCategory(category);
                }))
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.ViewModel!.State.SelectedCategory)
                .Subscribe(Observer.Create<CategoryModel?>(category =>
                {
                    CategoryComboBox.SelectedItem = category;
                }))
                .DisposeWith(disposables);

            // Bind Topic selection
            this.WhenAnyValue(x => x.TopicComboBox.SelectedItem)
                .Subscribe(Observer.Create<object?>(item =>
                {
                    var topic = item as TopicModel;
                    _ = ViewModel?.SetSelectedTopic(topic);
                }))
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.ViewModel!.State.SelectedTopic)
                .Subscribe(Observer.Create<TopicModel?>(topic =>
                {
                    TopicComboBox.SelectedItem = topic;
                }))
                .DisposeWith(disposables);

            // Bind Section selection
            this.WhenAnyValue(x => x.SectionComboBox.SelectedItem)
                .Subscribe(Observer.Create<object?>(item =>
                {
                    var section = item as SectionModel;
                    ViewModel?.SetSelectedSection(section);
                }))
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.ViewModel!.State.SelectedSection)
                .Subscribe(Observer.Create<SectionModel?>(section =>
                {
                    SectionComboBox.SelectedItem = section;
                }))
                .DisposeWith(disposables);

            // Bind TextBox input
            this.WhenAnyValue(x => x.ImportTextBox.Text)
                .Subscribe(Observer.Create<string?>(text => ViewModel?.SetImportText(text ?? string.Empty)))
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.ViewModel!.State.ImportText)
                .Subscribe(Observer.Create<string>(text =>
                {
                    if (ImportTextBox.Text != text)
                    {
                        ImportTextBox.Text = text;
                    }
                }))
                .DisposeWith(disposables);

            // Bind file path text
            this.OneWayBind(ViewModel,
                vm => vm.State.SelectedFilePath,
                v => v.FilePathTextBlock.Text,
                path => string.IsNullOrEmpty(path) ? QuestionStrings.NoFileChosen : path);

            // Bind error / success status text
            this.OneWayBind(ViewModel,
                vm => vm.State.SuccessMessage,
                v => v.SuccessTextBlock.Text);

            this.OneWayBind(ViewModel,
                vm => vm.State.SuccessMessage,
                v => v.SuccessTextBlock.IsVisible,
                msg => !string.IsNullOrEmpty(msg));

            this.OneWayBind(ViewModel,
                vm => vm.State.ErrorMessage,
                v => v.ErrorTextBlock.Text);

            this.OneWayBind(ViewModel,
                vm => vm.State.ErrorMessage,
                v => v.ErrorTextBlock.IsVisible,
                msg => !string.IsNullOrEmpty(msg));

            // Bind Buttons to Commands
            this.BindCommand(ViewModel,
                vm => vm.ChooseFileCommand,
                v => v.ChooseFileButton);

            this.BindCommand(ViewModel,
                vm => vm.ImportCommand,
                v => v.ImportButton);

            this.BindCommand(ViewModel,
                vm => vm.GoBackCommand,
                v => v.BackButton);

            // Register File Picker interaction handler
            ViewModel!.ShowFilePickerInteraction.RegisterHandler(async interaction =>
            {
                var result = await ShowFilePickerAsync();
                interaction.SetOutput(result);
            }).DisposeWith(disposables);
        });
    }

    private async Task<(string filename, string content)?> ShowFilePickerAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider == null)
        {
            return null;
        }

        var options = new FilePickerOpenOptions
        {
            Title = "Select Questions File",
            AllowMultiple = false
        };

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
        if (files.Count == 0)
        {
            return null;
        }

        var file = files[0];
        using var stream = await file.OpenReadAsync();
        using var reader = new System.IO.StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        return (file.Name, content);
    }
}

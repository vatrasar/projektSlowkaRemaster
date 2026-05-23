using System;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Data.Converters;
using Microsoft.Extensions.Options;
using ReactiveUI;
using Splat;
using Avalonia.ReactiveUI;

using ProjektSlowkaRemasterd.Src.Core.Config;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;
using ProjektSlowkaRemasterd.Src.Core.Domain.Enums;
using ProjektSlowkaRemasterd.Src.Features.Question.UI.Screens.QuestionEditor;

namespace ProjektSlowkaRemasterd.Src.Features.Question.UI.Screens.QuestionEditor;

using Category = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Category;
using Question = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Question;


/// <summary>
/// Screen: QuestionEditorView
/// Purpose: UI for creating and editing questions and notes with category/topic/section structure, media attachments, and preview functionality.
/// Available Functionalities: Select category, select topic, select/create section, add images (file or clipboard), edit note flag, edit problematic flag, group ID field, preview last added card.
/// Key UI Elements: CategoryComboBox, TopicComboBox, SectionComboBox, CustomSectionTextBox, QuestionTextBox, AnswerTextBox, SaveButton, BackButton, EditLastAddedButton.
/// Navigation:
///   - Navigate From: ManageView, HomeView
///   - Navigate To: Back to calling screen
/// </summary>
public partial class QuestionEditorView : ReactiveUserControl<QuestionEditorViewModel>
{
    public QuestionEditorView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            // Title Text Binding based on QuestionId
            this.WhenAnyValue(v => v.ViewModel!.State.QuestionId)
                .Select(id => id.HasValue && id.Value > 0 ? "Edit Question" : "Add New Question")
                .Subscribe(Observer.Create<string>(text => TitleTextBlock.Text = text))
                .DisposeWith(disposables);

            // Set up ComboBox ItemsSource bindings
            this.OneWayBind(ViewModel,
                vm => vm.State.Categories,
                v => v.CategoryComboBox.ItemsSource);

            this.OneWayBind(ViewModel,
                vm => vm.State.Topics,
                v => v.TopicComboBox.ItemsSource);

            this.OneWayBind(ViewModel,
                vm => vm.State.Sections,
                v => v.SectionComboBox.ItemsSource);

            // Set up ComboBox Selection bindings (State to UI)
            this.OneWayBind(ViewModel,
                vm => vm.State.SelectedCategory,
                v => v.CategoryComboBox.SelectedItem);

            this.OneWayBind(ViewModel,
                vm => vm.State.SelectedTopic,
                v => v.TopicComboBox.SelectedItem);

            this.OneWayBind(ViewModel,
                vm => vm.State.SelectedSection,
                v => v.SectionComboBox.SelectedItem);

            // Set up ComboBox Selection subscriptions (UI changes to VM methods)
            this.WhenAnyValue(v => v.CategoryComboBox.SelectedItem)
                .Subscribe(Observer.Create<object?>(cat => {
                    if (ViewModel != null)
                    {
                        var task = ViewModel.SetSelectedCategory(cat as Category);
                    }
                }))
                .DisposeWith(disposables);

            this.WhenAnyValue(v => v.TopicComboBox.SelectedItem)
                .Subscribe(Observer.Create<object?>(top => {
                    if (ViewModel != null)
                    {
                        var task = ViewModel.SetSelectedTopic(top as Topic);
                    }
                }))
                .DisposeWith(disposables);

            this.WhenAnyValue(v => v.SectionComboBox.SelectedItem)
                .Subscribe(Observer.Create<object?>(sec => {
                    ViewModel?.SetSelectedSection(sec as Section);
                }))
                .DisposeWith(disposables);

            // Custom Section Text
            this.WhenAnyValue(v => v.CustomSectionTextBox.Text)
                .Subscribe(Observer.Create<string?>(text => ViewModel?.SetCustomSectionName(text ?? string.Empty)))
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel,
                vm => vm.State.CustomSectionName,
                v => v.CustomSectionTextBox.Text);

            // Section field activity condition (requires topic selected)
            this.OneWayBind(ViewModel,
                vm => vm.State.SelectedTopic,
                v => v.SectionComboBox.IsEnabled,
                topic => topic != null);

            this.OneWayBind(ViewModel,
                vm => vm.State.SelectedTopic,
                v => v.CustomSectionTextBox.IsEnabled,
                topic => topic != null);

            // Question Text Input
            this.WhenAnyValue(v => v.QuestionTextBox.Text)
                .Subscribe(Observer.Create<string?>(text => ViewModel?.SetQuestionText(text ?? string.Empty)))
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel,
                vm => vm.State.QuestionText,
                v => v.QuestionTextBox.Text);

            // Answer Text Input
            this.WhenAnyValue(v => v.AnswerTextBox.Text)
                .Subscribe(Observer.Create<string?>(text => ViewModel?.SetAnswerText(text ?? string.Empty)))
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel,
                vm => vm.State.AnswerText,
                v => v.AnswerTextBox.Text);

            // Is Note Checkbox
            this.WhenAnyValue(v => v.IsNoteCheckBox.IsChecked)
                .Subscribe(Observer.Create<bool?>(val => ViewModel?.SetIsNote(val ?? false)))
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel,
                vm => vm.State.IsNote,
                v => v.IsNoteCheckBox.IsChecked);

            // Question textbox and images visibility bound to IsNote
            this.OneWayBind(ViewModel,
                vm => vm.State.IsNote,
                v => v.QuestionInputPanel.IsVisible,
                isNote => !isNote);

            // Is Problematic Checkbox
            this.WhenAnyValue(v => v.IsProblematicCheckBox.IsChecked)
                .Subscribe(Observer.Create<bool?>(val => ViewModel?.SetIsProblematic(val ?? false)))
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel,
                vm => vm.State.IsProblematic,
                v => v.IsProblematicCheckBox.IsChecked);

            // Group Connection ID Input
            this.WhenAnyValue(v => v.GroupIdTextBox.Text)
                .Subscribe(Observer.Create<string?>(text => {
                    if (int.TryParse(text, out int gId))
                        ViewModel?.SetGroupId(gId);
                    else
                        ViewModel?.SetGroupId(null);
                }))
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel,
                vm => vm.State.GroupId,
                v => v.GroupIdTextBox.Text,
                gId => gId?.ToString() ?? string.Empty);

            // Image list ItemSources
            this.OneWayBind(ViewModel,
                vm => vm.State.QuestionImages,
                v => v.QuestionImagesItemsControl.ItemsSource);

            this.OneWayBind(ViewModel,
                vm => vm.State.AnswerImages,
                v => v.AnswerImagesItemsControl.ItemsSource);

            // Image Add Buttons click bindings
            Observable.FromEventPattern(ChooseQuestionImageButton, nameof(Button.Click))
                .Subscribe(Observer.Create<EventPattern<object>>(_ => { var t = SelectImageFileAsync(MediaStatus.QUESTION); }))
                .DisposeWith(disposables);

            Observable.FromEventPattern(PasteQuestionImageButton, nameof(Button.Click))
                .Subscribe(Observer.Create<EventPattern<object>>(_ => { var t = PasteImageFromClipboardAsync(MediaStatus.QUESTION); }))
                .DisposeWith(disposables);

            Observable.FromEventPattern(ChooseAnswerImageButton, nameof(Button.Click))
                .Subscribe(Observer.Create<EventPattern<object>>(_ => { var t = SelectImageFileAsync(MediaStatus.ANSWER); }))
                .DisposeWith(disposables);

            Observable.FromEventPattern(PasteAnswerImageButton, nameof(Button.Click))
                .Subscribe(Observer.Create<EventPattern<object>>(_ => { var t = PasteImageFromClipboardAsync(MediaStatus.ANSWER); }))
                .DisposeWith(disposables);

            // Feedback strings
            this.OneWayBind(ViewModel,
                vm => vm.State.SuccessMessage,
                v => v.SuccessTextBlock.Text);

            this.OneWayBind(ViewModel,
                vm => vm.State.ErrorMessage,
                v => v.ErrorTextBlock.Text);

            // Last added preview bindings
            this.OneWayBind(ViewModel,
                vm => vm.State.LastAddedId,
                v => v.LastAddedPreviewBorder.IsVisible,
                id => id.HasValue);

            this.OneWayBind(ViewModel,
                vm => vm.State.LastAddedQuestionText,
                v => v.LastAddedQuestionTextBlock.Text);

            this.OneWayBind(ViewModel,
                vm => vm.State.LastAddedAnswerText,
                v => v.LastAddedAnswerTextBlock.Text);

            // Edit last added button execution
            Observable.FromEventPattern(EditLastAddedButton, nameof(Button.Click))
                .Subscribe(Observer.Create<EventPattern<object>>(_ =>
                {
                    if (ViewModel != null && ViewModel.State.LastAddedId.HasValue)
                    {
                        var lastId = ViewModel.State.LastAddedId.Value;
                        var catId = ViewModel.State.SelectedCategory?.Id;
                        ViewModel.HostScreen.Router.Navigate.Execute(
                            new QuestionEditorViewModel(ViewModel.HostScreen, catId, lastId)
                        );
                    }
                }))
                .DisposeWith(disposables);

            // Basic actions buttons commands
            this.BindCommand(ViewModel,
                vm => vm.SaveQuestionCommand,
                v => v.SaveButton);

            this.BindCommand(ViewModel,
                vm => vm.GoBackCommand,
                v => v.BackButton);

            this.OneWayBind(ViewModel,
                vm => vm.State.IsLoading,
                v => v.SaveButton.IsEnabled,
                loading => !loading);
        });
    }

    private async Task SelectImageFileAsync(MediaStatus status)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is { } provider)
        {
            var options = new FilePickerOpenOptions
            {
                Title = "Select Image File",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Images") { Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif", "*.svg" } }
                }
            };

            var files = await provider.OpenFilePickerAsync(options);
            if (files.Count > 0 && files[0].Path.IsAbsoluteUri)
            {
                var filePath = files[0].Path.LocalPath;
                ViewModel?.AddImage(filePath, status);
            }
        }
    }

    private async Task PasteImageFromClipboardAsync(MediaStatus status)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.Clipboard is { } clipboard)
        {
            try
            {
                var bitmap = await clipboard.TryGetBitmapAsync();
                if (bitmap != null)
                {
                    using var ms = new MemoryStream();
                    bitmap.Save(ms);
                    var bytes = ms.ToArray();
                    ViewModel?.AddImageBytes(bytes, status);
                }
            }
            catch (Exception ex)
            {
                if (ViewModel != null)
                {
                    ViewModel.SetErrorMessage($"Failed to paste image: {ex.Message}");
                }
            }
        }
    }

    public async void OnRemoveQuestionImageClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is string filename)
        {
            if (ViewModel != null)
            {
                await ViewModel.RemoveImageAsync(filename, MediaStatus.QUESTION);
            }
        }
    }

    public async void OnRemoveAnswerImageClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is string filename)
        {
            if (ViewModel != null)
            {
                await ViewModel.RemoveImageAsync(filename, MediaStatus.ANSWER);
            }
        }
    }
}

/// <summary>
/// Converter to translate image filename / absolute path string to a displayable Bitmap.
/// </summary>
public class ImagePathToBitmapConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string pathOrFilename)
        {
            try
            {
                if (pathOrFilename == "[Clipboard Image]")
                {
                    return null;
                }

                string fullPath;
                if (Path.IsPathRooted(pathOrFilename))
                {
                    fullPath = pathOrFilename;
                }
                else
                {
                    var config = Locator.Current.GetService<IOptions<AppConfig>>()?.Value;
                    var mediaDir = config?.ResolvedMediaDirectoryPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "media");
                    fullPath = Path.Combine(mediaDir, pathOrFilename);
                }

                if (File.Exists(fullPath))
                {
                    return new Avalonia.Media.Imaging.Bitmap(fullPath);
                }
            }
            catch
            {
                // return null on exception
            }
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

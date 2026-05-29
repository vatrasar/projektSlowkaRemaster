using System;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using ReactiveUI;
using Avalonia.ReactiveUI;
using Avalonia.Platform.Storage;
using ProjektSlowkaRemasterd.Src.Features.Category.UI.Screens.Manage;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;
using ProjektSlowkaRemasterd.Src.Core.Domain.Enums;

namespace ProjektSlowkaRemasterd.Src.Features.Category.UI.Screens.Manage;

using Category = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Category;


/// <summary>
/// Screen: ManageView
/// Purpose: Entry point for Category, Topic, Section and Question management.
/// Available Functionalities: Custom management of categories, topics, sections, and questions.
/// Key UI Elements: ListBoxes, buttons, inline forms, status messages.
/// Navigation:
///   - Navigate From: HostView sidebar navigation
///   - Navigate To: QuestionEditorView (via Add/Edit buttons)
/// </summary>
public partial class ManageView : ReactiveUserControl<ManageViewModel>
{
    public ManageView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            // Bind ListBoxes ItemsSource
            this.OneWayBind(ViewModel,
                vm => vm.State.Categories,
                v => v.CategoriesListBox.ItemsSource);

            this.OneWayBind(ViewModel,
                vm => vm.State.Topics,
                v => v.TopicsListBox.ItemsSource);

            this.OneWayBind(ViewModel,
                vm => vm.State.Sections,
                v => v.SectionsListBox.ItemsSource);

            this.OneWayBind(ViewModel,
                vm => vm.State.Questions,
                v => v.QuestionsListBox.ItemsSource);

            // Bind selection
            this.WhenAnyValue(x => x.CategoriesListBox.SelectedItem)
                .Where(selected => selected is Core.Domain.Models.Category)
                .Select(selected => (Core.Domain.Models.Category)selected!)
                .Subscribe(Observer.Create<Core.Domain.Models.Category>(category =>
                {
                    _ = ViewModel?.SelectCategoryAsync(category);
                }));

            this.WhenAnyValue(x => x.ViewModel!.State.SelectedCategory)
                .Subscribe(Observer.Create<Core.Domain.Models.Category?>(category =>
                {
                    CategoriesListBox.SelectedItem = category;
                }));

            this.WhenAnyValue(x => x.TopicsListBox.SelectedItem)
                .Subscribe(Observer.Create<object?>(item =>
                {
                    var topic = item as Topic;
                    _ = ViewModel?.SelectTopicAsync(topic);
                }));

            this.WhenAnyValue(x => x.ViewModel!.State.SelectedTopic)
                .Subscribe(Observer.Create<Topic?>(topic =>
                {
                    TopicsListBox.SelectedItem = topic;
                }));

            // Visibility bindings
            this.OneWayBind(ViewModel,
                vm => vm.State.SelectedCategory,
                v => v.NoCategorySelectedTextBlock.IsVisible,
                cat => cat == null);

            this.OneWayBind(ViewModel,
                vm => vm.State.SelectedCategory,
                v => v.CategoryContentGrid.IsVisible,
                cat => cat != null);

            this.OneWayBind(ViewModel,
                vm => vm.State.IsEditingCategory,
                v => v.CategoryDetailsPanel.IsVisible,
                editing => !editing);

            this.OneWayBind(ViewModel,
                vm => vm.State.IsEditingCategory,
                v => v.CategoryEditPanel.IsVisible,
                editing => editing);

            this.OneWayBind(ViewModel,
                vm => vm.State.EditingTopic,
                v => v.TopicEditPanel.IsVisible,
                topic => topic != null);

            this.OneWayBind(ViewModel,
                vm => vm.State.SelectedTopic,
                v => v.SectionsPanel.IsVisible,
                topic => topic != null);

            this.OneWayBind(ViewModel,
                vm => vm.State.EditingSection,
                v => v.SectionEditPanel.IsVisible,
                sec => sec != null);

            // Text / Input bindings
            this.WhenAnyValue(x => x.EditCategoryNameTextBox.Text)
                .Subscribe(Observer.Create<string?>(text => ViewModel?.SetEditingCategoryName(text ?? string.Empty)));

            this.WhenAnyValue(x => x.ViewModel!.State.EditingCategoryName)
                .Subscribe(Observer.Create<string>(name =>
                {
                    if (EditCategoryNameTextBox.Text != name)
                    {
                        EditCategoryNameTextBox.Text = name;
                    }
                }));

            this.WhenAnyValue(x => x.EditCategoryDoubleSidedCheckBox.IsChecked)
                .Subscribe(Observer.Create<bool?>(isChecked => ViewModel?.SetEditingCategoryDoubleSided(isChecked ?? false)));

            this.WhenAnyValue(x => x.ViewModel!.State.EditingCategoryDoubleSided)
                .Subscribe(Observer.Create<bool>(val =>
                {
                    if (EditCategoryDoubleSidedCheckBox.IsChecked != val)
                    {
                        EditCategoryDoubleSidedCheckBox.IsChecked = val;
                    }
                }));

            this.WhenAnyValue(x => x.NewTopicNameTextBox.Text)
                .Subscribe(Observer.Create<string?>(text => ViewModel?.SetNewTopicName(text ?? string.Empty)));

            this.WhenAnyValue(x => x.ViewModel!.State.NewTopicName)
                .Subscribe(Observer.Create<string>(text =>
                {
                    if (NewTopicNameTextBox.Text != text)
                    {
                        NewTopicNameTextBox.Text = text;
                    }
                }));

            this.WhenAnyValue(x => x.EditTopicNameTextBox.Text)
                .Subscribe(Observer.Create<string?>(text => ViewModel?.SetEditingTopicName(text ?? string.Empty)));

            this.WhenAnyValue(x => x.ViewModel!.State.EditingTopicName)
                .Subscribe(Observer.Create<string>(text =>
                {
                    if (EditTopicNameTextBox.Text != text)
                    {
                        EditTopicNameTextBox.Text = text;
                    }
                }));

            this.WhenAnyValue(x => x.NewSectionNameTextBox.Text)
                .Subscribe(Observer.Create<string?>(text => ViewModel?.SetNewSectionName(text ?? string.Empty)));

            this.WhenAnyValue(x => x.ViewModel!.State.NewSectionName)
                .Subscribe(Observer.Create<string>(text =>
                {
                    if (NewSectionNameTextBox.Text != text)
                    {
                        NewSectionNameTextBox.Text = text;
                    }
                }));

            this.WhenAnyValue(x => x.EditSectionNameTextBox.Text)
                .Subscribe(Observer.Create<string?>(text => ViewModel?.SetEditingSectionName(text ?? string.Empty)));

            this.WhenAnyValue(x => x.ViewModel!.State.EditingSectionName)
                .Subscribe(Observer.Create<string>(text =>
                {
                    if (EditSectionNameTextBox.Text != text)
                    {
                        EditSectionNameTextBox.Text = text;
                    }
                }));

            this.WhenAnyValue(x => x.ShowArchivedCheckBox.IsChecked)
                .Subscribe(Observer.Create<bool?>(isChecked => ViewModel?.SetShowArchivedOnly(isChecked ?? false)));

            this.WhenAnyValue(x => x.ViewModel!.State.ShowArchivedOnly)
                .Subscribe(Observer.Create<bool>(val =>
                {
                    if (ShowArchivedCheckBox.IsChecked != val)
                    {
                        ShowArchivedCheckBox.IsChecked = val;
                    }
                }));

            // Success / Error status messages
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

            // Command bindings
            this.BindCommand(ViewModel,
                vm => vm.NavigateToAddCategoryCommand,
                v => v.AddCategoryButton);

            this.BindCommand(ViewModel,
                vm => vm.SaveCategoryEditCommand,
                v => v.SaveCategoryEditButton);

            this.BindCommand(ViewModel,
                vm => vm.DeleteCategoryCommand,
                v => v.DeleteCategoryButton);

            this.BindCommand(ViewModel,
                vm => vm.ArchiveAllQuestionsCommand,
                v => v.ArchiveAllButton);

            this.BindCommand(ViewModel,
                vm => vm.ExportCategoryCommand,
                v => v.ExportLaTeXButton);

            ViewModel!.ShowFolderPickerInteraction.RegisterHandler(async interaction =>
            {
                var folderPath = await ShowFolderPickerAsync();
                interaction.SetOutput(folderPath);
            }).DisposeWith(disposables);

            this.BindCommand(ViewModel,
                vm => vm.NavigateToBulkImportCommand,
                v => v.BulkImportButton);

            this.BindCommand(ViewModel,
                vm => vm.NavigateToAddQuestionCommand,
                v => v.AddQuestionButton);

            this.BindCommand(ViewModel,
                vm => vm.AddTopicCommand,
                v => v.AddTopicButton);

            this.BindCommand(ViewModel,
                vm => vm.SaveTopicEditCommand,
                v => v.SaveTopicEditButton);

            this.BindCommand(ViewModel,
                vm => vm.AddSectionCommand,
                v => v.AddSectionButton);

            this.BindCommand(ViewModel,
                vm => vm.SaveSectionEditCommand,
                v => v.SaveSectionEditButton);

            // Void actions using clicks
            Observable.FromEventPattern<EventHandler<RoutedEventArgs>, RoutedEventArgs>(
                h => EditCategoryButton.Click += h,
                h => EditCategoryButton.Click -= h)
                .Subscribe(Observer.Create<EventPattern<RoutedEventArgs>>(_ => ViewModel?.StartEditCategory()))
                .DisposeWith(disposables);

            Observable.FromEventPattern<EventHandler<RoutedEventArgs>, RoutedEventArgs>(
                h => CancelCategoryEditButton.Click += h,
                h => CancelCategoryEditButton.Click -= h)
                .Subscribe(Observer.Create<EventPattern<RoutedEventArgs>>(_ => ViewModel?.CancelEditCategory()))
                .DisposeWith(disposables);

            Observable.FromEventPattern<EventHandler<RoutedEventArgs>, RoutedEventArgs>(
                h => CancelTopicEditButton.Click += h,
                h => CancelTopicEditButton.Click -= h)
                .Subscribe(Observer.Create<EventPattern<RoutedEventArgs>>(_ => ViewModel?.CancelEditTopic()))
                .DisposeWith(disposables);

            Observable.FromEventPattern<EventHandler<RoutedEventArgs>, RoutedEventArgs>(
                h => CancelSectionEditButton.Click += h,
                h => CancelSectionEditButton.Click -= h)
                .Subscribe(Observer.Create<EventPattern<RoutedEventArgs>>(_ => ViewModel?.CancelEditSection()))
                .DisposeWith(disposables);
        });
    }

    // ListBox DataTemplate Event Handlers
    public void OnEditQuestionClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is Core.Domain.Models.Question question)
        {
            ViewModel?.EditQuestion(question);
        }
    }

    public async void OnDeleteQuestionClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is Core.Domain.Models.Question question)
        {
            if (ViewModel != null)
            {
                await ViewModel.DeleteQuestionAsync(question);
            }
        }
    }

    public async void OnRestoreQuestionClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is Core.Domain.Models.Question question)
        {
            if (ViewModel != null)
            {
                await ViewModel.RestoreQuestionAsync(question);
            }
        }
    }

    public void OnStartEditTopicClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is Topic topic)
        {
            ViewModel?.StartEditTopic(topic);
        }
    }

    public void OnStartEditSectionClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is Section section)
        {
            ViewModel?.StartEditSection(section);
        }
    }

    public async void OnDeleteSectionClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is Section section)
        {
            if (ViewModel != null)
            {
                await ViewModel.DeleteSectionCommand.Execute(section);
            }
        }
    }

    private async Task<string?> ShowFolderPickerAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider == null)
        {
            return null;
        }

        var options = new FolderPickerOpenOptions
        {
            Title = "Select Export Folder",
            AllowMultiple = false
        };

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(options);
        if (folders.Count == 0)
        {
            return null;
        }

        if (!folders[0].Path.IsAbsoluteUri)
        {
            return null;
        }

        return folders[0].Path.LocalPath;
    }
}

public class QuestionStatusToVisibleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is QuestionStatus status && parameter is string paramStr)
        {
            if (paramStr == "Archived")
            {
                return status == QuestionStatus.TO_ARCHIVE;
            }
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}


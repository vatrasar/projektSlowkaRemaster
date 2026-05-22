using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using ReactiveUI;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;

namespace ProjektSlowkaRemasterd.Src.Features.Search.UI.Screens.Search;

using Category = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Category;

/// <summary>
/// Screen: SearchView
/// Purpose: Full-text search and metadata filtering for flashcards.
/// Available Functionalities: Filter by category, topic, section, or status, search text, edit or delete questions.
/// Key UI Elements: SearchTextBox, CategoryComboBox, TopicComboBox, SectionComboBox, StatusComboBox, ResultsListBox.
/// Navigation:
///   - Navigate From: HostView sidebar navigation
///   - Navigate To: QuestionEditorView (when editing a search result)
/// </summary>
public partial class SearchView : ReactiveUserControl<SearchViewModel>
{
    public SearchView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            // Bind Loading Progress Bar
            this.OneWayBind(ViewModel, vm => vm.State.IsLoading, v => v.SearchProgressBar.IsVisible);

            // Bind ListSources
            this.OneWayBind(ViewModel, vm => vm.State.Categories, v => v.CategoryComboBox.ItemsSource);
            this.OneWayBind(ViewModel, vm => vm.State.Topics, v => v.TopicComboBox.ItemsSource);
            this.OneWayBind(ViewModel, vm => vm.State.Sections, v => v.SectionComboBox.ItemsSource);
            this.OneWayBind(ViewModel, vm => vm.State.Statuses, v => v.StatusComboBox.ItemsSource);
            this.OneWayBind(ViewModel, vm => vm.State.Results, v => v.ResultsListBox.ItemsSource);

            // Bind Selection values
            this.OneWayBind(ViewModel, vm => vm.State.SelectedCategory, v => v.CategoryComboBox.SelectedItem);
            this.OneWayBind(ViewModel, vm => vm.State.SelectedTopic, v => v.TopicComboBox.SelectedItem);
            this.OneWayBind(ViewModel, vm => vm.State.SelectedSection, v => v.SectionComboBox.SelectedItem);
            this.OneWayBind(ViewModel, vm => vm.State.SelectedStatus, v => v.StatusComboBox.SelectedItem);
            this.OneWayBind(ViewModel, vm => vm.State.SearchText, v => v.SearchTextBox.Text);

            // Bind No Results text block visibility
            this.WhenAnyValue(x => x.ViewModel!.State.Results.Count, x => x.ViewModel!.State.IsLoading)
                .Select(t => t.Item1 == 0 && !t.Item2)
                .Subscribe(Observer.Create<bool>(visible => NoResultsTextBlock.IsVisible = visible))
                .DisposeWith(disposables);

            // Bind result list visibility
            this.WhenAnyValue(x => x.ViewModel!.State.Results.Count)
                .Select(count => count > 0)
                .Subscribe(Observer.Create<bool>(visible => ResultsListBox.IsVisible = visible))
                .DisposeWith(disposables);

            // Event-based synchronization for selections to trigger filter loading & updates
            this.WhenAnyValue(x => x.CategoryComboBox.SelectedItem)
                .Select(item => item as Category)
                .Subscribe(Observer.Create<Category?>(async cat =>
                {
                    if (ViewModel != null && cat != ViewModel.State.SelectedCategory)
                    {
                        await ViewModel.SetSelectedCategoryAsync(cat);
                    }
                }))
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.TopicComboBox.SelectedItem)
                .Select(item => item as Topic)
                .Subscribe(Observer.Create<Topic?>(async topic =>
                {
                    if (ViewModel != null && topic != ViewModel.State.SelectedTopic)
                    {
                        await ViewModel.SetSelectedTopicAsync(topic);
                    }
                }))
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.SectionComboBox.SelectedItem)
                .Select(item => item as Section)
                .Subscribe(Observer.Create<Section?>(async section =>
                {
                    if (ViewModel != null && section != ViewModel.State.SelectedSection)
                    {
                        await ViewModel.SetSelectedSectionAsync(section);
                    }
                }))
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.StatusComboBox.SelectedItem)
                .Select(item => item as string)
                .Subscribe(Observer.Create<string?>(async status =>
                {
                    if (ViewModel != null && status != ViewModel.State.SelectedStatus)
                    {
                        await ViewModel.SetSelectedStatusAsync(status ?? "All");
                    }
                }))
                .DisposeWith(disposables);

            // Debounced Search Text Input
            this.WhenAnyValue(x => x.SearchTextBox.Text)
                .Throttle(TimeSpan.FromMilliseconds(400))
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(Observer.Create<string?>(async text =>
                {
                    if (ViewModel != null && text != ViewModel.State.SearchText)
                    {
                        await ViewModel.SetSearchTextAsync(text ?? string.Empty);
                    }
                }))
                .DisposeWith(disposables);
        });
    }

    // ListBox Item Edit button click handler
    public void OnEditQuestionClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is SearchResultItem item)
        {
            ViewModel?.EditQuestionCommand.Execute(item.Question.Id).Subscribe();
        }
    }

    // ListBox Item Delete button click handler
    public void OnDeleteQuestionClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is SearchResultItem item)
        {
            ViewModel?.DeleteQuestionCommand.Execute(item.Question.Id).Subscribe();
        }
    }
}

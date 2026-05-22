using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace ProjektSlowkaRemasterd.Src.Features.Review.UI.Screens.ReviewSelection;

/// <summary>
/// Code-behind for the ReviewSelectionView.
/// Establishes reactive bindings for categories, topics, and counts.
/// </summary>
public partial class ReviewSelectionView : ReactiveUserControl<ReviewSelectionViewModel>
{
    public ReviewSelectionView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            // Bind loading indicator
            this.OneWayBind(ViewModel, vm => vm.State.IsLoading, v => v.LoadingBar.IsVisible);

            // Bind categories list
            this.OneWayBind(ViewModel, vm => vm.State.Categories, v => v.CategoriesListBox.ItemsSource);

            // Bind selected category (bidirectional)
            this.Bind(ViewModel, vm => vm.State.SelectedCategory, v => v.CategoriesListBox.SelectedItem);

            // When selected category changes, update topics list and headers
            this.WhenAnyValue(x => x.ViewModel!.State.SelectedCategory)
                .Subscribe(Observer.Create<CategoryReviewInfo?>(selected =>
                {
                    if (selected == null)
                    {
                        SelectedCategoryNameText.Text = "Select a Category";
                        SelectedCategoryStatusText.Text = "Choose a category from the left to view due reviews.";
                        StartAllButton.IsVisible = false;
                        TopicsListBox.IsVisible = false;
                    }
                    else
                    {
                        SelectedCategoryNameText.Text = selected.Category.Name;
                        SelectedCategoryStatusText.Text = $"Total due reviews in category: {selected.DueCount}";
                        StartAllButton.IsVisible = selected.DueCount > 0;
                        AllCountText.Text = $"{selected.DueCount} due";
                        TopicsListBox.IsVisible = true;
                    }
                }))
                .DisposeWith(disposables);

            // Bind topics list
            this.OneWayBind(ViewModel, vm => vm.State.Topics, v => v.TopicsListBox.ItemsSource);

            // Bind category selection trigger
            this.WhenAnyValue(x => x.CategoriesListBox.SelectedItem)
                .Where(item => item != null)
                .Select(item => (CategoryReviewInfo)item!)
                .Subscribe(Observer.Create<CategoryReviewInfo>(categoryInfo =>
                {
                    ViewModel?.SelectCategoryCommand.Execute(categoryInfo).Subscribe();
                }))
                .DisposeWith(disposables);

            // Bind Start All button (calls StartReviewCommand with null)
            this.BindCommand(ViewModel, vm => vm.StartReviewCommand, v => v.StartAllButton, 
                this.WhenAnyValue(x => x.ViewModel!.State.SelectedCategory)
                    .Select(_ => (TopicReviewInfo?)null));
        });
    }
}

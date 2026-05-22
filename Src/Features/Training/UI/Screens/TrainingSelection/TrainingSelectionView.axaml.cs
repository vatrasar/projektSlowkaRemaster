using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;
using ProjektSlowkaRemasterd.Src.Features.Training.UI.Screens.TrainingSelection;

namespace ProjektSlowkaRemasterd.Src.Features.Training.UI.Screens.TrainingSelection;

/// <summary>
/// Screen: TrainingSelectionView
/// Purpose: Interface for selecting training parameters.
/// Available Functionalities: Select tomorrow, problematic, marked categories, or specific category/topic.
/// Key UI Elements: CategoryComboBox, TopicComboBox, TrainTomorrowButton, TrainProblematicButton, TrainMarkedButton, TrainCategoryButton, TrainTopicButton.
/// Navigation:
///   - Navigate From: HostView sidebar navigation
///   - Navigate To: TrainingSessionView
/// </summary>
public partial class TrainingSelectionView : ReactiveUserControl<TrainingSelectionViewModel>
{
    public TrainingSelectionView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            // Bind loading bar
            this.OneWayBind(ViewModel, vm => vm.State.IsLoading, v => v.SelectionLoadingBar.IsVisible);

            // Bind counts
            this.WhenAnyValue(x => x.ViewModel!.State.TomorrowCount)
                .Select(count => $"{count} cards")
                .Subscribe(Observer.Create<string>(text => TomorrowCountText.Text = text))
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.ViewModel!.State.ProblematicCount)
                .Select(count => $"{count} cards")
                .Subscribe(Observer.Create<string>(text => ProblematicCountText.Text = text))
                .DisposeWith(disposables);

            // Bind Lists
            this.OneWayBind(ViewModel, vm => vm.State.Categories, v => v.CategoriesListBox.ItemsSource);
            this.OneWayBind(ViewModel, vm => vm.State.Categories, v => v.CategoryComboBox.ItemsSource);
            this.OneWayBind(ViewModel, vm => vm.State.Topics, v => v.TopicComboBox.ItemsSource);

            // ComboBox selections (OneWay + Event selection synchronization)
            this.OneWayBind(ViewModel, vm => vm.State.SelectedCategory, v => v.CategoryComboBox.SelectedItem);
            this.OneWayBind(ViewModel, vm => vm.State.SelectedTopic, v => v.TopicComboBox.SelectedItem);

            this.WhenAnyValue(x => x.CategoryComboBox.SelectedItem)
                .Select(item => item as CategoryTrainingInfo)
                .Subscribe(Observer.Create<CategoryTrainingInfo?>(async cat =>
                {
                    if (ViewModel != null)
                    {
                        await ViewModel.SetSelectedCategoryAsync(cat);
                    }
                }))
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.TopicComboBox.SelectedItem)
                .Select(item => item as TopicTrainingInfo)
                .Subscribe(Observer.Create<TopicTrainingInfo?>(topic =>
                {
                    ViewModel?.SetSelectedTopic(topic);
                }))
                .DisposeWith(disposables);

            // Bind commands to buttons
            this.BindCommand(ViewModel, vm => vm.TrainTomorrowCommand, v => v.TrainTomorrowButton);
            this.BindCommand(ViewModel, vm => vm.TrainProblematicCommand, v => v.TrainProblematicButton);
            this.BindCommand(ViewModel, vm => vm.TrainMarkedCategoriesCommand, v => v.TrainMarkedButton);
            this.BindCommand(ViewModel, vm => vm.TrainSelectedCategoryCommand, v => v.TrainCategoryButton);
            this.BindCommand(ViewModel, vm => vm.TrainSelectedTopicCommand, v => v.TrainTopicButton);
        });
    }
}

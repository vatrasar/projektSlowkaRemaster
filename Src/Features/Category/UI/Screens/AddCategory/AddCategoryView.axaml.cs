using System;
using System.Reactive;
using System.Reactive.Disposables;
using ReactiveUI;
using Avalonia.ReactiveUI;

using ProjektSlowkaRemasterd.Src.Features.Category.UI.Screens.AddCategory;

namespace ProjektSlowkaRemasterd.Src.Features.Category.UI.Screens.AddCategory;

/// <summary>
/// Screen: AddCategoryView
/// Purpose: Form for user to create a new category with bidirectional reviews option.
/// Available Functionalities: Enter category name, select double-sided reviews, save or cancel.
/// Key UI Elements: CategoryNameTextBox, DoubleSidedCheckBox, SaveCategoryButton, CancelButton, ErrorTextBlock.
/// Navigation:
///   - Navigate From: HomeView welcome page, ManageView
///   - Navigate To: QuestionEditorView (if successful), or back to parent
/// </summary>
public partial class AddCategoryView : ReactiveUserControl<AddCategoryViewModel>
{
    public AddCategoryView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            // Bind text input
            this.WhenAnyValue(v => v.CategoryNameTextBox.Text)
                .Subscribe(Observer.Create<string?>(text => ViewModel?.SetCategoryName(text ?? string.Empty)))
                .DisposeWith(disposables);

            // Bind checkbox
            this.WhenAnyValue(v => v.DoubleSidedCheckBox.IsChecked)
                .Subscribe(Observer.Create<bool?>(val => ViewModel?.SetDoubleSided(val ?? false)))
                .DisposeWith(disposables);

            // Bind Error message
            this.OneWayBind(ViewModel,
                vm => vm.State.ErrorMessage,
                v => v.ErrorTextBlock.Text);

            // Bind Cancel button to GoBack command
            this.BindCommand(ViewModel,
                vm => vm.GoBackCommand,
                v => v.CancelButton);

            // Bind Save button to SaveCategory command
            this.BindCommand(ViewModel,
                vm => vm.SaveCategoryCommand,
                v => v.SaveCategoryButton);

            // Disable save button during loading
            this.OneWayBind(ViewModel,
                vm => vm.State.IsLoading,
                v => v.SaveCategoryButton.IsEnabled,
                loading => !loading);
        });
    }
}

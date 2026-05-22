using System;
using System.Reactive;
using ReactiveUI;
using ProjektSlowkaRemasterd.Src.Core.Mvvm;
using ProjektSlowkaRemasterd.Src.Features.Home.UI.Screens.Home;
using ProjektSlowkaRemasterd.Src.Features.Review.UI.Screens.ReviewSelection;
using ProjektSlowkaRemasterd.Src.Features.Training.UI.Screens.TrainingSelection;
using ProjektSlowkaRemasterd.Src.Features.Category.UI.Screens.Manage;
using ProjektSlowkaRemasterd.Src.Features.Search.UI.Screens.Search;
using ProjektSlowkaRemasterd.Src.Features.Settings.UI.Screens.Settings;

namespace ProjektSlowkaRemasterd.Src.Features.Shell.UI.Screens.Host;

/// <summary>
/// Root shell host view model coordinating sidebar navigation commands.
/// </summary>
public class HostViewModel : ViewModelBase, IRoutableViewModel
{
    public string? UrlPathSegment => "host";
    public IScreen HostScreen { get; }

    public ReactiveCommand<Unit, IRoutableViewModel> NavigateHome { get; }
    public ReactiveCommand<Unit, IRoutableViewModel> NavigateReview { get; }
    public ReactiveCommand<Unit, IRoutableViewModel> NavigateTraining { get; }
    public ReactiveCommand<Unit, IRoutableViewModel> NavigateManage { get; }
    public ReactiveCommand<Unit, IRoutableViewModel> NavigateSearch { get; }
    public ReactiveCommand<Unit, IRoutableViewModel> NavigateSettings { get; }

    public HostViewModel(IScreen hostScreen)
    {
        HostScreen = hostScreen;

        NavigateHome = ReactiveCommand.CreateFromObservable(() => 
            HostScreen.Router.Navigate.Execute(new HomeViewModel(HostScreen)));

        NavigateReview = ReactiveCommand.CreateFromObservable(() => 
            HostScreen.Router.Navigate.Execute(new ReviewSelectionViewModel(HostScreen)));

        NavigateTraining = ReactiveCommand.CreateFromObservable(() => 
            HostScreen.Router.Navigate.Execute(new TrainingSelectionViewModel(HostScreen)));

        NavigateManage = ReactiveCommand.CreateFromObservable(() => 
            HostScreen.Router.Navigate.Execute(new ManageViewModel(HostScreen)));

        NavigateSearch = ReactiveCommand.CreateFromObservable(() => 
            HostScreen.Router.Navigate.Execute(new SearchViewModel(HostScreen)));

        NavigateSettings = ReactiveCommand.CreateFromObservable(() => 
            HostScreen.Router.Navigate.Execute(new SettingsViewModel(HostScreen)));
            
        HostScreen.Router.Navigate.Execute(new HomeViewModel(HostScreen)).Subscribe();
    }
}



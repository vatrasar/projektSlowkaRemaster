using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using Splat;
using ProjektSlowkaRemasterd.Src.Core.Mvvm;
using ProjektSlowkaRemasterd.Src.Core.Domain.RepositoryContracts;

namespace ProjektSlowkaRemasterd.Src.Features.Home.UI.Screens.Home;

/// <summary>
/// ViewModel for the Home screen.
/// Displays a welcome message and a 7-day review forecast queue.
/// </summary>
public class HomeViewModel : ViewModelBase<HomeState>, IRoutableViewModel
{
    public string? UrlPathSegment => "home";
    public IScreen HostScreen { get; }
    
    private readonly IQuestionRepository _questionRepository;

    public ReactiveCommand<Unit, Unit> LoadDataCommand { get; }

    public HomeViewModel(IScreen hostScreen) 
        : this(hostScreen, Locator.Current.GetService<IQuestionRepository>()!)
    {
    }

    public HomeViewModel(IScreen hostScreen, IQuestionRepository questionRepository) 
        : base(new HomeState())
    {
        HostScreen = hostScreen;
        _questionRepository = questionRepository ?? throw new ArgumentNullException(nameof(questionRepository));

        LoadDataCommand = ReactiveCommand.CreateFromTask(LoadDataAsync);

        // Load forecast on startup
        LoadDataCommand.Execute().Subscribe();
    }

    private async Task LoadDataAsync()
    {
        UpdateState(s => s with { IsLoading = true });
        
        try
        {
            var today = DateTime.Today;
            var endDate = today.AddDays(6);
            
            // Get all active review questions up to today + 6 days
            var allQuestions = await _questionRepository.GetReviewQuestionsAsync(endDate);
            var activeQuestions = allQuestions
                .Where(q => q.Status != Core.Domain.Enums.QuestionStatus.TO_ARCHIVE)
                .ToList();
            
            var list = new List<ForecastItem>();
            for (int i = 0; i < 7; i++)
            {
                var day = today.AddDays(i);
                int count;
                if (i == 0)
                {
                    // Today includes overdue reviews as well (nextReview <= today)
                    count = activeQuestions.Count(q => q.NextReview.Date <= day.Date);
                }
                else
                {
                    count = activeQuestions.Count(q => q.NextReview.Date == day.Date);
                }
                
                var dayName = day.DayOfWeek.ToString();
                list.Add(new ForecastItem(dayName, count));
            }
            
            UpdateState(s => s with 
            { 
                WeeklyReviews = list.ToImmutableList(),
                IsLoading = false 
            });
        }
        catch (Exception)
        {
            UpdateState(s => s with { IsLoading = false });
        }
    }
}

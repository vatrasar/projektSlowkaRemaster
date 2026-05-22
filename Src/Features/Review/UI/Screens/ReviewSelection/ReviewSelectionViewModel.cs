using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using Splat;
using ProjektSlowkaRemasterd.Src.Core.Mvvm;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;
using ProjektSlowkaRemasterd.Src.Core.Domain.RepositoryContracts;
using ProjektSlowkaRemasterd.Src.Core.Domain.Enums;
using ProjektSlowkaRemasterd.Src.Features.Review.UI.Screens.ReviewSession;

namespace ProjektSlowkaRemasterd.Src.Features.Review.UI.Screens.ReviewSelection;

/// <summary>
/// ViewModel for the Review Selection screen.
/// Displays a list of categories and topics with due card counts.
/// </summary>
public class ReviewSelectionViewModel : ViewModelBase<ReviewSelectionState>, IRoutableViewModel
{
    public string? UrlPathSegment => "review-selection";
    public IScreen HostScreen { get; }

    private readonly ICategoryRepository _categoryRepository;
    private readonly ITopicRepository _topicRepository;
    private readonly IQuestionRepository _questionRepository;

    public ReactiveCommand<Unit, Unit> LoadDataCommand { get; }
    public ReactiveCommand<CategoryReviewInfo, Unit> SelectCategoryCommand { get; }
    public ReactiveCommand<TopicReviewInfo?, Unit> StartReviewCommand { get; }

    public ReviewSelectionViewModel(IScreen hostScreen)
        : this(
            hostScreen,
            Locator.Current.GetService<ICategoryRepository>()!,
            Locator.Current.GetService<ITopicRepository>()!,
            Locator.Current.GetService<IQuestionRepository>()!)
    {
    }

    public ReviewSelectionViewModel(
        IScreen hostScreen,
        ICategoryRepository categoryRepository,
        ITopicRepository topicRepository,
        IQuestionRepository questionRepository)
        : base(new ReviewSelectionState())
    {
        HostScreen = hostScreen;
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _topicRepository = topicRepository ?? throw new ArgumentNullException(nameof(topicRepository));
        _questionRepository = questionRepository ?? throw new ArgumentNullException(nameof(questionRepository));

        LoadDataCommand = ReactiveCommand.CreateFromTask(LoadDataAsync);
        SelectCategoryCommand = ReactiveCommand.CreateFromTask<CategoryReviewInfo>(SelectCategoryAsync);
        StartReviewCommand = ReactiveCommand.Create<TopicReviewInfo?>(StartReview);

        LoadDataCommand.Execute().Subscribe();
    }

    private async Task LoadDataAsync()
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = string.Empty });
        try
        {
            var today = DateTime.Today;
            var categories = (await _categoryRepository.GetAllAsync()).ToList();
            var categoryInfos = new List<CategoryReviewInfo>();

            foreach (var c in categories)
            {
                var dueQuestions = await _questionRepository.GetReviewQuestionsByCategoryAsync(c.Id, today);
                var activeDue = dueQuestions.Count(q => q.Status != QuestionStatus.TO_ARCHIVE);
                categoryInfos.Add(new CategoryReviewInfo(c, activeDue));
            }

            UpdateState(s => s with 
            { 
                Categories = categoryInfos.ToImmutableList(), 
                IsLoading = false 
            });

            if (State.SelectedCategory != null)
            {
                var refreshedSelected = categoryInfos.FirstOrDefault(ci => ci.Category.Id == State.SelectedCategory.Category.Id);
                if (refreshedSelected != null)
                {
                    await SelectCategoryAsync(refreshedSelected);
                }
            }
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Error: {ex.Message}" });
        }
    }

    private async Task SelectCategoryAsync(CategoryReviewInfo categoryInfo)
    {
        UpdateState(s => s with { SelectedCategory = categoryInfo, IsLoading = true, ErrorMessage = string.Empty });
        try
        {
            var categoryId = categoryInfo.Category.Id;
            var today = DateTime.Today;
            var topics = await _topicRepository.GetByCategoryIdAsync(categoryId);
            var topicInfos = new List<TopicReviewInfo>();

            foreach (var t in topics)
            {
                var dueQuestions = await _questionRepository.GetReviewQuestionsByTopicAsync(t.Id, today);
                var activeDue = dueQuestions.Count(q => q.Status != QuestionStatus.TO_ARCHIVE);
                topicInfos.Add(new TopicReviewInfo(t, activeDue));
            }

            UpdateState(s => s with 
            { 
                Topics = topicInfos.ToImmutableList(),
                AllInCategoryCount = categoryInfo.DueCount,
                SelectedTopic = null,
                IsLoading = false 
            });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Error: {ex.Message}" });
        }
    }

    private void StartReview(TopicReviewInfo? topicInfo)
    {
        if (State.SelectedCategory == null) return;

        var categoryId = State.SelectedCategory.Category.Id;
        int? topicId = topicInfo?.Topic.Id;

        HostScreen.Router.Navigate.Execute(new ReviewSessionViewModel(HostScreen, categoryId, topicId)).Subscribe();
    }
}

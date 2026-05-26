using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using Splat;
using ProjektSlowkaRemasterd.Src.Core.Mvvm;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;
using ProjektSlowkaRemasterd.Src.Core.Domain.RepositoryContracts;
using ProjektSlowkaRemasterd.Src.Features.Training.UI.Screens.TrainingSession;

namespace ProjektSlowkaRemasterd.Src.Features.Training.UI.Screens.TrainingSelection;

using Question = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Question;
using Category = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Category;

public class TrainingSelectionViewModel : ViewModelBase<TrainingSelectionState>, IRoutableViewModel
{
    public string? UrlPathSegment => "training-selection";
    public IScreen HostScreen { get; }

    private readonly ICategoryRepository _categoryRepository;
    private readonly ITopicRepository _topicRepository;
    private readonly IQuestionRepository _questionRepository;

    private List<Category> _allCategoriesList = new();
    private List<Question> _allQuestionsList = new();

    public ReactiveCommand<Unit, Unit> LoadCommand { get; }
    public ReactiveCommand<int, Unit> ToggleCategoryMarkCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleProblematicFilterCommand { get; }
    public ReactiveCommand<Unit, Unit> TrainSelectedCategoryCommand { get; }
    public ReactiveCommand<Unit, Unit> TrainSelectedTopicCommand { get; }
    public ReactiveCommand<Unit, Unit> TrainMarkedCategoriesCommand { get; }

    public TrainingSelectionViewModel(IScreen hostScreen)
        : this(
            hostScreen,
            Locator.Current.GetService<ICategoryRepository>()!,
            Locator.Current.GetService<ITopicRepository>()!,
            Locator.Current.GetService<IQuestionRepository>()!)
    {
    }

    public TrainingSelectionViewModel(
        IScreen hostScreen,
        ICategoryRepository categoryRepository,
        ITopicRepository topicRepository,
        IQuestionRepository questionRepository)
        : base(new TrainingSelectionState())
    {
        HostScreen = hostScreen;
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _topicRepository = topicRepository ?? throw new ArgumentNullException(nameof(topicRepository));
        _questionRepository = questionRepository ?? throw new ArgumentNullException(nameof(questionRepository));

        LoadCommand = ReactiveCommand.CreateFromTask(LoadAsync);
        ToggleCategoryMarkCommand = ReactiveCommand.Create<int>(ToggleCategoryMark);
        ToggleProblematicFilterCommand = ReactiveCommand.Create(ToggleProblematicFilter);

        var canTrainCategory = StateObservable
            .Select(s => s.SelectedCategory != null && s.SelectedCategory.TotalCount > 0);

        var canTrainTopic = StateObservable
            .Select(s => s.SelectedTopic != null && s.SelectedTopic.TotalCount > 0);

        var canTrainMarked = StateObservable
            .Select(s => s.Categories.Any(c => c.IsMarked && c.TotalCount > 0));

        TrainSelectedCategoryCommand = ReactiveCommand.CreateFromTask(_ => StartTrainingSelectedCategoryAsync(), canTrainCategory);
        TrainSelectedTopicCommand = ReactiveCommand.CreateFromTask(_ => StartTrainingSelectedTopicAsync(), canTrainTopic);
        TrainMarkedCategoriesCommand = ReactiveCommand.CreateFromTask(_ => StartTrainingMarkedCategoriesAsync(), canTrainMarked);

        LoadCommand.Execute().Subscribe();
    }

    private async Task LoadAsync()
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = string.Empty });
        try
        {
            var categories = await _categoryRepository.GetAllAsync();
            var questions = await _questionRepository.GetAllAsync();

            _allCategoriesList = categories.ToList();
            _allQuestionsList = questions.ToList();

            var allCategoryInfos = _allCategoriesList.Select(c =>
            {
                int count = _allQuestionsList.Count(q => q.CategoryId == c.Id && q.Status != Core.Domain.Enums.QuestionStatus.TO_ARCHIVE);
                return new CategoryTrainingInfo(c, count, false);
            }).ToImmutableList();

            UpdateState(s => s with
            {
                IsLoading = false,
                AllCategories = allCategoryInfos
            });

            RefreshCategoryInfos();

            await UpdateSelectedCategoryAfterLoadAsync();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Failed to load training selection: {ex.Message}" });
        }
    }

    private async Task UpdateSelectedCategoryAfterLoadAsync()
    {
        if (State.SelectedCategory == null)
            return;

        var currentSelectedCat = State.AllCategories.FirstOrDefault(c => c.Category.Id == State.SelectedCategory.Category.Id);
        if (currentSelectedCat != null)
        {
            UpdateState(s => s with { SelectedCategory = currentSelectedCat });
            await LoadTopicsForCategoryAsync(currentSelectedCat.Category.Id);
        }
    }

    public async Task SetSelectedCategoryAsync(CategoryTrainingInfo? categoryInfo)
    {
        UpdateState(s => s with { SelectedCategory = categoryInfo, SelectedTopic = null, Topics = ImmutableList<TopicTrainingInfo>.Empty });
        if (categoryInfo != null)
        {
            await LoadTopicsForCategoryAsync(categoryInfo.Category.Id);
        }
    }

    private async Task LoadTopicsForCategoryAsync(int categoryId)
    {
        try
        {
            var topics = await _topicRepository.GetByCategoryIdAsync(categoryId);
            var questions = await _questionRepository.GetByCategoryIdAsync(categoryId);
            var questionList = questions.ToList();

            var topicInfos = topics.Select(t =>
            {
                int count = questionList.Count(q => q.TopicId == t.Id && q.Status != Core.Domain.Enums.QuestionStatus.TO_ARCHIVE);
                return new TopicTrainingInfo(t, count);
            }).ToImmutableList();

            UpdateState(s => s with { Topics = topicInfos });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { ErrorMessage = $"Failed to load topics: {ex.Message}" });
        }
    }

    public void SetSelectedTopic(TopicTrainingInfo? topicInfo)
    {
        UpdateState(s => s with { SelectedTopic = topicInfo });
    }

    private void ToggleCategoryMark(int categoryId)
    {
        var nextMarkedIds = State.MarkedCategoryIds.Contains(categoryId)
            ? State.MarkedCategoryIds.Remove(categoryId)
            : State.MarkedCategoryIds.Add(categoryId);

        UpdateState(s => s with { MarkedCategoryIds = nextMarkedIds });
        RefreshCategoryInfos();
    }

    private void ToggleProblematicFilter()
    {
        UpdateState(s => s with { FilterProblematic = !s.FilterProblematic });
        RefreshCategoryInfos();
    }

    private void RefreshCategoryInfos()
    {
        DateTime tomorrow = DateTime.Today.AddDays(1);

        int problematicCount = _allQuestionsList.Count(q =>
            q.Status != Core.Domain.Enums.QuestionStatus.TO_ARCHIVE &&
            q.Interval == 1 &&
            q.NextReview.Date <= tomorrow.Date &&
            q.IsProblematic);

        var filteredQuestions = _allQuestionsList.Where(q =>
            q.Status != Core.Domain.Enums.QuestionStatus.TO_ARCHIVE &&
            q.Interval == 1 &&
            q.NextReview.Date <= tomorrow.Date &&
            (!State.FilterProblematic || q.IsProblematic)
        ).ToList();

        var markedIds = State.MarkedCategoryIds;

        var categoryInfos = _allCategoriesList
            .Select(c =>
            {
                int count = filteredQuestions.Count(q => q.CategoryId == c.Id);
                bool isMarked = markedIds.Contains(c.Id);
                return new CategoryTrainingInfo(c, count, isMarked);
            })
            .Where(info => info.TotalCount > 0)
            .ToImmutableList();

        UpdateState(s => s with
        {
            Categories = categoryInfos,
            ProblematicCount = problematicCount
        });
    }

    private async Task StartTrainingSelectedCategoryAsync()
    {
        if (State.SelectedCategory == null)
            throw new InvalidOperationException("No category selected.");

        var questions = await _questionRepository.GetByCategoryIdAsync(State.SelectedCategory.Category.Id);
        var filtered = questions.Where(q => q.Status != Core.Domain.Enums.QuestionStatus.TO_ARCHIVE).ToList();

        HostScreen.Router.Navigate.Execute(new TrainingSessionViewModel(HostScreen, filtered, $"Category: {State.SelectedCategory.Category.Name}", "Training all active questions")).Subscribe();
    }

    private async Task StartTrainingSelectedTopicAsync()
    {
        if (State.SelectedTopic == null)
            throw new InvalidOperationException("No topic selected.");

        var questions = await _questionRepository.GetByTopicIdAsync(State.SelectedTopic.Topic.Id);
        var filtered = questions.Where(q => q.Status != Core.Domain.Enums.QuestionStatus.TO_ARCHIVE).ToList();

        HostScreen.Router.Navigate.Execute(new TrainingSessionViewModel(HostScreen, filtered, $"Topic: {State.SelectedTopic.Topic.Name}", $"Category: {State.SelectedCategory?.Category.Name}")).Subscribe();
    }

    private async Task StartTrainingMarkedCategoriesAsync()
    {
        var markedCategories = State.Categories.Where(c => c.IsMarked).ToList();
        if (!markedCategories.Any())
            throw new InvalidOperationException("No categories marked.");

        var allQuestions = await LoadFilteredQuestionsForCategoriesAsync(markedCategories);

        var title = State.FilterProblematic ? "Problematic Tomorrow" : "Tomorrow's Reviews";
        var subtitle = BuildCategorySubtitle(markedCategories);

        HostScreen.Router.Navigate.Execute(new TrainingSessionViewModel(HostScreen, allQuestions, title, subtitle)).Subscribe();
    }

    private async Task<List<Question>> LoadFilteredQuestionsForCategoriesAsync(List<CategoryTrainingInfo> categories)
    {
        var allQuestions = new List<Question>();
        foreach (var cat in categories)
        {
            var questions = await LoadFilteredQuestionsForCategoryAsync(cat.Category.Id);
            allQuestions.AddRange(questions);
        }
        return allQuestions;
    }

    private async Task<List<Question>> LoadFilteredQuestionsForCategoryAsync(int categoryId)
    {
        var questions = await _questionRepository.GetByCategoryIdAsync(categoryId);
        DateTime tomorrow = DateTime.Today.AddDays(1);
        return questions.Where(q =>
            q.Status != Core.Domain.Enums.QuestionStatus.TO_ARCHIVE &&
            q.Interval == 1 &&
            q.NextReview.Date <= tomorrow.Date &&
            (!State.FilterProblematic || q.IsProblematic)
        ).ToList();
    }

    private string BuildCategorySubtitle(List<CategoryTrainingInfo> categories)
    {
        var subtitle = string.Join(", ", categories.Select(c => c.Category.Name));
        if (subtitle.Length > 60)
        {
            return subtitle.Substring(0, 57) + "...";
        }
        return subtitle;
    }
}

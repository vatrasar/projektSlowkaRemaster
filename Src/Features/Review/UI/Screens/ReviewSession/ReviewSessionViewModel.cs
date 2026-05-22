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
using ProjektSlowkaRemasterd.Src.Features.Review.UI.Screens.ReviewSelection;

namespace ProjektSlowkaRemasterd.Src.Features.Review.UI.Screens.ReviewSession;

using Question = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Question;
using Media = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Media;

public class ReviewQueueItem
{
    public Question Question { get; set; }
    public string Direction { get; set; } // "Q->A" or "A->Q"

    public ReviewQueueItem(Question question, string direction)
    {
        Question = question;
        Direction = direction;
    }
}

/// <summary>
/// ViewModel for the Active Review Session.
/// Handles spaced repetition card presentation, bidirectional queue, and group batching.
/// </summary>
public class ReviewSessionViewModel : ViewModelBase<ReviewSessionState>, IRoutableViewModel
{
    public string? UrlPathSegment => "review-session";
    public IScreen HostScreen { get; }

    private readonly ICategoryRepository _categoryRepository;
    private readonly ITopicRepository _topicRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IStatisticsRepository _statisticsRepository;
    private readonly IMediaRepository _mediaRepository;

    private readonly int _categoryId;
    private readonly int? _topicId;
    private readonly List<ReviewQueueItem> _queue = new();
    private bool _isReverseMode = false;

    public ReactiveCommand<Unit, Unit> LoadSessionCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowAnswerCommand { get; }
    public ReactiveCommand<Unit, Unit> KnowCommand { get; }
    public ReactiveCommand<Unit, Unit> UnknownCommand { get; }
    public ReactiveCommand<Unit, Unit> ArchiveCommand { get; }
    public ReactiveCommand<Unit, IRoutableViewModel> BackToSelectionCommand { get; }

    public ReviewSessionViewModel(IScreen hostScreen, int categoryId, int? topicId = null)
        : this(
            hostScreen,
            categoryId,
            topicId,
            Locator.Current.GetService<ICategoryRepository>()!,
            Locator.Current.GetService<ITopicRepository>()!,
            Locator.Current.GetService<IQuestionRepository>()!,
            Locator.Current.GetService<IStatisticsRepository>()!,
            Locator.Current.GetService<IMediaRepository>()!)
    {
    }

    public ReviewSessionViewModel(
        IScreen hostScreen,
        int categoryId,
        int? topicId,
        ICategoryRepository categoryRepository,
        ITopicRepository topicRepository,
        IQuestionRepository questionRepository,
        IStatisticsRepository statisticsRepository,
        IMediaRepository mediaRepository)
        : base(new ReviewSessionState())
    {
        HostScreen = hostScreen;
        _categoryId = categoryId;
        _topicId = topicId;
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _topicRepository = topicRepository ?? throw new ArgumentNullException(nameof(topicRepository));
        _questionRepository = questionRepository ?? throw new ArgumentNullException(nameof(questionRepository));
        _statisticsRepository = statisticsRepository ?? throw new ArgumentNullException(nameof(statisticsRepository));
        _mediaRepository = mediaRepository ?? throw new ArgumentNullException(nameof(mediaRepository));

        LoadSessionCommand = ReactiveCommand.CreateFromTask(LoadSessionAsync);
        ShowAnswerCommand = ReactiveCommand.Create(ShowAnswer);
        KnowCommand = ReactiveCommand.CreateFromTask(OnKnowAsync);
        UnknownCommand = ReactiveCommand.CreateFromTask(OnUnknownAsync);
        ArchiveCommand = ReactiveCommand.CreateFromTask(OnArchiveAsync);

        BackToSelectionCommand = ReactiveCommand.CreateFromObservable(() =>
            HostScreen.Router.Navigate.Execute(new ReviewSelectionViewModel(HostScreen)));

        LoadSessionCommand.Execute().Subscribe();
    }

    private async Task LoadSessionAsync()
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = string.Empty });
        try
        {
            var category = await _categoryRepository.GetByIdAsync(_categoryId);
            if (category == null)
            {
                UpdateState(s => s with { IsLoading = false, ErrorMessage = "Category not found." });
                return;
            }

            _isReverseMode = category.Reverse;
            UpdateState(s => s with { CategoryName = category.Name, IsBidirectional = _isReverseMode });

            if (_topicId.HasValue)
            {
                var topic = await _topicRepository.GetByIdAsync(_topicId.Value);
                if (topic != null)
                {
                    UpdateState(s => s with { TopicName = topic.Name });
                }
            }

            var today = DateTime.Today;
            IEnumerable<Question> dueQuestions;

            if (_topicId.HasValue)
            {
                dueQuestions = await _questionRepository.GetReviewQuestionsByTopicAsync(_topicId.Value, today);
            }
            else
            {
                dueQuestions = await _questionRepository.GetReviewQuestionsByCategoryAsync(_categoryId, today);
            }

            var dueActive = dueQuestions
                .Where(q => q.Status != QuestionStatus.TO_ARCHIVE)
                .ToList();

            var addedIds = new HashSet<int>();
            _queue.Clear();

            foreach (var q in dueActive)
            {
                if (addedIds.Contains(q.Id)) continue;

                if (q.GroupId.HasValue)
                {
                    // Load the entire group of related questions
                    var groupQuestions = await _questionRepository.GetByGroupIdAsync(q.GroupId.Value);
                    var activeGroupQuestions = groupQuestions
                        .Where(g => g.Status != QuestionStatus.TO_ARCHIVE)
                        .ToList();

                    foreach (var gq in activeGroupQuestions)
                    {
                        if (addedIds.Contains(gq.Id)) continue;

                        var dir = (_isReverseMode && gq.Status == QuestionStatus.KNOWN_ONE_SIDE) ? "A->Q" : "Q->A";
                        _queue.Add(new ReviewQueueItem(gq, dir));
                        addedIds.Add(gq.Id);
                    }
                }
                else
                {
                    var dir = (_isReverseMode && q.Status == QuestionStatus.KNOWN_ONE_SIDE) ? "A->Q" : "Q->A";
                    _queue.Add(new ReviewQueueItem(q, dir));
                    addedIds.Add(q.Id);
                }
            }

            UpdateState(s => s with 
            { 
                TotalQuestionsCount = _queue.Count,
                CurrentQuestionIndex = _queue.Any() ? 1 : 0,
                IsLoading = false
            });

            await ShowCurrentCardAsync();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Failed to start review: {ex.Message}" });
        }
    }

    private async Task ShowCurrentCardAsync()
    {
        if (!_queue.Any())
        {
            UpdateState(s => s with 
            { 
                CurrentQuestion = null, 
                IsFinished = true,
                QuestionText = string.Empty,
                AnswerText = string.Empty,
                QuestionMedia = ImmutableList<Media>.Empty,
                AnswerMedia = ImmutableList<Media>.Empty
            });
            return;
        }

        var item = _queue[0];
        var q = item.Question;

        UpdateState(s => s with { IsLoading = true });
        try
        {
            var mediaList = (await _mediaRepository.GetByQuestionIdAsync(q.Id)).ToList();
            var qMedia = mediaList.Where(m => m.Status == MediaStatus.QUESTION).ToImmutableList();
            var aMedia = mediaList.Where(m => m.Status == MediaStatus.ANSWER).ToImmutableList();

            bool isAQ = item.Direction == "A->Q";
            UpdateState(s => s with
            {
                CurrentQuestion = q,
                CurrentDirection = item.Direction,
                IsAnswerVisible = q.IsNotion,
                QuestionText = q.IsNotion ? string.Empty : (isAQ ? q.AnswerText : q.QuestionText),
                AnswerText = isAQ ? q.QuestionText : q.AnswerText,
                QuestionMedia = isAQ ? aMedia : qMedia,
                AnswerMedia = isAQ ? qMedia : aMedia,
                IsLoading = false
            });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Error displaying card: {ex.Message}" });
        }
    }

    private void ShowAnswer()
    {
        UpdateState(s => s with { IsAnswerVisible = true });
    }

    private async Task OnKnowAsync()
    {
        if (!_queue.Any()) return;

        var current = _queue[0];
        var q = current.Question;

        UpdateState(s => s with { IsLoading = true });
        try
        {
            if (q.GroupId.HasValue)
            {
                // Group evaluation rules
                if (_isReverseMode)
                {
                    if (current.Direction == "Q->A")
                    {
                        q.Status = QuestionStatus.KNOWN_ONE_SIDE;
                        await _questionRepository.UpdateAsync(q);
                        _queue.RemoveAt(0);
                        InsertGroupItemAsAQ(q);
                    }
                    else
                    {
                        q.Status = QuestionStatus.KNOWN;
                        await _questionRepository.UpdateAsync(q);
                        _queue.RemoveAt(0);
                    }
                }
                else
                {
                    q.Status = QuestionStatus.KNOWN;
                    await _questionRepository.UpdateAsync(q);
                    _queue.RemoveAt(0);
                }

                // Check if the entire group is now KNOWN
                var groupQuestions = (await _questionRepository.GetByGroupIdAsync(q.GroupId.Value))
                    .Where(g => g.Status != QuestionStatus.TO_ARCHIVE)
                    .ToList();

                if (groupQuestions.All(g => g.Status == QuestionStatus.KNOWN))
                {
                    // Promote all group questions
                    foreach (var gq in groupQuestions)
                    {
                        var stats = await GetOrCreateStatisticsAsync(gq.StatisticsId);
                        await PromoteQuestionAsync(gq, stats);
                    }
                }
            }
            else
            {
                // Non-grouped question rules
                if (_isReverseMode)
                {
                    if (current.Direction == "Q->A")
                    {
                        q.Status = QuestionStatus.KNOWN_ONE_SIDE;
                        await _questionRepository.UpdateAsync(q);
                        _queue.RemoveAt(0);
                        _queue.Add(new ReviewQueueItem(q, "A->Q"));
                    }
                    else
                    {
                        var stats = await GetOrCreateStatisticsAsync(q.StatisticsId);
                        await PromoteQuestionAsync(q, stats);
                        _queue.RemoveAt(0);
                    }
                }
                else
                {
                    var stats = await GetOrCreateStatisticsAsync(q.StatisticsId);
                    await PromoteQuestionAsync(q, stats);
                    _queue.RemoveAt(0);
                }
            }

            UpdateState(s => s with { CurrentQuestionIndex = s.CurrentQuestionIndex + 1 });
            await ShowCurrentCardAsync();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Error processing Know: {ex.Message}" });
        }
    }

    private async Task OnUnknownAsync()
    {
        if (!_queue.Any()) return;

        var current = _queue[0];
        var q = current.Question;

        UpdateState(s => s with { IsLoading = true });
        try
        {
            if (q.GroupId.HasValue)
            {
                // Remove all remaining items of this related group from the session queue
                _queue.RemoveAll(i => i.Question.GroupId == q.GroupId.Value);

                // Fetch all group questions and reset/penalize them
                var groupQuestions = (await _questionRepository.GetByGroupIdAsync(q.GroupId.Value))
                    .Where(g => g.Status != QuestionStatus.TO_ARCHIVE)
                    .ToList();

                foreach (var gq in groupQuestions)
                {
                    var stats = await GetOrCreateStatisticsAsync(gq.StatisticsId);
                    await PenalizeQuestionAsync(gq, stats);
                }
            }
            else
            {
                // Penalize single question
                var stats = await GetOrCreateStatisticsAsync(q.StatisticsId);
                await PenalizeQuestionAsync(q, stats);
                _queue.RemoveAt(0);
            }

            UpdateState(s => s with { CurrentQuestionIndex = s.CurrentQuestionIndex + 1 });
            await ShowCurrentCardAsync();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Error processing Unknown: {ex.Message}" });
        }
    }

    private async Task OnArchiveAsync()
    {
        if (!_queue.Any()) return;

        var current = _queue[0];
        var q = current.Question;

        UpdateState(s => s with { IsLoading = true });
        try
        {
            q.Status = QuestionStatus.TO_ARCHIVE;
            q.Interval = 360;
            q.NextReview = DateTime.Today.AddDays(1000000);
            await _questionRepository.UpdateAsync(q);

            _queue.RemoveAt(0);

            UpdateState(s => s with { CurrentQuestionIndex = s.CurrentQuestionIndex + 1 });
            await ShowCurrentCardAsync();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Error archiving: {ex.Message}" });
        }
    }

    private void InsertGroupItemAsAQ(Question question)
    {
        int lastIndex = -1;
        for (int i = 0; i < _queue.Count; i++)
        {
            if (_queue[i].Question.GroupId == question.GroupId)
            {
                lastIndex = i;
            }
        }

        var newItem = new ReviewQueueItem(question, "A->Q");
        if (lastIndex >= 0)
        {
            _queue.Insert(lastIndex + 1, newItem);
        }
        else
        {
            _queue.Insert(0, newItem);
        }
    }

    private async Task<Statistics> GetOrCreateStatisticsAsync(int statsId)
    {
        var stats = await _statisticsRepository.GetByIdAsync(statsId);
        if (stats == null)
        {
            stats = new Statistics { Id = statsId, Failures = 0 };
            await _statisticsRepository.AddAsync(stats);
        }
        return stats;
    }

    private async Task PromoteQuestionAsync(Question q, Statistics stats)
    {
        if (q.Interval >= 30)
        {
            q.Status = QuestionStatus.TO_ARCHIVE;
            q.Interval = 360;
            q.NextReview = DateTime.Today.AddDays(1000000);
        }
        else
        {
            q.Interval = GetNextInterval(q.Interval, stats.Failures);
            q.NextReview = DateTime.Today.AddDays(q.Interval);
            q.Status = QuestionStatus.UNCHECKED;
        }
        await _questionRepository.UpdateAsync(q);
    }

    private async Task PenalizeQuestionAsync(Question q, Statistics stats)
    {
        if (q.Interval >= 10)
        {
            stats.Failures += 1;
            await _statisticsRepository.UpdateAsync(stats);
        }

        if (stats.Failures >= 3)
        {
            q.Status = QuestionStatus.TO_ARCHIVE;
            q.Interval = 360;
            q.NextReview = DateTime.Today.AddDays(1000000);
        }
        else
        {
            q.Interval = 1;
            q.NextReview = DateTime.Today.AddDays(1);
            q.Status = QuestionStatus.UNCHECKED;
        }
        await _questionRepository.UpdateAsync(q);
    }

    public static int GetNextInterval(int currentInterval, int failures)
    {
        var isHardMode = failures >= 2;
        var sequence = isHardMode
            ? new[] { 1, 3, 6, 10, 20, 30 }
            : new[] { 1, 3, 10, 30 };

        foreach (var val in sequence)
        {
            if (val > currentInterval)
                return val;
        }
        return 30;
    }
}

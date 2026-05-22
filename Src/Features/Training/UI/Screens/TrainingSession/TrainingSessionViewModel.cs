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
using ProjektSlowkaRemasterd.Src.Features.Training.UI.Screens.TrainingSelection;

namespace ProjektSlowkaRemasterd.Src.Features.Training.UI.Screens.TrainingSession;

using Question = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Question;
using Media = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Media;
using Category = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Category;

public class TrainingQueueItem
{
    public Question Question { get; }
    public int State { get; set; } // 0, 1, 2, 3
    public string Direction { get; set; } // "Q->A" or "A->Q"

    public TrainingQueueItem(Question question, int state = 0, string direction = "Q->A")
    {
        Question = question;
        State = state;
        Direction = direction;
    }
}

/// <summary>
/// ViewModel for the active Training Session.
/// Implements the 0-3 queue state transition machine for both standard and bidirectional modes.
/// </summary>
public class TrainingSessionViewModel : ViewModelBase<TrainingSessionState>, IRoutableViewModel
{
    public string? UrlPathSegment => "training-session";
    public IScreen HostScreen { get; }

    private readonly ICategoryRepository _categoryRepository;
    private readonly IMediaRepository _mediaRepository;

    private readonly List<Question> _initialQuestions;
    private readonly List<TrainingQueueItem> _queue = new();
    private Dictionary<int, Category> _categoryMap = new();

    private int _initialTotalCount = 0;
    private int _completedCount = 0;

    public ReactiveCommand<Unit, Unit> LoadSessionCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowAnswerCommand { get; }
    public ReactiveCommand<Unit, Unit> KnowCommand { get; }
    public ReactiveCommand<Unit, Unit> UnknownCommand { get; }
    public ReactiveCommand<Unit, IRoutableViewModel> BackToSelectionCommand { get; }

    public TrainingSessionViewModel(IScreen hostScreen, List<Question> questions, string title, string subtitle)
        : this(
            hostScreen,
            questions,
            title,
            subtitle,
            Locator.Current.GetService<ICategoryRepository>()!,
            Locator.Current.GetService<IMediaRepository>()!)
    {
    }

    public TrainingSessionViewModel(
        IScreen hostScreen,
        List<Question> questions,
        string title,
        string subtitle,
        ICategoryRepository categoryRepository,
        IMediaRepository mediaRepository)
        : base(new TrainingSessionState { Title = title, Subtitle = subtitle })
    {
        HostScreen = hostScreen;
        _initialQuestions = questions ?? new List<Question>();
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _mediaRepository = mediaRepository ?? throw new ArgumentNullException(nameof(mediaRepository));

        LoadSessionCommand = ReactiveCommand.CreateFromTask(LoadSessionAsync);
        ShowAnswerCommand = ReactiveCommand.Create(ShowAnswer);
        KnowCommand = ReactiveCommand.CreateFromTask(OnKnowAsync);
        UnknownCommand = ReactiveCommand.CreateFromTask(OnUnknownAsync);

        BackToSelectionCommand = ReactiveCommand.CreateFromObservable(() =>
            HostScreen.Router.Navigate.Execute(new TrainingSelectionViewModel(HostScreen)));

        LoadSessionCommand.Execute().Subscribe();
    }

    private async Task LoadSessionAsync()
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = string.Empty });
        try
        {
            var categories = await _categoryRepository.GetAllAsync();
            _categoryMap = categories.ToDictionary(c => c.Id);

            if (!_initialQuestions.Any())
            {
                UpdateState(s => s with { IsLoading = false, IsFinished = true });
                return;
            }

            _initialTotalCount = _initialQuestions.Count;
            _completedCount = 0;

            // Build initial queue (State 0, direction Q->A)
            _queue.Clear();
            foreach (var q in _initialQuestions)
            {
                _queue.Add(new TrainingQueueItem(q, 0, "Q->A"));
            }

            UpdateState(s => s with
            {
                IsLoading = false,
                TotalQuestionsCount = _initialTotalCount,
                CurrentQuestionIndex = 1
            });

            await ShowCurrentCardAsync();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Failed to load training session: {ex.Message}" });
        }
    }

    private void ShowAnswer()
    {
        UpdateState(s => s with { IsAnswerVisible = true });
    }

    private async Task OnKnowAsync()
    {
        if (!_queue.Any()) return;

        UpdateState(s => s with { IsLoading = true });
        try
        {
            var current = _queue[0];
            bool reverseMode = false;
            if (_categoryMap.TryGetValue(current.Question.CategoryId, out var category))
            {
                reverseMode = category.Reverse;
            }

            if (reverseMode)
            {
                if (current.State == 0)
                {
                    if (current.Direction == "Q->A")
                    {
                        // State remains 0, goes to end with direction A->Q
                        _queue.RemoveAt(0);
                        current.Direction = "A->Q";
                        _queue.Add(current);
                    }
                    else
                    {
                        // A->Q Know -> State 3 (Completed)
                        _queue.RemoveAt(0);
                        _completedCount++;
                    }
                }
                else if (current.State == 1)
                {
                    // State 1, direction Q->A -> State 2, shift 10 positions back (without reversing to A->Q)
                    _queue.RemoveAt(0);
                    current.State = 2;
                    ShiftBack(current, 10);
                }
                else if (current.State == 2)
                {
                    if (current.Direction == "Q->A")
                    {
                        // State 2, direction Q->A -> State 0, goes to end and appears as A->Q
                        _queue.RemoveAt(0);
                        current.State = 0;
                        current.Direction = "A->Q";
                        _queue.Add(current);
                    }
                    else
                    {
                        // A->Q Know -> State 3 (Completed)
                        _queue.RemoveAt(0);
                        _completedCount++;
                    }
                }
            }
            else
            {
                // One-sided mode
                if (current.State == 0 || current.State == 2)
                {
                    _queue.RemoveAt(0);
                    _completedCount++;
                }
                else if (current.State == 1)
                {
                    _queue.RemoveAt(0);
                    current.State = 2;
                    ShiftBack(current, 10);
                }
            }

            UpdateState(s => s with
            {
                CurrentQuestionIndex = Math.Min(_initialTotalCount, _completedCount + 1)
            });

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

        UpdateState(s => s with { IsLoading = true });
        try
        {
            var current = _queue[0];
            bool reverseMode = false;
            if (_categoryMap.TryGetValue(current.Question.CategoryId, out var category))
            {
                reverseMode = category.Reverse;
            }

            _queue.RemoveAt(0);
            current.State = 1;

            if (reverseMode)
            {
                // Reset direction to Q->A and shift 3 positions back
                current.Direction = "Q->A";
            }

            ShiftBack(current, 3);

            await ShowCurrentCardAsync();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Error processing Unknown: {ex.Message}" });
        }
    }

    private void ShiftBack(TrainingQueueItem item, int positions)
    {
        int insertIndex = Math.Min(_queue.Count, positions);
        _queue.Insert(insertIndex, item);
    }

    private async Task ShowCurrentCardAsync()
    {
        if (!_queue.Any())
        {
            UpdateState(s => s with { IsLoading = false, IsFinished = true, CurrentQuestion = null });
            return;
        }

        var current = _queue[0];
        var q = current.Question;

        // Load media files for this question
        var mediaItems = await _mediaRepository.GetByQuestionIdAsync(q.Id);
        var mediaList = mediaItems.ToList();
        var qMedia = mediaList.Where(m => m.Status == MediaStatus.QUESTION).ToImmutableList();
        var aMedia = mediaList.Where(m => m.Status == MediaStatus.ANSWER).ToImmutableList();

        bool reverseMode = false;
        if (_categoryMap.TryGetValue(q.CategoryId, out var category))
        {
            reverseMode = category.Reverse;
        }

        // In A->Q direction, question text and answer text are swapped
        string qText = current.Direction == "A->Q" ? q.AnswerText : q.QuestionText;
        string aText = current.Direction == "A->Q" ? q.QuestionText : q.AnswerText;

        // In A->Q, media is also swapped
        var questionMedia = current.Direction == "A->Q" ? aMedia : qMedia;
        var answerMedia = current.Direction == "A->Q" ? qMedia : aMedia;

        UpdateState(s => s with
        {
            IsLoading = false,
            CurrentQuestion = q,
            QuestionText = qText,
            AnswerText = aText,
            QuestionMedia = questionMedia,
            AnswerMedia = answerMedia,
            IsAnswerVisible = false,
            CurrentDirection = current.Direction,
            IsBidirectional = reverseMode
        });
    }
}

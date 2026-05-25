using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ReactiveUI;
using Splat;
using ProjektSlowkaRemasterd.Src.Core.Mvvm;
using ProjektSlowkaRemasterd.Src.Core.Config;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;
using ProjektSlowkaRemasterd.Src.Core.Domain.RepositoryContracts;
using ProjektSlowkaRemasterd.Src.Core.Domain.Enums;

namespace ProjektSlowkaRemasterd.Src.Features.Question.UI.Screens.QuestionEditor;

using Category = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Category;
using Question = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Question;



public record TempImage(string? SourcePath, byte[]? Bytes, MediaStatus Status);

public class QuestionEditorViewModel : ViewModelBase<QuestionEditorState>, IRoutableViewModel
{
    public string? UrlPathSegment => "question-editor";
    public IScreen HostScreen { get; }

    private readonly ICategoryRepository _categoryRepository;
    private readonly ITopicRepository _topicRepository;
    private readonly ISectionRepository _sectionRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IStatisticsRepository _statisticsRepository;
    private readonly IMediaRepository _mediaRepository;
    private readonly AppConfig _config;

    private readonly List<TempImage> _tempImages = new();
    private readonly List<Media> _existingMediaToDelete = new();

    public ReactiveCommand<Unit, Unit> LoadDataCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveQuestionCommand { get; }
    public ReactiveCommand<Unit, IRoutableViewModel> GoBackCommand { get; }

    private readonly int? _prefilledCategoryId;

    public QuestionEditorViewModel(IScreen hostScreen, int? categoryId = null, int? questionId = null)
        : this(
            hostScreen, 
            Locator.Current.GetService<ICategoryRepository>()!,
            Locator.Current.GetService<ITopicRepository>()!,
            Locator.Current.GetService<ISectionRepository>()!,
            Locator.Current.GetService<IQuestionRepository>()!,
            Locator.Current.GetService<IStatisticsRepository>()!,
            Locator.Current.GetService<IMediaRepository>()!,
            categoryId,
            questionId)
    {
    }

    public QuestionEditorViewModel(
        IScreen hostScreen,
        ICategoryRepository categoryRepository,
        ITopicRepository topicRepository,
        ISectionRepository sectionRepository,
        IQuestionRepository questionRepository,
        IStatisticsRepository statisticsRepository,
        IMediaRepository mediaRepository,
        int? categoryId = null,
        int? questionId = null,
        AppConfig? config = null)
        : base(new QuestionEditorState { QuestionId = questionId })
    {
        HostScreen = hostScreen;
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _topicRepository = topicRepository ?? throw new ArgumentNullException(nameof(topicRepository));
        _sectionRepository = sectionRepository ?? throw new ArgumentNullException(nameof(sectionRepository));
        _questionRepository = questionRepository ?? throw new ArgumentNullException(nameof(questionRepository));
        _statisticsRepository = statisticsRepository ?? throw new ArgumentNullException(nameof(statisticsRepository));
        _mediaRepository = mediaRepository ?? throw new ArgumentNullException(nameof(mediaRepository));
        _config = config ?? Locator.Current.GetService<IOptions<AppConfig>>()!.Value;

        _prefilledCategoryId = categoryId;

        LoadDataCommand = ReactiveCommand.CreateFromTask(LoadDataAsync);
        SaveQuestionCommand = ReactiveCommand.CreateFromTask(SaveQuestionAsync);
        GoBackCommand = ReactiveCommand.CreateFromObservable(() => HostScreen.Router.NavigateBack.Execute());

        // Load data on startup
        LoadDataCommand.Execute().Subscribe();
    }

    private async Task LoadDataAsync()
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = string.Empty, SuccessMessage = string.Empty });

        try
        {
            var categories = (await _categoryRepository.GetAllAsync()).ToList();
            var stateCategories = categories.ToImmutableList();
            
            UpdateState(s => s with { Categories = stateCategories });

            // Load last added question for preview if category was prefilled (or use the prefilled category to display last added)
            int? lastAddedCatId = _prefilledCategoryId;
            if (State.QuestionId.HasValue && State.QuestionId.Value > 0)
            {
                // Editing mode
                var question = await _questionRepository.GetByIdAsync(State.QuestionId.Value);
                if (question != null)
                {
                    lastAddedCatId = question.CategoryId;
                    
                    var selCategory = categories.FirstOrDefault(c => c.Id == question.CategoryId);
                    var topics = selCategory != null 
                        ? (await _topicRepository.GetByCategoryIdAsync(selCategory.Id)).ToList()
                        : new List<Topic>();
                    
                    var selTopic = topics.FirstOrDefault(t => t.Id == question.TopicId);
                    
                    var sections = selTopic != null
                        ? (await _sectionRepository.GetByTopicIdAsync(selTopic.Id)).ToList()
                        : new List<Section>();
                        
                    var selSection = sections.FirstOrDefault(s => s.Id == question.SectionId);

                    // Load media
                    var mediaList = await _mediaRepository.GetByQuestionIdAsync(question.Id);
                    var qMedia = mediaList.Where(m => m.Status == MediaStatus.QUESTION).Select(m => m.Filename).ToImmutableList();
                    var aMedia = mediaList.Where(m => m.Status == MediaStatus.ANSWER).Select(m => m.Filename).ToImmutableList();

                    UpdateState(s => s with
                    {
                        QuestionText = question.QuestionText,
                        AnswerText = question.AnswerText,
                        SelectedCategory = selCategory,
                        Topics = topics.ToImmutableList(),
                        SelectedTopic = selTopic,
                        Sections = sections.ToImmutableList(),
                        SelectedSection = selSection,
                        IsNote = question.IsNotion,
                        IsProblematic = question.IsProblematic,
                        GroupId = question.GroupId,
                        QuestionImages = qMedia,
                        AnswerImages = aMedia
                    });
                }
            }
            else
            {
                // Adding mode
                if (_prefilledCategoryId.HasValue)
                {
                    var selCategory = categories.FirstOrDefault(c => c.Id == _prefilledCategoryId.Value);
                    if (selCategory != null)
                    {
                        var topics = (await _topicRepository.GetByCategoryIdAsync(selCategory.Id)).ToList();
                        UpdateState(s => s with 
                        { 
                            SelectedCategory = selCategory, 
                            Topics = topics.ToImmutableList() 
                        });
                    }
                }
            }

            // Load last added question in the category
            if (lastAddedCatId.HasValue)
            {
                var lastAdded = await _questionRepository.GetLastAddedAsync(lastAddedCatId.Value);
                if (lastAdded != null)
                {
                    UpdateState(s => s with
                    {
                        LastAddedId = lastAdded.Id,
                        LastAddedQuestionText = lastAdded.QuestionText,
                        LastAddedAnswerText = lastAdded.AnswerText
                    });
                }
            }

            UpdateState(s => s with { IsLoading = false });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Failed to load data: {ex.Message}" });
        }
    }

    public void SetQuestionText(string text)
    {
        if (State.QuestionText == text) return;
        UpdateState(s => s with { QuestionText = text });
    }

    public void SetAnswerText(string text)
    {
        if (State.AnswerText == text) return;
        UpdateState(s => s with { AnswerText = text });
    }

    public void SetIsNote(bool isNote)
    {
        if (State.IsNote == isNote) return;
        UpdateState(s => s with { IsNote = isNote });
    }

    public void SetIsProblematic(bool isProblematic)
    {
        if (State.IsProblematic == isProblematic) return;
        UpdateState(s => s with { IsProblematic = isProblematic });
    }

    public void SetCustomSectionName(string name)
    {
        if (State.CustomSectionName == name) return;
        UpdateState(s => s with { CustomSectionName = name });
    }

    public void SetGroupId(int? groupId)
    {
        if (State.GroupId == groupId) return;
        UpdateState(s => s with { GroupId = groupId });
    }

    public void SetErrorMessage(string message)
    {
        if (State.ErrorMessage == message) return;
        UpdateState(s => s with { ErrorMessage = message });
    }

    public async Task SetSelectedCategory(Category? category)
    {
        if (category == null)
        {
            if (State.SelectedCategory != null)
            {
                UpdateState(s => s with { SelectedCategory = null, Topics = ImmutableList<Topic>.Empty, SelectedTopic = null, Sections = ImmutableList<Section>.Empty, SelectedSection = null });
            }
            return;
        }

        if (State.SelectedCategory?.Id == category.Id)
        {
            return;
        }

        UpdateState(s => s with { SelectedCategory = category, IsLoading = true });
        try
        {
            var topics = (await _topicRepository.GetByCategoryIdAsync(category.Id)).ToList();
            int? lastAddedId = null;
            string lastAddedQ = string.Empty;
            string lastAddedA = string.Empty;
            var lastAdded = await _questionRepository.GetLastAddedAsync(category.Id);
            if (lastAdded != null)
            {
                lastAddedId = lastAdded.Id;
                lastAddedQ = lastAdded.QuestionText;
                lastAddedA = lastAdded.AnswerText;
            }

            UpdateState(s => s with 
            { 
                Topics = topics.ToImmutableList(), 
                SelectedTopic = null, 
                Sections = ImmutableList<Section>.Empty, 
                SelectedSection = null, 
                IsLoading = false,
                LastAddedId = lastAddedId,
                LastAddedQuestionText = lastAddedQ,
                LastAddedAnswerText = lastAddedA
            });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Error loading topics: {ex.Message}" });
        }
    }

    public async Task SetSelectedTopic(Topic? topic)
    {
        if (topic == null)
        {
            if (State.SelectedTopic != null)
            {
                UpdateState(s => s with { SelectedTopic = null, Sections = ImmutableList<Section>.Empty, SelectedSection = null });
            }
            return;
        }

        if (State.SelectedTopic?.Id == topic.Id)
        {
            return;
        }

        UpdateState(s => s with { SelectedTopic = topic, IsLoading = true });
        try
        {
            var sections = (await _sectionRepository.GetByTopicIdAsync(topic.Id)).ToList();
            UpdateState(s => s with { Sections = sections.ToImmutableList(), SelectedSection = null, IsLoading = false });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Error loading sections: {ex.Message}" });
        }
    }

    public void SetSelectedSection(Section? section)
    {
        if (State.SelectedSection?.Id == section?.Id) return;
        UpdateState(s => s with { SelectedSection = section });
    }

    public void AddImage(string sourcePath, MediaStatus status)
    {
        _tempImages.Add(new TempImage(sourcePath, null, status));
        UpdateTempImageState();
    }

    public void AddImageBytes(byte[] bytes, MediaStatus status)
    {
        _tempImages.Add(new TempImage(null, bytes, status));
        UpdateTempImageState();
    }

    private void UpdateTempImageState()
    {
        var qTemp = _tempImages.Where(i => i.Status == MediaStatus.QUESTION)
            .Select(i => i.SourcePath ?? "[Clipboard Image]")
            .Concat(State.QuestionImages)
            .ToImmutableList();
            
        var aTemp = _tempImages.Where(i => i.Status == MediaStatus.ANSWER)
            .Select(i => i.SourcePath ?? "[Clipboard Image]")
            .Concat(State.AnswerImages)
            .ToImmutableList();

        UpdateState(s => s with { QuestionImages = qTemp, AnswerImages = aTemp });
    }

    public async Task RemoveImageAsync(string filename, MediaStatus status)
    {
        // Check if it is an existing media
        if (State.QuestionId.HasValue && State.QuestionId.Value > 0)
        {
            var mediaList = await _mediaRepository.GetByQuestionIdAsync(State.QuestionId.Value);
            var match = mediaList.FirstOrDefault(m => m.Filename == filename && m.Status == status);
            if (match != null)
            {
                _existingMediaToDelete.Add(match);
                if (status == MediaStatus.QUESTION)
                {
                    UpdateState(s => s with { QuestionImages = State.QuestionImages.Remove(filename) });
                }
                else
                {
                    UpdateState(s => s with { AnswerImages = State.AnswerImages.Remove(filename) });
                }
                return;
            }
        }

        // Otherwise remove from temp images
        var tempMatch = _tempImages.FirstOrDefault(i => i.Status == status && (i.SourcePath == filename || (filename == "[Clipboard Image]" && i.Bytes != null)));
        if (tempMatch != null)
        {
            _tempImages.Remove(tempMatch);
            UpdateTempImageState();
        }
    }

    private async Task SaveQuestionAsync()
    {
        if (State.SelectedCategory == null)
        {
            UpdateState(s => s with { ErrorMessage = "Please select a Category." });
            return;
        }

        if (!State.IsNote && string.IsNullOrWhiteSpace(State.QuestionText))
        {
            UpdateState(s => s with { ErrorMessage = "Question field cannot be empty unless 'Is note' is checked." });
            return;
        }

        if (string.IsNullOrWhiteSpace(State.AnswerText) && !State.AnswerImages.Any())
        {
            UpdateState(s => s with { ErrorMessage = "Answer field cannot be empty." });
            return;
        }

        if (State.AnswerText.Length > 10000)
        {
            UpdateState(s => s with { ErrorMessage = "Answer field cannot exceed 10000 characters." });
            return;
        }

        UpdateState(s => s with { IsLoading = true, ErrorMessage = string.Empty, SuccessMessage = string.Empty });

        try
        {
            int? finalSectionId = State.SelectedSection?.Id;

            // Handle custom section name creation
            if (State.SelectedTopic != null && !string.IsNullOrWhiteSpace(State.CustomSectionName))
            {
                var customName = State.CustomSectionName.Trim();
                var existingSection = await _sectionRepository.GetByNameAndTopicAsync(State.SelectedTopic.Id, customName);
                if (existingSection != null)
                {
                    finalSectionId = existingSection.Id;
                }
                else
                {
                    var newSec = await _sectionRepository.AddAsync(new Section { Id = 0, TopicId = State.SelectedTopic.Id, Name = customName });
                    finalSectionId = newSec.Id;
                }
            }

            Question question;
            bool isNew = !State.QuestionId.HasValue || State.QuestionId.Value == 0;

            if (isNew)
            {
                // Create Statistics record first
                var stats = await _statisticsRepository.AddAsync(new Statistics { Id = 0, Failures = 0 });
                
                // Clear existing IsLastAdded flags in the category
                await _questionRepository.ClearLastAddedFlagAsync(State.SelectedCategory.Id);

                question = new Question
                {
                    Id = 0,
                    QuestionText = State.IsNote ? string.Empty : State.QuestionText.Trim(),
                    AnswerText = State.AnswerText.Trim(),
                    CategoryId = State.SelectedCategory.Id,
                    TopicId = State.SelectedTopic?.Id,
                    SectionId = finalSectionId,
                    StatisticsId = stats.Id,
                    GroupId = State.GroupId,
                    Status = QuestionStatus.UNCHECKED,
                    IsProblematic = State.IsProblematic,
                    IsLastAdded = true,
                    IsNotion = State.IsNote,
                    NextReview = DateTime.Today,
                    Interval = 1
                };

                question = await _questionRepository.AddAsync(question);
            }
            else
            {
                var oldQuestion = await _questionRepository.GetByIdAsync(State.QuestionId!.Value);
                if (oldQuestion == null)
                {
                    UpdateState(s => s with { IsLoading = false, ErrorMessage = "Question not found to edit." });
                    return;
                }

                question = oldQuestion;
                question.QuestionText = State.IsNote ? string.Empty : State.QuestionText.Trim();
                question.AnswerText = State.AnswerText.Trim();
                question.CategoryId = State.SelectedCategory.Id;
                question.TopicId = State.SelectedTopic?.Id;
                question.SectionId = finalSectionId;
                question.GroupId = State.GroupId;
                question.IsProblematic = State.IsProblematic;
                question.IsNotion = State.IsNote;

                await _questionRepository.UpdateAsync(question);
            }

            // Handle deleted media
            foreach (var m in _existingMediaToDelete)
            {
                await _mediaRepository.DeleteAsync(m.Id);
                var fullPath = Path.Combine(_config.ResolvedMediaDirectoryPath, m.Filename);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            _existingMediaToDelete.Clear();

            // Handle new temp images
            var mediaDir = _config.ResolvedMediaDirectoryPath;
            if (!Directory.Exists(mediaDir))
            {
                Directory.CreateDirectory(mediaDir);
            }

            foreach (var img in _tempImages)
            {
                // Add Media record to get ID
                var m = new Media
                {
                    Id = 0,
                    Filename = "temp", // placeholder
                    QuestionId = question.Id,
                    Status = img.Status
                };

                m = await _mediaRepository.AddAsync(m);

                // Save file with filename matching the ID
                string ext = ".png";
                if (!string.IsNullOrEmpty(img.SourcePath))
                {
                    ext = Path.GetExtension(img.SourcePath);
                    if (string.IsNullOrEmpty(ext)) ext = ".png";
                }
                
                string destFilename = $"{m.Id}{ext}";
                string destPath = Path.Combine(mediaDir, destFilename);

                if (img.Bytes != null)
                {
                    await File.WriteAllBytesAsync(destPath, img.Bytes);
                }
                else if (!string.IsNullOrEmpty(img.SourcePath) && File.Exists(img.SourcePath))
                {
                    File.Copy(img.SourcePath, destPath, overwrite: true);
                }

                // Update filename in database
                m.Filename = destFilename;
                await _mediaRepository.UpdateAsync(m);
            }
            _tempImages.Clear();

            UpdateState(s => s with
            {
                IsLoading = false,
                SuccessMessage = isNew ? "Question added successfully!" : "Question updated successfully!",
                LastAddedId = question.Id,
                LastAddedQuestionText = question.QuestionText,
                LastAddedAnswerText = question.AnswerText,
                QuestionText = isNew ? string.Empty : State.QuestionText,
                AnswerText = isNew ? string.Empty : State.AnswerText,
                CustomSectionName = string.Empty,
                QuestionImages = isNew ? ImmutableList<string>.Empty : State.QuestionImages,
                AnswerImages = isNew ? ImmutableList<string>.Empty : State.AnswerImages
            });

            if (isNew)
            {
                // Refresh list of topics and sections
                await SetSelectedCategory(State.SelectedCategory);
            }
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Save failed: {ex.Message}" });
        }
    }
}

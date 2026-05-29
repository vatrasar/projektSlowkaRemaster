using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using ReactiveUI;
using Splat;
using ProjektSlowkaRemasterd.Src.Core.Mvvm;
using CategoryModel = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Category;
using TopicModel = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Topic;
using SectionModel = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Section;
using ProjektSlowkaRemasterd.Src.Core.Domain.RepositoryContracts;
using ProjektSlowkaRemasterd.Src.Features.Question.Domain.Services;
using ProjektSlowkaRemasterd.Src.Features.Question.Domain.UseCases;
using ProjektSlowkaRemasterd.Src.Features.Question.Resources;

namespace ProjektSlowkaRemasterd.Src.Features.Question.UI.Screens.BulkImport;

public class BulkImportViewModel : ViewModelBase<BulkImportState>, IRoutableViewModel
{
    public string? UrlPathSegment => "bulk-import";
    public IScreen HostScreen { get; }

    private readonly ICategoryRepository _categoryRepository;
    private readonly ITopicRepository _topicRepository;
    private readonly ISectionRepository _sectionRepository;
    private readonly BulkImportQuestionsUseCase _importUseCase;

    private readonly int? _prefilledCategoryId;

    public ReactiveCommand<Unit, Unit> LoadDataCommand { get; }
    public ReactiveCommand<Unit, Unit> ChooseFileCommand { get; }
    public ReactiveCommand<Unit, Unit> ImportCommand { get; }
    public ReactiveCommand<Unit, IRoutableViewModel> GoBackCommand { get; }

    public Interaction<Unit, (string filename, string content)?> ShowFilePickerInteraction { get; } = new();

    public BulkImportViewModel(IScreen hostScreen, int? categoryId = null)
        : this(
            hostScreen,
            Locator.Current.GetService<ICategoryRepository>()!,
            Locator.Current.GetService<ITopicRepository>()!,
            Locator.Current.GetService<ISectionRepository>()!,
            Locator.Current.GetService<BulkImportQuestionsUseCase>()!,
            categoryId)
    {
    }

    public BulkImportViewModel(
        IScreen hostScreen,
        ICategoryRepository categoryRepository,
        ITopicRepository topicRepository,
        ISectionRepository sectionRepository,
        BulkImportQuestionsUseCase importUseCase,
        int? categoryId = null)
        : base(new BulkImportState())
    {
        HostScreen = hostScreen;
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _topicRepository = topicRepository ?? throw new ArgumentNullException(nameof(topicRepository));
        _sectionRepository = sectionRepository ?? throw new ArgumentNullException(nameof(sectionRepository));
        _importUseCase = importUseCase ?? throw new ArgumentNullException(nameof(importUseCase));
        _prefilledCategoryId = categoryId;

        LoadDataCommand = ReactiveCommand.CreateFromTask(LoadDataAsync);
        ChooseFileCommand = ReactiveCommand.CreateFromTask(ChooseFileAsync);
        ImportCommand = ReactiveCommand.CreateFromTask(ImportAsync);
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
            UpdateState(s => s with { Categories = categories.ToImmutableList() });

            if (_prefilledCategoryId.HasValue)
            {
                var selCategory = categories.FirstOrDefault(c => c.Id == _prefilledCategoryId.Value);
                if (selCategory != null)
                {
                    await SetSelectedCategory(selCategory);
                }
            }

            UpdateState(s => s with { IsLoading = false });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Failed to load categories: {ex.Message}" });
        }
    }

    public void SetImportText(string text)
    {
        if (State.ImportText == text) return;
        UpdateState(s => s with { ImportText = text });
    }

    public async Task SetSelectedCategory(CategoryModel? category)
    {
        if (category == null)
        {
            UpdateState(s => s with { SelectedCategory = null, Topics = ImmutableList<TopicModel>.Empty, SelectedTopic = null, Sections = ImmutableList<SectionModel>.Empty, SelectedSection = null });
            return;
        }

        if (State.SelectedCategory?.Id == category.Id) return;

        UpdateState(s => s with { SelectedCategory = category, IsLoading = true });
        try
        {
            var topics = (await _topicRepository.GetByCategoryIdAsync(category.Id)).ToList();
            UpdateState(s => s with { Topics = topics.ToImmutableList(), SelectedTopic = null, Sections = ImmutableList<SectionModel>.Empty, SelectedSection = null, IsLoading = false });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Error loading topics: {ex.Message}" });
        }
    }

    public async Task SetSelectedTopic(TopicModel? topic)
    {
        if (topic == null)
        {
            UpdateState(s => s with { SelectedTopic = null, Sections = ImmutableList<SectionModel>.Empty, SelectedSection = null });
            return;
        }

        if (State.SelectedTopic?.Id == topic.Id) return;

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

    public void SetSelectedSection(SectionModel? section)
    {
        if (State.SelectedSection?.Id == section?.Id) return;
        UpdateState(s => s with { SelectedSection = section });
    }

    private async Task ChooseFileAsync()
    {
        UpdateState(s => s with { ErrorMessage = string.Empty, SuccessMessage = string.Empty });
        var result = await ShowFilePickerInteraction.Handle(Unit.Default).ToTask();
        if (result != null)
        {
            UpdateState(s => s with 
            { 
                SelectedFilePath = result.Value.filename, 
                ImportText = result.Value.content 
            });
        }
    }

    private async Task ImportAsync()
    {
        if (State.SelectedCategory == null)
        {
            UpdateState(s => s with { ErrorMessage = "Please select a Category." });
            return;
        }

        if (string.IsNullOrWhiteSpace(State.ImportText))
        {
            UpdateState(s => s with { ErrorMessage = "Please select a file or paste questions text." });
            return;
        }

        UpdateState(s => s with { IsLoading = true, ErrorMessage = string.Empty, SuccessMessage = string.Empty });

        try
        {
            var parsedQuestions = BulkQuestionParser.Parse(State.ImportText);
            if (parsedQuestions.Count == 0)
            {
                UpdateState(s => s with { IsLoading = false, ErrorMessage = "No questions found to import." });
                return;
            }

            int importedCount = await _importUseCase.ExecuteAsync(
                parsedQuestions, 
                State.SelectedCategory.Id, 
                State.SelectedTopic?.Id, 
                State.SelectedSection?.Id);

            UpdateState(s => s with 
            { 
                IsLoading = false, 
                SuccessMessage = string.Format(QuestionStrings.SuccessMessage, importedCount),
                ImportText = string.Empty,
                SelectedFilePath = string.Empty
            });
        }
        catch (FormatException ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Database save error: {ex.Message}" });
        }
    }
}

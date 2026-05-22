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
using ProjektSlowkaRemasterd.Src.Core.Domain.Enums;
using ProjektSlowkaRemasterd.Src.Features.Question.UI.Screens.QuestionEditor;

namespace ProjektSlowkaRemasterd.Src.Features.Search.UI.Screens.Search;

using Category = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Category;
using Question = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Question;

/// <summary>
/// ViewModel for the Search screen.
/// Implements full-text search and filtering by category, topic, section, and status.
/// </summary>
public class SearchViewModel : ViewModelBase<SearchState>, IRoutableViewModel
{
    public string? UrlPathSegment => "search";
    public IScreen HostScreen { get; }

    private readonly ICategoryRepository _categoryRepository;
    private readonly ITopicRepository _topicRepository;
    private readonly ISectionRepository _sectionRepository;
    private readonly IQuestionRepository _questionRepository;

    public ReactiveCommand<Unit, Unit> LoadFiltersCommand { get; }
    public ReactiveCommand<Unit, Unit> SearchCommand { get; }
    public ReactiveCommand<int, Unit> DeleteQuestionCommand { get; }
    public ReactiveCommand<int, IRoutableViewModel> EditQuestionCommand { get; }

    public SearchViewModel(IScreen hostScreen)
        : this(
            hostScreen,
            Locator.Current.GetService<ICategoryRepository>()!,
            Locator.Current.GetService<ITopicRepository>()!,
            Locator.Current.GetService<ISectionRepository>()!,
            Locator.Current.GetService<IQuestionRepository>()!)
    {
    }

    public SearchViewModel(
        IScreen hostScreen,
        ICategoryRepository categoryRepository,
        ITopicRepository topicRepository,
        ISectionRepository sectionRepository,
        IQuestionRepository questionRepository)
        : base(new SearchState
        {
            Statuses = ImmutableList.Create("All", "Active Only", "Archived Only"),
            SelectedStatus = "All"
        })
    {
        HostScreen = hostScreen;
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _topicRepository = topicRepository ?? throw new ArgumentNullException(nameof(topicRepository));
        _sectionRepository = sectionRepository ?? throw new ArgumentNullException(nameof(sectionRepository));
        _questionRepository = questionRepository ?? throw new ArgumentNullException(nameof(questionRepository));

        LoadFiltersCommand = ReactiveCommand.CreateFromTask(LoadFiltersAsync);
        SearchCommand = ReactiveCommand.CreateFromTask(PerformSearchAsync);
        DeleteQuestionCommand = ReactiveCommand.CreateFromTask<int>(DeleteQuestionAsync);

        EditQuestionCommand = ReactiveCommand.CreateFromObservable<int, IRoutableViewModel>(qId =>
        {
            var item = State.Results.FirstOrDefault(r => r.Question.Id == qId);
            int? catId = item?.Question.CategoryId;
            return HostScreen.Router.Navigate.Execute(new QuestionEditorViewModel(HostScreen, catId, qId));
        });

        LoadFiltersCommand.Execute().Subscribe();
        SearchCommand.Execute().Subscribe();
    }

    private async Task LoadFiltersAsync()
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = string.Empty });
        try
        {
            var dbCategories = await _categoryRepository.GetAllAsync();
            var categoriesList = new List<Category> { new Category { Id = 0, Name = "All Categories" } };
            categoriesList.AddRange(dbCategories);

            var defaultCategory = categoriesList[0];
            var topicsList = new List<Topic> { new Topic { Id = 0, Name = "All Topics" } };
            var sectionsList = new List<Section> { new Section { Id = 0, Name = "All Sections" } };

            UpdateState(s => s with
            {
                IsLoading = false,
                Categories = categoriesList.ToImmutableList(),
                SelectedCategory = defaultCategory,
                Topics = topicsList.ToImmutableList(),
                SelectedTopic = topicsList[0],
                Sections = sectionsList.ToImmutableList(),
                SelectedSection = sectionsList[0]
            });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Failed to load categories: {ex.Message}" });
        }
    }

    public async Task SetSelectedCategoryAsync(Category? category)
    {
        var selectedCat = category ?? new Category { Id = 0, Name = "All Categories" };
        var topicsList = new List<Topic> { new Topic { Id = 0, Name = "All Topics" } };
        var sectionsList = new List<Section> { new Section { Id = 0, Name = "All Sections" } };

        UpdateState(s => s with
        {
            SelectedCategory = selectedCat,
            SelectedTopic = topicsList[0],
            SelectedSection = sectionsList[0],
            Topics = topicsList.ToImmutableList(),
            Sections = sectionsList.ToImmutableList()
        });

        if (selectedCat.Id != 0)
        {
            try
            {
                var dbTopics = await _topicRepository.GetByCategoryIdAsync(selectedCat.Id);
                topicsList.AddRange(dbTopics);
                UpdateState(s => s with { Topics = topicsList.ToImmutableList() });
            }
            catch (Exception ex)
            {
                UpdateState(s => s with { ErrorMessage = $"Failed to load topics: {ex.Message}" });
            }
        }

        await PerformSearchAsync();
    }

    public async Task SetSelectedTopicAsync(Topic? topic)
    {
        var selectedTop = topic ?? new Topic { Id = 0, Name = "All Topics" };
        var sectionsList = new List<Section> { new Section { Id = 0, Name = "All Sections" } };

        UpdateState(s => s with
        {
            SelectedTopic = selectedTop,
            SelectedSection = sectionsList[0],
            Sections = sectionsList.ToImmutableList()
        });

        if (selectedTop.Id != 0)
        {
            try
            {
                var dbSections = await _sectionRepository.GetByTopicIdAsync(selectedTop.Id);
                sectionsList.AddRange(dbSections);
                UpdateState(s => s with { Sections = sectionsList.ToImmutableList() });
            }
            catch (Exception ex)
            {
                UpdateState(s => s with { ErrorMessage = $"Failed to load sections: {ex.Message}" });
            }
        }

        await PerformSearchAsync();
    }

    public async Task SetSelectedSectionAsync(Section? section)
    {
        var selectedSec = section ?? new Section { Id = 0, Name = "All Sections" };
        UpdateState(s => s with { SelectedSection = selectedSec });
        await PerformSearchAsync();
    }

    public async Task SetSelectedStatusAsync(string status)
    {
        UpdateState(s => s with { SelectedStatus = status ?? "All" });
        await PerformSearchAsync();
    }

    public async Task SetSearchTextAsync(string searchText)
    {
        UpdateState(s => s with { SearchText = searchText ?? string.Empty });
        await PerformSearchAsync();
    }

    private async Task PerformSearchAsync()
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = string.Empty });
        try
        {
            var allQuestions = await _questionRepository.GetAllAsync();
            var categories = await _categoryRepository.GetAllAsync();
            var categoryMap = categories.ToDictionary(c => c.Id, c => c.Name);

            var topicCache = new Dictionary<int, string>();
            var sectionCache = new Dictionary<int, string>();

            var filtered = allQuestions.AsEnumerable();

            if (State.SelectedCategory != null && State.SelectedCategory.Id != 0)
            {
                filtered = filtered.Where(q => q.CategoryId == State.SelectedCategory.Id);
            }

            if (State.SelectedTopic != null && State.SelectedTopic.Id != 0)
            {
                filtered = filtered.Where(q => q.TopicId == State.SelectedTopic.Id);
            }

            if (State.SelectedSection != null && State.SelectedSection.Id != 0)
            {
                filtered = filtered.Where(q => q.SectionId == State.SelectedSection.Id);
            }

            if (State.SelectedStatus == "Active Only")
            {
                filtered = filtered.Where(q => q.Status == QuestionStatus.UNCHECKED || q.Status == QuestionStatus.KNOWN_ONE_SIDE);
            }
            else if (State.SelectedStatus == "Archived Only")
            {
                filtered = filtered.Where(q => q.Status == QuestionStatus.TO_ARCHIVE);
            }

            if (!string.IsNullOrWhiteSpace(State.SearchText))
            {
                var query = State.SearchText.Trim();
                filtered = filtered.Where(q =>
                    (q.QuestionText != null && q.QuestionText.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                    (q.AnswerText != null && q.AnswerText.Contains(query, StringComparison.OrdinalIgnoreCase))
                );
            }

            var resultsList = new List<SearchResultItem>();
            foreach (var q in filtered)
            {
                string catName = categoryMap.TryGetValue(q.CategoryId, out var cn) ? cn : "Unknown Category";

                string topicName = "-";
                if (q.TopicId.HasValue)
                {
                    if (topicCache.TryGetValue(q.TopicId.Value, out var cachedTopicName))
                    {
                        topicName = cachedTopicName;
                    }
                    else
                    {
                        var topic = await _topicRepository.GetByIdAsync(q.TopicId.Value);
                        topicName = topic?.Name ?? "-";
                        topicCache[q.TopicId.Value] = topicName;
                    }
                }

                string sectionName = "-";
                if (q.SectionId.HasValue)
                {
                    if (sectionCache.TryGetValue(q.SectionId.Value, out var cachedSectionName))
                    {
                        sectionName = cachedSectionName;
                    }
                    else
                    {
                        var sec = await _sectionRepository.GetByIdAsync(q.SectionId.Value);
                        sectionName = sec?.Name ?? "-";
                        sectionCache[q.SectionId.Value] = sectionName;
                    }
                }

                string statusText = q.Status switch
                {
                    QuestionStatus.UNCHECKED => "Unchecked",
                    QuestionStatus.KNOWN_ONE_SIDE => "Known One-Side",
                    QuestionStatus.TO_ARCHIVE => "Archived",
                    QuestionStatus.KNOWN => "Known",
                    _ => q.Status.ToString()
                };

                resultsList.Add(new SearchResultItem(q, catName, topicName, sectionName, statusText));
            }

            UpdateState(s => s with
            {
                IsLoading = false,
                Results = resultsList.ToImmutableList()
            });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Search failed: {ex.Message}" });
        }
    }

    private async Task DeleteQuestionAsync(int id)
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = string.Empty });
        try
        {
            await _questionRepository.DeleteAsync(id);
            await PerformSearchAsync();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Failed to delete question: {ex.Message}" });
        }
    }
}

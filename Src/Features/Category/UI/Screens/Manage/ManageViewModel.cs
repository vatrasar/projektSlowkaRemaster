using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ReactiveUI;
using Splat;
using ProjektSlowkaRemasterd.Src.Core.Mvvm;
using ProjektSlowkaRemasterd.Src.Core.Config;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;
using ProjektSlowkaRemasterd.Src.Core.Domain.RepositoryContracts;
using ProjektSlowkaRemasterd.Src.Core.Domain.Enums;
using ProjektSlowkaRemasterd.Src.Features.LaTeX.Domain;
using ProjektSlowkaRemasterd.Src.Features.Question.UI.Screens.QuestionEditor;
using ProjektSlowkaRemasterd.Src.Features.Question.UI.Screens.BulkImport;
using ProjektSlowkaRemasterd.Src.Features.Category.UI.Screens.AddCategory;

namespace ProjektSlowkaRemasterd.Src.Features.Category.UI.Screens.Manage;

public class ManageViewModel : ViewModelBase<ManageState>, IRoutableViewModel
{
    public string? UrlPathSegment => "manage";
    public IScreen HostScreen { get; }

    private readonly ICategoryRepository _categoryRepository;
    private readonly ITopicRepository _topicRepository;
    private readonly ISectionRepository _sectionRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IMediaRepository _mediaRepository;
    private readonly LaTeXGenerator _latexGenerator;

    public ReactiveCommand<Unit, Unit> LoadDataCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCategoryEditCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteCategoryCommand { get; }
    public ReactiveCommand<Unit, Unit> ArchiveAllQuestionsCommand { get; }
    
    public ReactiveCommand<Unit, Unit> AddTopicCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveTopicEditCommand { get; }

    public ReactiveCommand<Unit, Unit> AddSectionCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveSectionEditCommand { get; }
    public ReactiveCommand<Section, Unit> DeleteSectionCommand { get; }

    public ReactiveCommand<Unit, Unit> ExportCategoryCommand { get; }
    public Interaction<Unit, string?> ShowFolderPickerInteraction { get; } = new();

    public ReactiveCommand<Unit, IRoutableViewModel> NavigateToAddCategoryCommand { get; }
    public ReactiveCommand<Unit, IRoutableViewModel> NavigateToAddQuestionCommand { get; }
    public ReactiveCommand<Unit, IRoutableViewModel> NavigateToBulkImportCommand { get; }

    public ManageViewModel(IScreen hostScreen)
        : this(
            hostScreen, 
            Locator.Current.GetService<ICategoryRepository>()!,
            Locator.Current.GetService<ITopicRepository>()!,
            Locator.Current.GetService<ISectionRepository>()!,
            Locator.Current.GetService<IQuestionRepository>()!,
            Locator.Current.GetService<IMediaRepository>()!)
    {
    }

    public ManageViewModel(
        IScreen hostScreen,
        ICategoryRepository categoryRepository,
        ITopicRepository topicRepository,
        ISectionRepository sectionRepository,
        IQuestionRepository questionRepository,
        IMediaRepository mediaRepository)
        : base(new ManageState())
    {
        HostScreen = hostScreen;
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _topicRepository = topicRepository ?? throw new ArgumentNullException(nameof(topicRepository));
        _sectionRepository = sectionRepository ?? throw new ArgumentNullException(nameof(sectionRepository));
        _questionRepository = questionRepository ?? throw new ArgumentNullException(nameof(questionRepository));
        _mediaRepository = mediaRepository ?? throw new ArgumentNullException(nameof(mediaRepository));
        _latexGenerator = new LaTeXGenerator(_categoryRepository, _topicRepository, _sectionRepository, _questionRepository, _mediaRepository);

        LoadDataCommand = ReactiveCommand.CreateFromTask(LoadDataAsync);
        SaveCategoryEditCommand = ReactiveCommand.CreateFromTask(SaveCategoryEditAsync);
        DeleteCategoryCommand = ReactiveCommand.CreateFromTask(DeleteCategoryAsync);
        ArchiveAllQuestionsCommand = ReactiveCommand.CreateFromTask(ArchiveAllQuestionsAsync);

        AddTopicCommand = ReactiveCommand.CreateFromTask(AddTopicAsync);
        SaveTopicEditCommand = ReactiveCommand.CreateFromTask(SaveTopicEditAsync);

        AddSectionCommand = ReactiveCommand.CreateFromTask(AddSectionAsync);
        SaveSectionEditCommand = ReactiveCommand.CreateFromTask(SaveSectionEditAsync);
        DeleteSectionCommand = ReactiveCommand.CreateFromTask<Section>(DeleteSectionAsync);

        ExportCategoryCommand = ReactiveCommand.CreateFromTask(ExportCategoryToLaTeXAsync);

        NavigateToAddCategoryCommand = ReactiveCommand.CreateFromObservable(() => 
            HostScreen.Router.Navigate.Execute(new AddCategoryViewModel(HostScreen)));

        NavigateToAddQuestionCommand = ReactiveCommand.CreateFromObservable(() => 
            HostScreen.Router.Navigate.Execute(new QuestionEditorViewModel(HostScreen, State.SelectedCategory?.Id)));

        NavigateToBulkImportCommand = ReactiveCommand.CreateFromObservable(() => 
            HostScreen.Router.Navigate.Execute(new BulkImportViewModel(HostScreen, State.SelectedCategory?.Id)));

        // Load data on startup
        LoadDataCommand.Execute().Subscribe();
    }

    private async Task LoadDataAsync()
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = string.Empty, SuccessMessage = string.Empty });
        try
        {
            var categories = (await _categoryRepository.GetAllAsync()).ToList();
            UpdateState(s => s with { Categories = categories.ToImmutableList(), IsLoading = false });

            if (State.SelectedCategory != null)
            {
                var refreshedSelected = categories.FirstOrDefault(c => c.Id == State.SelectedCategory.Id);
                if (refreshedSelected != null)
                {
                    await SelectCategoryAsync(refreshedSelected);
                }
                else
                {
                    UpdateState(s => s with { SelectedCategory = null, Topics = ImmutableList<Topic>.Empty, SelectedTopic = null, Sections = ImmutableList<Section>.Empty, SelectedSection = null, Questions = ImmutableList<Core.Domain.Models.Question>.Empty });
                }
            }
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Error: {ex.Message}" });
        }
    }

    public async Task SelectCategoryAsync(Core.Domain.Models.Category category)
    {
        UpdateState(s => s with { SelectedCategory = category, IsLoading = true, ErrorMessage = string.Empty, SuccessMessage = string.Empty, IsEditingCategory = false });
        try
        {
            var topics = (await _topicRepository.GetByCategoryIdAsync(category.Id)).ToList();
            var questions = (await _questionRepository.GetByCategoryIdAsync(category.Id)).ToList();

            // Filter active/archived
            var filteredQuestions = questions
                .Where(q => State.ShowArchivedOnly ? q.Status == QuestionStatus.TO_ARCHIVE : q.Status != QuestionStatus.TO_ARCHIVE)
                .ToList();

            UpdateState(s => s with 
            { 
                Topics = topics.ToImmutableList(), 
                Questions = filteredQuestions.ToImmutableList(), 
                SelectedTopic = null, 
                Sections = ImmutableList<Section>.Empty, 
                SelectedSection = null,
                IsLoading = false 
            });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Error loading category details: {ex.Message}" });
        }
    }

    public void SetShowArchivedOnly(bool showArchived)
    {
        UpdateState(s => s with { ShowArchivedOnly = showArchived });
        if (State.SelectedCategory != null)
        {
            _ = SelectCategoryAsync(State.SelectedCategory);
        }
    }

    public void StartEditCategory()
    {
        if (State.SelectedCategory == null) return;
        UpdateState(s => s with 
        { 
            IsEditingCategory = true, 
            EditingCategoryName = State.SelectedCategory.Name, 
            EditingCategoryDoubleSided = State.SelectedCategory.Reverse 
        });
    }

    public void CancelEditCategory()
    {
        UpdateState(s => s with { IsEditingCategory = false });
    }

    public void SetEditingCategoryName(string name) => UpdateState(s => s with { EditingCategoryName = name });
    public void SetEditingCategoryDoubleSided(bool doubleSided) => UpdateState(s => s with { EditingCategoryDoubleSided = doubleSided });

    private async Task SaveCategoryEditAsync()
    {
        if (State.SelectedCategory == null) return;
        var name = State.EditingCategoryName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            UpdateState(s => s with { ErrorMessage = "Category name cannot be empty." });
            return;
        }

        UpdateState(s => s with { IsLoading = true, ErrorMessage = string.Empty });
        try
        {
            if (name != State.SelectedCategory.Name)
            {
                var existing = await _categoryRepository.GetByNameAsync(name);
                if (existing != null)
                {
                    UpdateState(s => s with { IsLoading = false, ErrorMessage = "Category with this name already exists." });
                    return;
                }
            }

            var updated = new Core.Domain.Models.Category { Id = State.SelectedCategory.Id, Name = name, Reverse = State.EditingCategoryDoubleSided, CreatedAt = State.SelectedCategory.CreatedAt };
            await _categoryRepository.UpdateAsync(updated);

            UpdateState(s => s with { SelectedCategory = updated, IsEditingCategory = false, SuccessMessage = "Category updated successfully!" });
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Error saving: {ex.Message}" });
        }
    }

    private async Task DeleteCategoryAsync()
    {
        if (State.SelectedCategory == null) return;
        UpdateState(s => s with { IsLoading = true, ErrorMessage = string.Empty });
        try
        {
            // Delete questions associated media files first
            var questions = await _questionRepository.GetByCategoryIdAsync(State.SelectedCategory.Id);
            var config = Locator.Current.GetService<IOptions<AppConfig>>()!.Value;
            foreach (var q in questions)
            {
                var mediaList = await _mediaRepository.GetByQuestionIdAsync(q.Id);
                foreach (var m in mediaList)
                {
                    var fullPath = Path.Combine(config.ResolvedMediaDirectoryPath, m.Filename);
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }
                }
            }

            await _categoryRepository.DeleteAsync(State.SelectedCategory.Id);
            UpdateState(s => s with { SelectedCategory = null, SuccessMessage = "Category deleted successfully!" });
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Error deleting category: {ex.Message}" });
        }
    }

    private async Task ArchiveAllQuestionsAsync()
    {
        if (State.SelectedCategory == null) return;
        UpdateState(s => s with { IsLoading = true, ErrorMessage = string.Empty });
        try
        {
            var questions = await _questionRepository.GetByCategoryIdAsync(State.SelectedCategory.Id);
            foreach (var q in questions)
            {
                if (q.Status != QuestionStatus.TO_ARCHIVE)
                {
                    q.Status = QuestionStatus.TO_ARCHIVE;
                    q.NextReview = DateTime.Today.AddDays(1000000);
                    q.Interval = 360;
                    await _questionRepository.UpdateAsync(q);
                }
            }

            UpdateState(s => s with { SuccessMessage = "All questions in category archived." });
            await SelectCategoryAsync(State.SelectedCategory);
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Error: {ex.Message}" });
        }
    }

    public void SetNewTopicName(string name) => UpdateState(s => s with { NewTopicName = name });

    private async Task AddTopicAsync()
    {
        if (State.SelectedCategory == null) return;
        var name = State.NewTopicName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            UpdateState(s => s with { ErrorMessage = "Topic name cannot be empty." });
            return;
        }

        UpdateState(s => s with { IsLoading = true, ErrorMessage = string.Empty });
        try
        {
            var existing = await _topicRepository.GetByNameAndCategoryAsync(State.SelectedCategory.Id, name);
            if (existing != null)
            {
                UpdateState(s => s with { IsLoading = false, ErrorMessage = "Topic name already exists in this category." });
                return;
            }

            await _topicRepository.AddAsync(new Topic { Id = 0, CategoryId = State.SelectedCategory.Id, Name = name });
            UpdateState(s => s with { NewTopicName = string.Empty, SuccessMessage = "Topic added!" });
            await SelectCategoryAsync(State.SelectedCategory);
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Error adding topic: {ex.Message}" });
        }
    }

    public void StartEditTopic(Topic topic) => UpdateState(s => s with { EditingTopic = topic, EditingTopicName = topic.Name });
    public void CancelEditTopic() => UpdateState(s => s with { EditingTopic = null });
    public void SetEditingTopicName(string name) => UpdateState(s => s with { EditingTopicName = name });

    private async Task SaveTopicEditAsync()
    {
        if (State.EditingTopic == null || State.SelectedCategory == null) return;
        var name = State.EditingTopicName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            UpdateState(s => s with { ErrorMessage = "Topic name cannot be empty." });
            return;
        }

        UpdateState(s => s with { IsLoading = true, ErrorMessage = string.Empty });
        try
        {
            if (name != State.EditingTopic.Name)
            {
                var existing = await _topicRepository.GetByNameAndCategoryAsync(State.SelectedCategory.Id, name);
                if (existing != null)
                {
                    UpdateState(s => s with { IsLoading = false, ErrorMessage = "Topic with this name already exists in this category." });
                    return;
                }
            }

            var updated = new Topic { Id = State.EditingTopic.Id, CategoryId = State.SelectedCategory.Id, Name = name };
            await _topicRepository.UpdateAsync(updated);

            UpdateState(s => s with { EditingTopic = null, SuccessMessage = "Topic updated!" });
            await SelectCategoryAsync(State.SelectedCategory);
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Error: {ex.Message}" });
        }
    }

    public async Task SelectTopicAsync(Topic? topic)
    {
        if (topic == null)
        {
            UpdateState(s => s with { SelectedTopic = null, Sections = ImmutableList<Section>.Empty, SelectedSection = null });
            return;
        }

        UpdateState(s => s with { SelectedTopic = topic, IsLoading = true, ErrorMessage = string.Empty });
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

    public void SetNewSectionName(string name) => UpdateState(s => s with { NewSectionName = name });

    private async Task AddSectionAsync()
    {
        if (State.SelectedTopic == null) return;
        var name = State.NewSectionName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            UpdateState(s => s with { ErrorMessage = "Section name cannot be empty." });
            return;
        }

        UpdateState(s => s with { IsLoading = true, ErrorMessage = string.Empty });
        try
        {
            var existing = await _sectionRepository.GetByNameAndTopicAsync(State.SelectedTopic.Id, name);
            if (existing != null)
            {
                UpdateState(s => s with { IsLoading = false, ErrorMessage = "Section name already exists in this topic." });
                return;
            }

            await _sectionRepository.AddAsync(new Section { Id = 0, TopicId = State.SelectedTopic.Id, Name = name });
            UpdateState(s => s with { NewSectionName = string.Empty, SuccessMessage = "Section added!" });
            await SelectTopicAsync(State.SelectedTopic);
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Error adding section: {ex.Message}" });
        }
    }

    public void StartEditSection(Section section) => UpdateState(s => s with { EditingSection = section, EditingSectionName = section.Name });
    public void CancelEditSection() => UpdateState(s => s with { EditingSection = null });
    public void SetEditingSectionName(string name) => UpdateState(s => s with { EditingSectionName = name });

    private async Task SaveSectionEditAsync()
    {
        if (State.EditingSection == null || State.SelectedTopic == null) return;
        var name = State.EditingSectionName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            UpdateState(s => s with { ErrorMessage = "Section name cannot be empty." });
            return;
        }

        UpdateState(s => s with { IsLoading = true, ErrorMessage = string.Empty });
        try
        {
            if (name != State.EditingSection.Name)
            {
                var existing = await _sectionRepository.GetByNameAndTopicAsync(State.SelectedTopic.Id, name);
                if (existing != null)
                {
                    UpdateState(s => s with { IsLoading = false, ErrorMessage = "Section with this name already exists in this topic." });
                    return;
                }
            }

            var updated = new Section { Id = State.EditingSection.Id, TopicId = State.SelectedTopic.Id, Name = name };
            await _sectionRepository.UpdateAsync(updated);

            UpdateState(s => s with { EditingSection = null, SuccessMessage = "Section updated!" });
            await SelectTopicAsync(State.SelectedTopic);
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Error: {ex.Message}" });
        }
    }

    private async Task DeleteSectionAsync(Section section)
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = string.Empty });
        try
        {
            await _sectionRepository.DeleteAsync(section.Id);
            UpdateState(s => s with { SuccessMessage = "Section deleted!" });
            if (State.SelectedTopic != null)
            {
                await SelectTopicAsync(State.SelectedTopic);
                if (State.SelectedCategory != null)
                {
                    await SelectCategoryAsync(State.SelectedCategory);
                }
            }
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Error: {ex.Message}" });
        }
    }

    public async Task RestoreQuestionAsync(Core.Domain.Models.Question question)
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = string.Empty });
        try
        {
            question.Status = QuestionStatus.UNCHECKED;
            question.Interval = 1;
            question.NextReview = DateTime.Today;
            await _questionRepository.UpdateAsync(question);

            UpdateState(s => s with { SuccessMessage = "Question restored successfully!" });
            if (State.SelectedCategory != null)
            {
                await SelectCategoryAsync(State.SelectedCategory);
            }
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Error restoring: {ex.Message}" });
        }
    }

    public async Task DeleteQuestionAsync(Core.Domain.Models.Question question)
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = string.Empty });
        try
        {
            // Delete associated media files
            var mediaList = await _mediaRepository.GetByQuestionIdAsync(question.Id);
            var config = Locator.Current.GetService<IOptions<AppConfig>>()!.Value;
            foreach (var m in mediaList)
            {
                var fullPath = Path.Combine(config.ResolvedMediaDirectoryPath, m.Filename);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }

            await _questionRepository.DeleteAsync(question.Id);
            UpdateState(s => s with { SuccessMessage = "Question deleted successfully!" });
            if (State.SelectedCategory != null)
            {
                await SelectCategoryAsync(State.SelectedCategory);
            }
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Error deleting question: {ex.Message}" });
        }
    }

    public void EditQuestion(Core.Domain.Models.Question question)
    {
        HostScreen.Router.Navigate.Execute(new QuestionEditorViewModel(HostScreen, State.SelectedCategory?.Id, question.Id)).Subscribe();
    }

    private async Task ExportCategoryToLaTeXAsync()
    {
        if (State.SelectedCategory == null) return;

        var selectedParentPath = await ShowFolderPickerInteraction.Handle(Unit.Default);
        if (string.IsNullOrEmpty(selectedParentPath))
        {
            return;
        }

        await ExecuteLaTeXExportAsync(selectedParentPath);
    }

    private async Task ExecuteLaTeXExportAsync(string selectedParentPath)
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = string.Empty, SuccessMessage = string.Empty });
        try
        {
            await ExportToFolderAsync(selectedParentPath);
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Export failed: {ex.Message}" });
        }
    }

    private async Task ExportToFolderAsync(string selectedParentPath)
    {
        var folderName = string.Join("_", State.SelectedCategory!.Name.Split(Path.GetInvalidFileNameChars()));
        var exportPath = Path.Combine(selectedParentPath, folderName);

        await _latexGenerator.ExportCategoryAsync(State.SelectedCategory.Id, exportPath);

        UpdateState(s => s with { IsLoading = false, SuccessMessage = $"Category exported successfully to: {exportPath}" });
    }
}

using System;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using Splat;
using ProjektSlowkaRemasterd.Src.Core.Mvvm;
using ProjektSlowkaRemasterd.Src.Core.Domain.RepositoryContracts;
using ProjektSlowkaRemasterd.Src.Features.Question.UI.Screens.QuestionEditor;

namespace ProjektSlowkaRemasterd.Src.Features.Category.UI.Screens.AddCategory;

public class AddCategoryViewModel : ViewModelBase<AddCategoryState>, IRoutableViewModel
{
    public string? UrlPathSegment => "add-category";
    public IScreen HostScreen { get; }

    private readonly ICategoryRepository _categoryRepository;

    public ReactiveCommand<Unit, Unit> SaveCategoryCommand { get; }
    public ReactiveCommand<Unit, IRoutableViewModel> GoBackCommand { get; }

    public AddCategoryViewModel(IScreen hostScreen)
        : this(hostScreen, Locator.Current.GetService<ICategoryRepository>()!)
    {
    }

    public AddCategoryViewModel(IScreen hostScreen, ICategoryRepository categoryRepository)
        : base(new AddCategoryState())
    {
        HostScreen = hostScreen;
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));

        SaveCategoryCommand = ReactiveCommand.CreateFromTask(SaveCategoryAsync);
        
        GoBackCommand = ReactiveCommand.CreateFromObservable(() =>
            HostScreen.Router.NavigateBack.Execute()
        );
    }

    public void SetCategoryName(string name)
    {
        UpdateState(s => s with { CategoryName = name, ErrorMessage = string.Empty });
    }

    public void SetDoubleSided(bool isDoubleSided)
    {
        UpdateState(s => s with { IsDoubleSided = isDoubleSided });
    }

    private async Task SaveCategoryAsync()
    {
        var name = State.CategoryName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            UpdateState(s => s with { ErrorMessage = "Category name cannot be empty." });
            return;
        }

        UpdateState(s => s with { IsLoading = true, ErrorMessage = string.Empty });

        try
        {
            var existing = await _categoryRepository.GetByNameAsync(name);
            if (existing != null)
            {
                UpdateState(s => s with { IsLoading = false, ErrorMessage = "Category with this name already exists." });
                return;
            }

            var category = new Core.Domain.Models.Category { Id = 0, Name = name, Reverse = State.IsDoubleSided, CreatedAt = DateTime.UtcNow };
            var saved = await _categoryRepository.AddAsync(category);

            UpdateState(s => s with { IsLoading = false });

            // Since the category is new, it has no questions, redirect to adding the first question.
            // Navigate to QuestionEditorViewModel with the categoryId pre-filled.
            HostScreen.Router.Navigate.Execute(new QuestionEditorViewModel(HostScreen, saved.Id)).Subscribe();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = $"Error: {ex.Message}" });
        }
    }
}

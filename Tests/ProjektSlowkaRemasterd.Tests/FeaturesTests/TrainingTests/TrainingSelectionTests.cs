using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Moq;
using ReactiveUI;
using Xunit;
using Splat;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;
using ProjektSlowkaRemasterd.Src.Core.Domain.RepositoryContracts;
using ProjektSlowkaRemasterd.Src.Core.Domain.Enums;
using ProjektSlowkaRemasterd.Src.Features.Training.UI.Screens.TrainingSelection;
using ProjektSlowkaRemasterd.Src.Features.Training.UI.Screens.TrainingSession;

namespace ProjektSlowkaRemasterd.Tests.FeaturesTests.TrainingTests;

public class TrainingSelectionTests
{
    private readonly Mock<IScreen> _mockScreen;
    private readonly Mock<ICategoryRepository> _mockCategoryRepo;
    private readonly Mock<ITopicRepository> _mockTopicRepo;
    private readonly Mock<IQuestionRepository> _mockQuestionRepo;

    public TrainingSelectionTests()
    {
        _mockScreen = new Mock<IScreen>();
        _mockCategoryRepo = new Mock<ICategoryRepository>();
        _mockTopicRepo = new Mock<ITopicRepository>();
        _mockQuestionRepo = new Mock<IQuestionRepository>();
        var mockMediaRepo = new Mock<IMediaRepository>();

        _mockScreen.Setup(s => s.Router).Returns(new RoutingState());
        mockMediaRepo.Setup(m => m.GetByQuestionIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Media>());

        Locator.CurrentMutable.Register(() => _mockCategoryRepo.Object, typeof(ICategoryRepository));
        Locator.CurrentMutable.Register(() => mockMediaRepo.Object, typeof(IMediaRepository));
    }

    [Fact]
    public async Task TrainingSelection_Load_FiltersTomorrowAndIntervalOneCorrectly()
    {
        var category1 = new Category { Id = 1, Name = "Category 1" };
        var category2 = new Category { Id = 2, Name = "Category 2" };
        
        var q1 = new Question { Id = 1, CategoryId = 1, NextReview = DateTime.Today.AddDays(1), Interval = 1, Status = QuestionStatus.UNCHECKED };
        var q2 = new Question { Id = 2, CategoryId = 1, NextReview = DateTime.Today.AddDays(1), Interval = 2, Status = QuestionStatus.UNCHECKED };
        var q3 = new Question { Id = 3, CategoryId = 2, NextReview = DateTime.Today.AddDays(2), Interval = 1, Status = QuestionStatus.UNCHECKED };
        var q4 = new Question { Id = 4, CategoryId = 2, NextReview = DateTime.Today, Interval = 1, Status = QuestionStatus.UNCHECKED };
        var q5 = new Question { Id = 5, CategoryId = 2, NextReview = DateTime.Today, Interval = 1, Status = QuestionStatus.TO_ARCHIVE };

        _mockCategoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category> { category1, category2 });
        _mockQuestionRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Question> { q1, q2, q3, q4, q5 });

        var vm = new TrainingSelectionViewModel(
            _mockScreen.Object,
            _mockCategoryRepo.Object,
            _mockTopicRepo.Object,
            _mockQuestionRepo.Object
        );

        await vm.LoadCommand.Execute().ToTask();

        Assert.Equal(2, vm.State.AllCategories.Count);
        Assert.Equal(2, vm.State.AllCategories.First(c => c.Category.Id == 1).TotalCount);

        Assert.Equal(2, vm.State.Categories.Count);
        Assert.Equal(1, vm.State.Categories.First(c => c.Category.Id == 1).TotalCount);
        Assert.Equal(1, vm.State.Categories.First(c => c.Category.Id == 2).TotalCount);
    }

    [Fact]
    public async Task TrainingSelection_ToggleProblematicFilter_FiltersCategoriesCorrectly()
    {
        var category1 = new Category { Id = 1, Name = "Category 1" };
        var category2 = new Category { Id = 2, Name = "Category 2" };
        
        var q1 = new Question { Id = 1, CategoryId = 1, NextReview = DateTime.Today.AddDays(1), Interval = 1, IsProblematic = false, Status = QuestionStatus.UNCHECKED };
        var q2 = new Question { Id = 2, CategoryId = 2, NextReview = DateTime.Today, Interval = 1, IsProblematic = true, Status = QuestionStatus.UNCHECKED };

        _mockCategoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category> { category1, category2 });
        _mockQuestionRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Question> { q1, q2 });

        var vm = new TrainingSelectionViewModel(
            _mockScreen.Object,
            _mockCategoryRepo.Object,
            _mockTopicRepo.Object,
            _mockQuestionRepo.Object
        );

        await vm.LoadCommand.Execute().ToTask();

        Assert.Equal(2, vm.State.Categories.Count);
        Assert.Equal(1, vm.State.ProblematicCount);
        Assert.False(vm.State.FilterProblematic);

        await vm.ToggleProblematicFilterCommand.Execute().ToTask();

        Assert.True(vm.State.FilterProblematic);
        var singleCat = Assert.Single(vm.State.Categories);
        Assert.Equal(2, singleCat.Category.Id);
        Assert.Equal(1, singleCat.TotalCount);

        await vm.ToggleProblematicFilterCommand.Execute().ToTask();

        Assert.False(vm.State.FilterProblematic);
        Assert.Equal(2, vm.State.Categories.Count);
    }

    [Fact]
    public async Task TrainingSelection_ToggleCategoryMark_PreservesMarkedStateAcrossFilterToggle()
    {
        var category1 = new Category { Id = 1, Name = "Category 1" };
        var category2 = new Category { Id = 2, Name = "Category 2" };
        
        var q1 = new Question { Id = 1, CategoryId = 1, NextReview = DateTime.Today.AddDays(1), Interval = 1, IsProblematic = false, Status = QuestionStatus.UNCHECKED };
        var q2 = new Question { Id = 2, CategoryId = 2, NextReview = DateTime.Today, Interval = 1, IsProblematic = true, Status = QuestionStatus.UNCHECKED };

        _mockCategoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category> { category1, category2 });
        _mockQuestionRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Question> { q1, q2 });

        var vm = new TrainingSelectionViewModel(
            _mockScreen.Object,
            _mockCategoryRepo.Object,
            _mockTopicRepo.Object,
            _mockQuestionRepo.Object
        );

        await vm.LoadCommand.Execute().ToTask();

        await vm.ToggleCategoryMarkCommand.Execute(1).ToTask();

        Assert.True(vm.State.Categories.First(c => c.Category.Id == 1).IsMarked);
        Assert.Contains(1, vm.State.MarkedCategoryIds);

        await vm.ToggleProblematicFilterCommand.Execute().ToTask();

        Assert.DoesNotContain(vm.State.Categories, c => c.Category.Id == 1);
        Assert.Contains(1, vm.State.MarkedCategoryIds);

        await vm.ToggleProblematicFilterCommand.Execute().ToTask();

        Assert.True(vm.State.Categories.First(c => c.Category.Id == 1).IsMarked);
    }

    [Fact]
    public async Task TrainingSelection_TrainMarkedCategories_NavigatesWithCorrectQuestions()
    {
        var category1 = new Category { Id = 1, Name = "Category 1" };
        var category2 = new Category { Id = 2, Name = "Category 2" };
        
        var q1 = new Question { Id = 1, CategoryId = 1, NextReview = DateTime.Today, Interval = 1, IsProblematic = false, Status = QuestionStatus.UNCHECKED };
        var q2 = new Question { Id = 2, CategoryId = 1, NextReview = DateTime.Today, Interval = 1, IsProblematic = true, Status = QuestionStatus.UNCHECKED };
        var q3 = new Question { Id = 3, CategoryId = 2, NextReview = DateTime.Today, Interval = 1, IsProblematic = true, Status = QuestionStatus.UNCHECKED };

        _mockCategoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category> { category1, category2 });
        _mockQuestionRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Question> { q1, q2, q3 });
        _mockQuestionRepo.Setup(r => r.GetByCategoryIdAsync(1)).ReturnsAsync(new List<Question> { q1, q2 });
        _mockQuestionRepo.Setup(r => r.GetByCategoryIdAsync(2)).ReturnsAsync(new List<Question> { q3 });

        var vm = new TrainingSelectionViewModel(
            _mockScreen.Object,
            _mockCategoryRepo.Object,
            _mockTopicRepo.Object,
            _mockQuestionRepo.Object
        );

        await vm.LoadCommand.Execute().ToTask();

        await vm.ToggleCategoryMarkCommand.Execute(1).ToTask();
        await vm.ToggleCategoryMarkCommand.Execute(2).ToTask();

        await vm.TrainMarkedCategoriesCommand.Execute().ToTask();

        var navStack = _mockScreen.Object.Router.NavigationStack;
        var firstSession = Assert.IsType<TrainingSessionViewModel>(navStack.Last());
        await firstSession.LoadSessionCommand.Execute().ToTask();

        var fields = typeof(TrainingSessionViewModel).GetField("_initialQuestions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var firstQuestions = (List<Question>)fields!.GetValue(firstSession)!;

        Assert.Equal(3, firstQuestions.Count);
        Assert.Equal("Tomorrow's Reviews", firstSession.State.Title);

        await vm.ToggleProblematicFilterCommand.Execute().ToTask();

        await vm.TrainMarkedCategoriesCommand.Execute().ToTask();

        var secondSession = Assert.IsType<TrainingSessionViewModel>(navStack.Last());
        await secondSession.LoadSessionCommand.Execute().ToTask();

        var secondQuestions = (List<Question>)fields.GetValue(secondSession)!;

        Assert.Equal(2, secondQuestions.Count);
        Assert.DoesNotContain(secondQuestions, q => q.Id == 1);
        Assert.Equal("Problematic Tomorrow", secondSession.State.Title);
    }
}

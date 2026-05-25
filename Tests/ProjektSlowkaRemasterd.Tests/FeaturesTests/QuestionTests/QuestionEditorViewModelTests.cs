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
using ProjektSlowkaRemasterd.Src.Core.Config;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;
using ProjektSlowkaRemasterd.Src.Core.Domain.RepositoryContracts;
using ProjektSlowkaRemasterd.Src.Core.Domain.Enums;
using ProjektSlowkaRemasterd.Src.Features.Question.UI.Screens.QuestionEditor;

namespace ProjektSlowkaRemasterd.Tests.FeaturesTests.QuestionTests;

public class QuestionEditorViewModelTests
{
    private readonly Mock<IScreen> _mockScreen;
    private readonly Mock<ICategoryRepository> _mockCategoryRepo;
    private readonly Mock<ITopicRepository> _mockTopicRepo;
    private readonly Mock<ISectionRepository> _mockSectionRepo;
    private readonly Mock<IQuestionRepository> _mockQuestionRepo;
    private readonly Mock<IStatisticsRepository> _mockStatisticsRepo;
    private readonly Mock<IMediaRepository> _mockMediaRepo;
    private readonly AppConfig _config;

    public QuestionEditorViewModelTests()
    {
        _mockScreen = new Mock<IScreen>();
        _mockCategoryRepo = new Mock<ICategoryRepository>();
        _mockTopicRepo = new Mock<ITopicRepository>();
        _mockSectionRepo = new Mock<ISectionRepository>();
        _mockQuestionRepo = new Mock<IQuestionRepository>();
        _mockStatisticsRepo = new Mock<IStatisticsRepository>();
        _mockMediaRepo = new Mock<IMediaRepository>();

        _mockScreen.Setup(s => s.Router).Returns(new RoutingState());
        _config = new AppConfig
        {
            MediaDirectoryPath = "test_media"
        };
    }

    [Fact]
    public async Task SaveQuestion_WhenAnswerTextIsEmptyAndNoImages_FailsValidation()
    {
        var category = new Category { Id = 1, Name = "Polish" };
        _mockCategoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category> { category });
        _mockCategoryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);

        var vm = new QuestionEditorViewModel(
            _mockScreen.Object,
            _mockCategoryRepo.Object,
            _mockTopicRepo.Object,
            _mockSectionRepo.Object,
            _mockQuestionRepo.Object,
            _mockStatisticsRepo.Object,
            _mockMediaRepo.Object,
            categoryId: null,
            questionId: null,
            config: _config
        );

        await vm.LoadDataCommand.Execute().ToTask();

        await vm.SetSelectedCategory(category);
        vm.SetQuestionText("What is the capital of Poland?");
        vm.SetAnswerText(string.Empty);

        await vm.SaveQuestionCommand.Execute().ToTask();

        Assert.Equal("Answer field cannot be empty.", vm.State.ErrorMessage);
        _mockQuestionRepo.Verify(r => r.AddAsync(It.IsAny<Question>()), Times.Never);
    }

    [Fact]
    public async Task SaveQuestion_WhenAnswerTextIsEmptyButHasImages_Succeeds()
    {
        var category = new Category { Id = 1, Name = "Polish" };
        var addedQuestion = new Question { Id = 10, CategoryId = 1, AnswerText = string.Empty };

        _mockCategoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category> { category });
        _mockCategoryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
        _mockQuestionRepo.Setup(r => r.AddAsync(It.IsAny<Question>())).ReturnsAsync(addedQuestion);
        _mockStatisticsRepo.Setup(r => r.AddAsync(It.IsAny<Statistics>())).ReturnsAsync(new Statistics { Id = 1 });
        _mockMediaRepo.Setup(r => r.AddAsync(It.IsAny<Media>())).ReturnsAsync(new Media { Id = 1 });

        var vm = new QuestionEditorViewModel(
            _mockScreen.Object,
            _mockCategoryRepo.Object,
            _mockTopicRepo.Object,
            _mockSectionRepo.Object,
            _mockQuestionRepo.Object,
            _mockStatisticsRepo.Object,
            _mockMediaRepo.Object,
            categoryId: null,
            questionId: null,
            config: _config
        );

        await vm.LoadDataCommand.Execute().ToTask();

        await vm.SetSelectedCategory(category);
        vm.SetQuestionText("What is the capital of Poland?");
        vm.SetAnswerText(string.Empty);
        vm.AddImage("image.png", MediaStatus.ANSWER);

        await vm.SaveQuestionCommand.Execute().ToTask();

        Assert.Empty(vm.State.ErrorMessage);
        Assert.Equal("Question added successfully!", vm.State.SuccessMessage);
        _mockQuestionRepo.Verify(r => r.AddAsync(It.IsAny<Question>()), Times.Once);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Moq;
using ReactiveUI;
using Xunit;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;
using ProjektSlowkaRemasterd.Src.Core.Domain.RepositoryContracts;
using ProjektSlowkaRemasterd.Src.Core.Domain.Enums;
using ProjektSlowkaRemasterd.Src.Features.Review.UI.Screens.ReviewSession;

namespace ProjektSlowkaRemasterd.Tests.FeaturesTests.ReviewTests;

public class ReviewSessionTests
{
    private readonly Mock<IScreen> _mockScreen;
    private readonly Mock<ICategoryRepository> _mockCategoryRepo;
    private readonly Mock<ITopicRepository> _mockTopicRepo;
    private readonly Mock<IQuestionRepository> _mockQuestionRepo;
    private readonly Mock<IStatisticsRepository> _mockStatisticsRepo;
    private readonly Mock<IMediaRepository> _mockMediaRepo;

    public ReviewSessionTests()
    {
        _mockScreen = new Mock<IScreen>();
        _mockCategoryRepo = new Mock<ICategoryRepository>();
        _mockTopicRepo = new Mock<ITopicRepository>();
        _mockQuestionRepo = new Mock<IQuestionRepository>();
        _mockStatisticsRepo = new Mock<IStatisticsRepository>();
        _mockMediaRepo = new Mock<IMediaRepository>();

        // Set up Router Mock so ViewModel instantiation doesn't throw
        _mockScreen.Setup(s => s.Router).Returns(new RoutingState());
        _mockMediaRepo.Setup(m => m.GetByQuestionIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<Media>());
    }

    [Fact]
    public async Task LoadSession_WithBidirectionalCategory_AddsQToAAndAllowsPromotionToAToQ()
    {
        // Arrange
        var category = new Category { Id = 1, Name = "Languages", Reverse = true };
        var question = new Question 
        { 
            Id = 10, 
            QuestionText = "Apple", 
            AnswerText = "Jabłko", 
            CategoryId = 1, 
            Status = QuestionStatus.UNCHECKED,
            Interval = 1
        };

        _mockCategoryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
        _mockQuestionRepo.Setup(r => r.GetReviewQuestionsByCategoryAsync(1, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Question> { question });

        var vm = new ReviewSessionViewModel(
            _mockScreen.Object,
            categoryId: 1,
            topicId: null,
            _mockCategoryRepo.Object,
            _mockTopicRepo.Object,
            _mockQuestionRepo.Object,
            _mockStatisticsRepo.Object,
            _mockMediaRepo.Object
        );

        // Act - Trigger initial load
        await vm.LoadSessionCommand.Execute().ToTask();

        // Assert initial load direction
        Assert.Equal("Q->A", vm.State.CurrentDirection);
        Assert.Equal("Apple", vm.State.QuestionText);

        // Act - Click "Know" (promotes Q->A to A->Q in bidirectional mode)
        await vm.KnowCommand.Execute().ToTask();

        // Assert that the question was updated to KNOWN_ONE_SIDE and scheduled as A->Q
        _mockQuestionRepo.Verify(r => r.UpdateAsync(It.Is<Question>(q => q.Status == QuestionStatus.KNOWN_ONE_SIDE)), Times.Once);
        Assert.Equal("A->Q", vm.State.CurrentDirection);
        Assert.Equal("Apple", vm.State.AnswerText); // Answer text is swapped to the back (shown when answer is revealed, but initially current card loads)
    }

    [Fact]
    public async Task OnUnknown_WithGroupedQuestion_ResetsAllQuestionsInGroup()
    {
        // Arrange
        var category = new Category { Id = 1, Name = "Math", Reverse = false };
        var q1 = new Question { Id = 101, GroupId = 5, CategoryId = 1, Status = QuestionStatus.UNCHECKED, StatisticsId = 10 };
        var q2 = new Question { Id = 102, GroupId = 5, CategoryId = 1, Status = QuestionStatus.UNCHECKED, StatisticsId = 11 };

        _mockCategoryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
        _mockQuestionRepo.Setup(r => r.GetReviewQuestionsByCategoryAsync(1, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Question> { q1 }); // q1 is due
        _mockQuestionRepo.Setup(r => r.GetByGroupIdAsync(5))
            .ReturnsAsync(new List<Question> { q1, q2 }); // Both belong to group 5

        _mockStatisticsRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new Statistics { Failures = 0 });

        var vm = new ReviewSessionViewModel(
            _mockScreen.Object,
            categoryId: 1,
            topicId: null,
            _mockCategoryRepo.Object,
            _mockTopicRepo.Object,
            _mockQuestionRepo.Object,
            _mockStatisticsRepo.Object,
            _mockMediaRepo.Object
        );

        // Act
        await vm.LoadSessionCommand.Execute().ToTask();
        
        // Assert: Both group questions were loaded into the active queue
        Assert.Equal(2, vm.State.TotalQuestionsCount);

        // Act - Mark first as "Unknown"
        await vm.UnknownCommand.Execute().ToTask();

        // Assert: Both questions in the group should be updated/penalized (their interval reset to 1)
        _mockQuestionRepo.Verify(r => r.UpdateAsync(It.Is<Question>(q => q.Id == 101 && q.Interval == 1)), Times.Once);
        _mockQuestionRepo.Verify(r => r.UpdateAsync(It.Is<Question>(q => q.Id == 102 && q.Interval == 1)), Times.Once);
        
        // Assert: The session should finish since all items of group 5 were removed
        Assert.True(vm.State.IsFinished);
    }
}

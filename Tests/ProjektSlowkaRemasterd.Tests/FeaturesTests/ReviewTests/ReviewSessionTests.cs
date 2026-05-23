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

    [Theory]
    [InlineData(1, 3)]
    [InlineData(3, 10)]
    [InlineData(10, 30)]
    public async Task OnKnow_NormalMode_AdvancesInterval(int initialInterval, int expectedInterval)
    {
        var category = new Category { Id = 1, Name = "Languages", Reverse = false };
        var question = new Question 
        { 
            Id = 10, 
            CategoryId = 1, 
            Status = QuestionStatus.UNCHECKED,
            Interval = initialInterval,
            StatisticsId = 5
        };

        _mockCategoryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
        _mockQuestionRepo.Setup(r => r.GetReviewQuestionsByCategoryAsync(1, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Question> { question });
        _mockStatisticsRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Statistics { Id = 5, Failures = 0 });

        var vm = new ReviewSessionViewModel(
            _mockScreen.Object,
            1,
            null,
            _mockCategoryRepo.Object,
            _mockTopicRepo.Object,
            _mockQuestionRepo.Object,
            _mockStatisticsRepo.Object,
            _mockMediaRepo.Object
        );

        await vm.LoadSessionCommand.Execute().ToTask();
        await vm.KnowCommand.Execute().ToTask();

        _mockQuestionRepo.Verify(r => r.UpdateAsync(It.Is<Question>(q => q.Interval == expectedInterval)), Times.Once);
    }

    [Fact]
    public async Task OnUnknown_IntervalLessThanTen_DoesNotIncrementFailures()
    {
        var category = new Category { Id = 1, Name = "Languages", Reverse = false };
        var question = new Question 
        { 
            Id = 10, 
            CategoryId = 1, 
            Status = QuestionStatus.UNCHECKED,
            Interval = 3,
            StatisticsId = 5
        };
        var stats = new Statistics { Id = 5, Failures = 0 };

        _mockCategoryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
        _mockQuestionRepo.Setup(r => r.GetReviewQuestionsByCategoryAsync(1, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Question> { question });
        _mockStatisticsRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(stats);

        var vm = new ReviewSessionViewModel(
            _mockScreen.Object,
            1,
            null,
            _mockCategoryRepo.Object,
            _mockTopicRepo.Object,
            _mockQuestionRepo.Object,
            _mockStatisticsRepo.Object,
            _mockMediaRepo.Object
        );

        await vm.LoadSessionCommand.Execute().ToTask();
        await vm.UnknownCommand.Execute().ToTask();

        _mockStatisticsRepo.Verify(r => r.UpdateAsync(It.IsAny<Statistics>()), Times.Never);
        Assert.Equal(0, stats.Failures);
    }

    [Fact]
    public async Task OnUnknown_IntervalTenOrMore_IncrementsFailures()
    {
        var category = new Category { Id = 1, Name = "Languages", Reverse = false };
        var question = new Question 
        { 
            Id = 10, 
            CategoryId = 1, 
            Status = QuestionStatus.UNCHECKED,
            Interval = 10,
            StatisticsId = 5
        };
        var stats = new Statistics { Id = 5, Failures = 1 };

        _mockCategoryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
        _mockQuestionRepo.Setup(r => r.GetReviewQuestionsByCategoryAsync(1, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Question> { question });
        _mockStatisticsRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(stats);

        var vm = new ReviewSessionViewModel(
            _mockScreen.Object,
            1,
            null,
            _mockCategoryRepo.Object,
            _mockTopicRepo.Object,
            _mockQuestionRepo.Object,
            _mockStatisticsRepo.Object,
            _mockMediaRepo.Object
        );

        await vm.LoadSessionCommand.Execute().ToTask();
        await vm.UnknownCommand.Execute().ToTask();

        _mockStatisticsRepo.Verify(r => r.UpdateAsync(It.Is<Statistics>(s => s.Failures == 2)), Times.Once);
        Assert.Equal(2, stats.Failures);
    }

    [Theory]
    [InlineData(1, 3)]
    [InlineData(3, 6)]
    [InlineData(6, 10)]
    [InlineData(10, 20)]
    [InlineData(20, 30)]
    public async Task OnKnow_HardMode_AdvancesInterval(int initialInterval, int expectedInterval)
    {
        var category = new Category { Id = 1, Name = "Languages", Reverse = false };
        var question = new Question 
        { 
            Id = 10, 
            CategoryId = 1, 
            Status = QuestionStatus.UNCHECKED,
            Interval = initialInterval,
            StatisticsId = 5
        };

        _mockCategoryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
        _mockQuestionRepo.Setup(r => r.GetReviewQuestionsByCategoryAsync(1, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Question> { question });
        _mockStatisticsRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Statistics { Id = 5, Failures = 2 });

        var vm = new ReviewSessionViewModel(
            _mockScreen.Object,
            1,
            null,
            _mockCategoryRepo.Object,
            _mockTopicRepo.Object,
            _mockQuestionRepo.Object,
            _mockStatisticsRepo.Object,
            _mockMediaRepo.Object
        );

        await vm.LoadSessionCommand.Execute().ToTask();
        await vm.KnowCommand.Execute().ToTask();

        _mockQuestionRepo.Verify(r => r.UpdateAsync(It.Is<Question>(q => q.Interval == expectedInterval)), Times.Once);
    }

    [Fact]
    public async Task OnKnow_Interval30_ArchivesQuestion()
    {
        var category = new Category { Id = 1, Name = "Languages", Reverse = false };
        var question = new Question 
        { 
            Id = 10, 
            CategoryId = 1, 
            Status = QuestionStatus.UNCHECKED,
            Interval = 30,
            StatisticsId = 5
        };

        _mockCategoryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
        _mockQuestionRepo.Setup(r => r.GetReviewQuestionsByCategoryAsync(1, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Question> { question });
        _mockStatisticsRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Statistics { Id = 5, Failures = 0 });

        var vm = new ReviewSessionViewModel(
            _mockScreen.Object,
            1,
            null,
            _mockCategoryRepo.Object,
            _mockTopicRepo.Object,
            _mockQuestionRepo.Object,
            _mockStatisticsRepo.Object,
            _mockMediaRepo.Object
        );

        await vm.LoadSessionCommand.Execute().ToTask();
        await vm.KnowCommand.Execute().ToTask();

        _mockQuestionRepo.Verify(r => r.UpdateAsync(It.Is<Question>(q => 
            q.Status == QuestionStatus.TO_ARCHIVE && 
            q.Interval == 360 && 
            q.NextReview > DateTime.Today.AddDays(999999))), Times.Once);
    }

    [Fact]
    public async Task OnUnknown_FailuresBecomeThree_ArchivesQuestion()
    {
        var category = new Category { Id = 1, Name = "Languages", Reverse = false };
        var question = new Question 
        { 
            Id = 10, 
            CategoryId = 1, 
            Status = QuestionStatus.UNCHECKED,
            Interval = 10,
            StatisticsId = 5
        };
        var stats = new Statistics { Id = 5, Failures = 2 };

        _mockCategoryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
        _mockQuestionRepo.Setup(r => r.GetReviewQuestionsByCategoryAsync(1, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Question> { question });
        _mockStatisticsRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(stats);

        var vm = new ReviewSessionViewModel(
            _mockScreen.Object,
            1,
            null,
            _mockCategoryRepo.Object,
            _mockTopicRepo.Object,
            _mockQuestionRepo.Object,
            _mockStatisticsRepo.Object,
            _mockMediaRepo.Object
        );

        await vm.LoadSessionCommand.Execute().ToTask();
        await vm.UnknownCommand.Execute().ToTask();

        _mockQuestionRepo.Verify(r => r.UpdateAsync(It.Is<Question>(q => 
            q.Status == QuestionStatus.TO_ARCHIVE && 
            q.Interval == 360 && 
            q.NextReview > DateTime.Today.AddDays(999999))), Times.Once);
        _mockStatisticsRepo.Verify(r => r.UpdateAsync(It.Is<Statistics>(s => s.Failures == 3)), Times.Once);
    }

    [Fact]
    public async Task OnUnknown_BidirectionalMode_SkipsSecondDirection()
    {
        var category = new Category { Id = 1, Name = "Languages", Reverse = true };
        var question = new Question 
        { 
            Id = 10, 
            CategoryId = 1, 
            Status = QuestionStatus.UNCHECKED,
            Interval = 1,
            StatisticsId = 5
        };

        _mockCategoryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
        _mockQuestionRepo.Setup(r => r.GetReviewQuestionsByCategoryAsync(1, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Question> { question });
        _mockStatisticsRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Statistics { Id = 5, Failures = 0 });

        var vm = new ReviewSessionViewModel(
            _mockScreen.Object,
            1,
            null,
            _mockCategoryRepo.Object,
            _mockTopicRepo.Object,
            _mockQuestionRepo.Object,
            _mockStatisticsRepo.Object,
            _mockMediaRepo.Object
        );

        await vm.LoadSessionCommand.Execute().ToTask();
        await vm.UnknownCommand.Execute().ToTask();

        Assert.True(vm.State.IsFinished);
        _mockQuestionRepo.Verify(r => r.UpdateAsync(It.Is<Question>(q => q.Interval == 1 && q.Status == QuestionStatus.UNCHECKED)), Times.Once);
    }

    [Fact]
    public async Task LoadSession_WithGroupedQuestions_LoadsEntireGroupInSequenceWithoutDuplicates()
    {
        var category = new Category { Id = 1, Name = "Math", Reverse = false };
        var q1 = new Question { Id = 101, GroupId = 5, CategoryId = 1, Status = QuestionStatus.UNCHECKED, StatisticsId = 10 };
        var q2 = new Question { Id = 102, GroupId = 5, CategoryId = 1, Status = QuestionStatus.UNCHECKED, StatisticsId = 11 };
        var q3 = new Question { Id = 103, GroupId = 5, CategoryId = 1, Status = QuestionStatus.UNCHECKED, StatisticsId = 12 };

        _mockCategoryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
        _mockQuestionRepo.Setup(r => r.GetReviewQuestionsByCategoryAsync(1, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Question> { q1, q2 });
        _mockQuestionRepo.Setup(r => r.GetByGroupIdAsync(5))
            .ReturnsAsync(new List<Question> { q1, q2, q3 });

        var vm = new ReviewSessionViewModel(
            _mockScreen.Object,
            1,
            null,
            _mockCategoryRepo.Object,
            _mockTopicRepo.Object,
            _mockQuestionRepo.Object,
            _mockStatisticsRepo.Object,
            _mockMediaRepo.Object
        );

        await vm.LoadSessionCommand.Execute().ToTask();

        Assert.Equal(3, vm.State.TotalQuestionsCount);
        _mockQuestionRepo.Verify(r => r.GetByGroupIdAsync(5), Times.Exactly(2));
    }

    [Fact]
    public async Task OnKnow_GroupedQuestions_PostponesUpdateUntilAllKnown()
    {
        var category = new Category { Id = 1, Name = "Math", Reverse = false };
        var q1 = new Question { Id = 101, GroupId = 5, CategoryId = 1, Status = QuestionStatus.UNCHECKED, Interval = 1, StatisticsId = 10 };
        var q2 = new Question { Id = 102, GroupId = 5, CategoryId = 1, Status = QuestionStatus.UNCHECKED, Interval = 1, StatisticsId = 11 };

        _mockCategoryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
        _mockQuestionRepo.Setup(r => r.GetReviewQuestionsByCategoryAsync(1, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Question> { q1 });
        _mockQuestionRepo.Setup(r => r.GetByGroupIdAsync(5))
            .ReturnsAsync(new List<Question> { q1, q2 });
        _mockStatisticsRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new Statistics { Failures = 0 });

        var vm = new ReviewSessionViewModel(
            _mockScreen.Object,
            1,
            null,
            _mockCategoryRepo.Object,
            _mockTopicRepo.Object,
            _mockQuestionRepo.Object,
            _mockStatisticsRepo.Object,
            _mockMediaRepo.Object
        );

        await vm.LoadSessionCommand.Execute().ToTask();

        await vm.KnowCommand.Execute().ToTask();

        Assert.Equal(QuestionStatus.KNOWN, q1.Status);
        Assert.Equal(1, q1.Interval);
        _mockQuestionRepo.Verify(r => r.UpdateAsync(q1), Times.Once);
        _mockQuestionRepo.Verify(r => r.UpdateAsync(q2), Times.Never);

        await vm.KnowCommand.Execute().ToTask();

        Assert.Equal(QuestionStatus.UNCHECKED, q1.Status);
        Assert.Equal(3, q1.Interval);
        Assert.Equal(QuestionStatus.UNCHECKED, q2.Status);
        Assert.Equal(3, q2.Interval);

        _mockQuestionRepo.Verify(r => r.UpdateAsync(q1), Times.Exactly(2));
        _mockQuestionRepo.Verify(r => r.UpdateAsync(q2), Times.Exactly(2));
    }

    [Fact]
    public async Task OnUnknown_GroupedQuestions_ResetsIntervalAndIncrementsFailuresOnlyForIntervalTenPlus()
    {
        var category = new Category { Id = 1, Name = "Math", Reverse = false };
        var q1 = new Question { Id = 101, GroupId = 5, CategoryId = 1, Status = QuestionStatus.UNCHECKED, Interval = 10, StatisticsId = 10 };
        var q2 = new Question { Id = 102, GroupId = 5, CategoryId = 1, Status = QuestionStatus.UNCHECKED, Interval = 3, StatisticsId = 11 };
        var stats1 = new Statistics { Id = 10, Failures = 0 };
        var stats2 = new Statistics { Id = 11, Failures = 0 };

        _mockCategoryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
        _mockQuestionRepo.Setup(r => r.GetReviewQuestionsByCategoryAsync(1, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Question> { q1 });
        _mockQuestionRepo.Setup(r => r.GetByGroupIdAsync(5))
            .ReturnsAsync(new List<Question> { q1, q2 });
        _mockStatisticsRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(stats1);
        _mockStatisticsRepo.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(stats2);

        var vm = new ReviewSessionViewModel(
            _mockScreen.Object,
            1,
            null,
            _mockCategoryRepo.Object,
            _mockTopicRepo.Object,
            _mockQuestionRepo.Object,
            _mockStatisticsRepo.Object,
            _mockMediaRepo.Object
        );

        await vm.LoadSessionCommand.Execute().ToTask();

        await vm.UnknownCommand.Execute().ToTask();

        _mockStatisticsRepo.Verify(r => r.UpdateAsync(It.Is<Statistics>(s => s.Id == 10 && s.Failures == 1)), Times.Once);
        _mockStatisticsRepo.Verify(r => r.UpdateAsync(It.Is<Statistics>(s => s.Id == 11 && s.Failures == 1)), Times.Never);

        _mockQuestionRepo.Verify(r => r.UpdateAsync(It.Is<Question>(q => q.Id == 101 && q.Interval == 1 && q.Status == QuestionStatus.UNCHECKED)), Times.Once);
        _mockQuestionRepo.Verify(r => r.UpdateAsync(It.Is<Question>(q => q.Id == 102 && q.Interval == 1 && q.Status == QuestionStatus.UNCHECKED)), Times.Once);

        Assert.True(vm.State.IsFinished);
    }
}


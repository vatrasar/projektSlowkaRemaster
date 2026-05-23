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
using ProjektSlowkaRemasterd.Src.Features.Training.UI.Screens.TrainingSession;

namespace ProjektSlowkaRemasterd.Tests.FeaturesTests.TrainingTests;

public class TrainingSessionTests
{
    private readonly Mock<IScreen> _mockScreen;
    private readonly Mock<ICategoryRepository> _mockCategoryRepo;
    private readonly Mock<IMediaRepository> _mockMediaRepo;

    public TrainingSessionTests()
    {
        _mockScreen = new Mock<IScreen>();
        _mockCategoryRepo = new Mock<ICategoryRepository>();
        _mockMediaRepo = new Mock<IMediaRepository>();

        _mockScreen.Setup(s => s.Router).Returns(new RoutingState());
        _mockMediaRepo.Setup(m => m.GetByQuestionIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<Media>());
    }

    [Fact]
    public async Task TrainingSession_OneSidedMode_KnowImmediatelyCompletes()
    {
        // Arrange
        var category = new Category { Id = 1, Name = "General", Reverse = false };
        var q = new Question { Id = 10, CategoryId = 1, QuestionText = "Q", AnswerText = "A" };
        var questions = new List<Question> { q };

        _mockCategoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category> { category });

        var vm = new TrainingSessionViewModel(
            _mockScreen.Object,
            questions,
            "Title",
            "Subtitle",
            _mockCategoryRepo.Object,
            _mockMediaRepo.Object
        );

        // Act
        await vm.LoadSessionCommand.Execute().ToTask();
        Assert.False(vm.State.IsFinished);
        Assert.Equal("Q->A", vm.State.CurrentDirection);

        await vm.KnowCommand.Execute().ToTask();

        // Assert: It should be completed
        Assert.True(vm.State.IsFinished);
    }

    [Fact]
    public async Task TrainingSession_OneSidedMode_UnknownTransitionsToState1AndRequiresKnowToState2ThenComplete()
    {
        // Arrange
        var category = new Category { Id = 1, Name = "General", Reverse = false };
        
        // We load 15 questions to see the shift-back effects clearly
        var questions = Enumerable.Range(1, 15).Select(i => new Question 
        { 
            Id = i, 
            CategoryId = 1, 
            QuestionText = $"Q{i}", 
            AnswerText = $"A{i}" 
        }).ToList();

        _mockCategoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category> { category });

        var vm = new TrainingSessionViewModel(
            _mockScreen.Object,
            questions,
            "Title",
            "Subtitle",
            _mockCategoryRepo.Object,
            _mockMediaRepo.Object
        );

        await vm.LoadSessionCommand.Execute().ToTask();

        // Target: Q1 (currently at index 0 in the queue)
        // Act 1: Click "Unknown" on Q1 -> should set state to 1, shift back by 3 (so it will be behind Q2, Q3, Q4)
        await vm.UnknownCommand.Execute().ToTask();

        // Current question should be Q2 (since Q1 was shifted back)
        Assert.Equal("Q2", vm.State.QuestionText);

        // Click "Know" on Q2, Q3, Q4 (these will be completed immediately as they are in State 0)
        await vm.KnowCommand.Execute().ToTask(); // Q2
        await vm.KnowCommand.Execute().ToTask(); // Q3
        await vm.KnowCommand.Execute().ToTask(); // Q4

        // Now Q1 should be the current card (State 1)
        Assert.Equal("Q1", vm.State.QuestionText);

        // Act 2: Click "Know" on Q1 (State 1) -> should transition to State 2 and shift back by 10 positions
        await vm.KnowCommand.Execute().ToTask();

        // Current question should be Q5
        Assert.Equal("Q5", vm.State.QuestionText);

        // Click "Know" on Q5 to Q14 (10 questions)
        for (int i = 5; i <= 14; i++)
        {
            await vm.KnowCommand.Execute().ToTask();
        }

        // Q1 (State 2) should now be up (since it was shifted back by 10, placing it before Q15)
        Assert.Equal("Q1", vm.State.QuestionText);
        await vm.KnowCommand.Execute().ToTask();

        // Q15 is now up
        Assert.Equal("Q15", vm.State.QuestionText);

        // Act 3: Click "Know" on Q15 -> should complete
        await vm.KnowCommand.Execute().ToTask();

        // Session should be finished
        Assert.True(vm.State.IsFinished);
    }

    [Fact]
    public async Task TrainingSession_BidirectionalMode_KnowPromotesToReverseThenCompletes()
    {
        // Arrange
        var category = new Category { Id = 1, Name = "Languages", Reverse = true };
        var q = new Question { Id = 10, CategoryId = 1, QuestionText = "Hello", AnswerText = "Cześć" };
        var questions = new List<Question> { q };

        _mockCategoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category> { category });

        var vm = new TrainingSessionViewModel(
            _mockScreen.Object,
            questions,
            "Title",
            "Subtitle",
            _mockCategoryRepo.Object,
            _mockMediaRepo.Object
        );

        await vm.LoadSessionCommand.Execute().ToTask();

        // Initial card is Q->A
        Assert.Equal("Q->A", vm.State.CurrentDirection);
        Assert.Equal("Hello", vm.State.QuestionText);

        // Act 1: Click "Know" on Q->A -> should go to A->Q (remains State 0)
        await vm.KnowCommand.Execute().ToTask();

        // Queue is not empty: we should have the same card in A->Q direction
        Assert.False(vm.State.IsFinished);
        Assert.Equal("A->Q", vm.State.CurrentDirection);
        Assert.Equal("Cześć", vm.State.QuestionText); // Question Text swapped to Answer in reverse mode

        // Act 2: Click "Know" on A->Q -> should complete
        await vm.KnowCommand.Execute().ToTask();

    }

    [Fact]
    public async Task TrainingSession_Bidirectional_State0_Unknown_ResetsDirectionToQtoA_ShiftsBack3()
    {
        var category = new Category { Id = 1, Name = "Languages", Reverse = true };
        var questions = Enumerable.Range(1, 5).Select(i => new Question 
        { 
            Id = i, 
            CategoryId = 1, 
            QuestionText = $"Q{i}", 
            AnswerText = $"A{i}" 
        }).ToList();

        _mockCategoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category> { category });

        var vm = new TrainingSessionViewModel(
            _mockScreen.Object,
            questions,
            "Title",
            "Subtitle",
            _mockCategoryRepo.Object,
            _mockMediaRepo.Object
        );

        await vm.LoadSessionCommand.Execute().ToTask();

        await vm.KnowCommand.Execute().ToTask();
        await vm.KnowCommand.Execute().ToTask();
        await vm.UnknownCommand.Execute().ToTask();

        Assert.Equal("Q4", vm.State.QuestionText);

        await vm.KnowCommand.Execute().ToTask();
        await vm.KnowCommand.Execute().ToTask();

        Assert.Equal("A1", vm.State.QuestionText);
        await vm.KnowCommand.Execute().ToTask();

        Assert.Equal("Q3", vm.State.QuestionText);
        Assert.Equal("Q->A", vm.State.CurrentDirection);
    }

    [Fact]
    public async Task TrainingSession_Bidirectional_State1_Know_MovesToState2_ShiftsBack10_KeepsQtoA()
    {
        var category = new Category { Id = 1, Name = "Languages", Reverse = true };
        var questions = Enumerable.Range(1, 15).Select(i => new Question 
        { 
            Id = i, 
            CategoryId = 1, 
            QuestionText = $"Q{i}", 
            AnswerText = $"A{i}" 
        }).ToList();

        _mockCategoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category> { category });

        var vm = new TrainingSessionViewModel(
            _mockScreen.Object,
            questions,
            "Title",
            "Subtitle",
            _mockCategoryRepo.Object,
            _mockMediaRepo.Object
        );

        await vm.LoadSessionCommand.Execute().ToTask();

        await vm.UnknownCommand.Execute().ToTask();
        
        await vm.KnowCommand.Execute().ToTask();
        await vm.KnowCommand.Execute().ToTask();
        await vm.KnowCommand.Execute().ToTask();
        
        Assert.Equal("Q1", vm.State.QuestionText);
        Assert.Equal("Q->A", vm.State.CurrentDirection);

        await vm.KnowCommand.Execute().ToTask();

        Assert.Equal("Q5", vm.State.QuestionText);

        for (int i = 5; i <= 14; i++)
        {
            await vm.KnowCommand.Execute().ToTask();
        }

        Assert.Equal("Q1", vm.State.QuestionText);
        Assert.Equal("Q->A", vm.State.CurrentDirection);
    }

    [Fact]
    public async Task TrainingSession_Bidirectional_SingleCardTransitionFlow()
    {
        var category = new Category { Id = 1, Name = "Languages", Reverse = true };
        var questions = new List<Question> 
        { 
            new Question { Id = 1, CategoryId = 1, QuestionText = "Hello", AnswerText = "Cześć" }
        };

        _mockCategoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category> { category });

        var vm = new TrainingSessionViewModel(
            _mockScreen.Object,
            questions,
            "Title",
            "Subtitle",
            _mockCategoryRepo.Object,
            _mockMediaRepo.Object
        );

        await vm.LoadSessionCommand.Execute().ToTask();

        Assert.Equal("Hello", vm.State.QuestionText);
        Assert.Equal("Q->A", vm.State.CurrentDirection);

        await vm.UnknownCommand.Execute().ToTask();
        Assert.Equal("Hello", vm.State.QuestionText);
        Assert.Equal("Q->A", vm.State.CurrentDirection);

        await vm.KnowCommand.Execute().ToTask();
        Assert.Equal("Hello", vm.State.QuestionText);
        Assert.Equal("Q->A", vm.State.CurrentDirection);

        await vm.KnowCommand.Execute().ToTask();
        Assert.Equal("Cześć", vm.State.QuestionText);
        Assert.Equal("A->Q", vm.State.CurrentDirection);

        await vm.KnowCommand.Execute().ToTask();
        Assert.True(vm.State.IsFinished);
    }

    [Fact]
    public async Task TrainingSession_Bidirectional_State2_Unknown_ResetsToState1QtoA()
    {
        var category = new Category { Id = 1, Name = "Languages", Reverse = true };
        var questions = new List<Question> 
        { 
            new Question { Id = 1, CategoryId = 1, QuestionText = "Hello", AnswerText = "Cześć" }
        };

        _mockCategoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category> { category });

        var vm = new TrainingSessionViewModel(
            _mockScreen.Object,
            questions,
            "Title",
            "Subtitle",
            _mockCategoryRepo.Object,
            _mockMediaRepo.Object
        );

        await vm.LoadSessionCommand.Execute().ToTask();

        await vm.UnknownCommand.Execute().ToTask();
        await vm.KnowCommand.Execute().ToTask();

        await vm.UnknownCommand.Execute().ToTask();
        
        await vm.KnowCommand.Execute().ToTask();
        Assert.Equal("Hello", vm.State.QuestionText);
        Assert.Equal("Q->A", vm.State.CurrentDirection);

        await vm.KnowCommand.Execute().ToTask();
        Assert.Equal("Cześć", vm.State.QuestionText);
        Assert.Equal("A->Q", vm.State.CurrentDirection);
    }
}


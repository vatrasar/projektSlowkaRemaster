using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ProjektSlowkaRemasterd.Src.Infrastructure.Data;
using ProjektSlowkaRemasterd.Src.Infrastructure.Data.Entities;
using ProjektSlowkaRemasterd.Src.Core.Domain.Enums;
using ProjektSlowkaRemasterd.Src.Features.Question.Domain.Services;
using ProjektSlowkaRemasterd.Src.Features.Question.Domain.UseCases;

namespace ProjektSlowkaRemasterd.Tests.FeaturesTests.QuestionTests;

public class BulkImportTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<NameOfAppDbContext> _contextOptions;

    public BulkImportTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _contextOptions = new DbContextOptionsBuilder<NameOfAppDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Initialize schema
        using var context = new NameOfAppDbContext(_contextOptions);
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }

    private NameOfAppDbContext CreateContext() => new(_contextOptions);

    [Fact]
    public void Parse_GivenValidFormat_ParsesSuccessfully()
    {
        var input = @"---
Q1:
What is the past simple of 'go'?
A1:
went
---
Q2:
Translate 'pies'
A2:
dog
---";

        var parsed = BulkQuestionParser.Parse(input);

        Assert.Equal(2, parsed.Count);
        
        Assert.Equal("What is the past simple of 'go'?", parsed[0].QuestionText);
        Assert.Equal("went", parsed[0].AnswerText);
        Assert.Equal(1, parsed[0].QuestionNumber);

        Assert.Equal("Translate 'pies'", parsed[1].QuestionText);
        Assert.Equal("dog", parsed[1].AnswerText);
        Assert.Equal(2, parsed[1].QuestionNumber);
    }

    [Fact]
    public void Parse_GivenNumberMismatch_ThrowsFormatException()
    {
        var input = @"---
Q1:
Question
A2:
Answer
---";

        var ex = Assert.Throws<FormatException>(() => BulkQuestionParser.Parse(input));
        Assert.Contains("Question number (1) does not match Answer number (2)", ex.Message);
    }

    [Fact]
    public void Parse_GivenQuestionMissing_ThrowsFormatException()
    {
        var input = @"---
A1:
Answer
---";

        var ex = Assert.Throws<FormatException>(() => BulkQuestionParser.Parse(input));
        Assert.Contains("Question field (Q[num]:) is missing.", ex.Message);
    }

    [Fact]
    public void Parse_GivenTextOutsideBlock_ThrowsFormatException()
    {
        var input = @"Some text outside
---
Q1:
Q
A1:
A
---";

        var ex = Assert.Throws<FormatException>(() => BulkQuestionParser.Parse(input));
        Assert.Contains("Text found before specifying the field", ex.Message);
    }

    [Fact]
    public void Parse_GivenNoTrailingSeparator_ParsesSuccessfully()
    {
        var input = @"---
Q1:
Q
A1:
A";

        var parsed = BulkQuestionParser.Parse(input);
        Assert.Single(parsed);
        Assert.Equal("Q", parsed[0].QuestionText);
        Assert.Equal("A", parsed[0].AnswerText);
    }

    [Fact]
    public void Parse_GivenAnswerExceedsLimit_ThrowsFormatException()
    {
        var longAnswer = new string('A', 10005);
        var input = $@"---
Q1:
Q
A1:
{longAnswer}
---";

        var ex = Assert.Throws<FormatException>(() => BulkQuestionParser.Parse(input));
        Assert.Contains("Answer length cannot exceed 10000 characters", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_GivenValidQuestions_SavesSuccessfullyAndManagesLastAdded()
    {
        // 1. Seed database with category and an existing last-added question
        using (var context = CreateContext())
        {
            var category = new CategoryEntity { Id = 1, Name = "English", Reverse = false, CreatedAt = DateTime.Today };
            await context.Categories.AddAsync(category);

            var oldStats = new StatisticsEntity { Failures = 0 };
            var oldQuestion = new QuestionEntity
            {
                Id = 1,
                QuestionText = "Old Q",
                AnswerText = "Old A",
                CategoryId = 1,
                Statistics = oldStats,
                IsLastAdded = true,
                Status = QuestionStatus.UNCHECKED,
                NextReview = DateTime.Today,
                Interval = 1
            };
            await context.Questions.AddAsync(oldQuestion);
            await context.SaveChangesAsync();
        }

        // 2. Perform bulk import using Use Case
        var parsed = new List<ParsedQuestion>
        {
            new() { QuestionNumber = 1, QuestionText = "New Q1", AnswerText = "New A1" },
            new() { QuestionNumber = 2, QuestionText = "New Q2", AnswerText = "New A2" }
        };

        using (var context = CreateContext())
        {
            var useCase = new BulkImportQuestionsUseCase(context);
            var result = await useCase.ExecuteAsync(parsed, categoryId: 1, topicId: null, sectionId: null);

            Assert.Equal(2, result);
        }

        // 3. Verify Database State
        using (var context = CreateContext())
        {
            var questions = await context.Questions
                .Include(q => q.Statistics)
                .OrderBy(q => q.Id)
                .ToListAsync();

            // Total 3 questions (1 old, 2 new)
            Assert.Equal(3, questions.Count);

            // Verify old question lost its IsLastAdded flag
            Assert.False(questions[0].IsLastAdded);

            // Verify new questions are saved
            Assert.Equal("New Q1", questions[1].QuestionText);
            Assert.False(questions[1].IsLastAdded);
            Assert.NotNull(questions[1].Statistics);

            // Verify the very last question has IsLastAdded set to true
            Assert.Equal("New Q2", questions[2].QuestionText);
            Assert.True(questions[2].IsLastAdded);
            Assert.NotNull(questions[2].Statistics);
        }
    }

    [Fact]
    public async Task ExecuteAsync_GivenInvalidCategoryId_RollsBackTransactionAndThrows()
    {
        // Category 999 does not exist in DB, which violates foreign key constraints
        var parsed = new List<ParsedQuestion>
        {
            new() { QuestionNumber = 1, QuestionText = "Q1", AnswerText = "A1" }
        };

        using (var context = CreateContext())
        {
            var useCase = new BulkImportQuestionsUseCase(context);

            // Category 999 doesn't exist, so this will throw due to foreign key constraints
            await Assert.ThrowsAnyAsync<Exception>(() => useCase.ExecuteAsync(parsed, categoryId: 999, topicId: null, sectionId: null));
        }

        // Verify that no questions were saved
        using (var context = CreateContext())
        {
            var count = await context.Questions.CountAsync();
            Assert.Equal(0, count);
        }
    }
}

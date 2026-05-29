using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjektSlowkaRemasterd.Src.Infrastructure.Data;
using ProjektSlowkaRemasterd.Src.Infrastructure.Data.Entities;
using ProjektSlowkaRemasterd.Src.Core.Domain.Enums;
using ProjektSlowkaRemasterd.Src.Features.Question.Domain.Services;

namespace ProjektSlowkaRemasterd.Src.Features.Question.Domain.UseCases;

/// <summary>
/// Use Case: BulkImportQuestionsUseCase
/// Purpose: Performs a transactional import of questions into a specific category,
/// optionally under a specific topic and section.
/// Invoked by: BulkImportViewModel
/// </summary>
public class BulkImportQuestionsUseCase
{
    private readonly NameOfAppDbContext _context;

    public BulkImportQuestionsUseCase(NameOfAppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<int> ExecuteAsync(List<ParsedQuestion> parsedQuestions, int categoryId, int? topicId, int? sectionId)
    {
        if (parsedQuestions == null || !parsedQuestions.Any())
        {
            return 0;
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await ProcessImportAsync(parsedQuestions, categoryId, topicId, sectionId);
            await transaction.CommitAsync();
            return parsedQuestions.Count;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task ProcessImportAsync(List<ParsedQuestion> parsedQuestions, int categoryId, int? topicId, int? sectionId)
    {
        await ClearCategoryLastAddedAsync(categoryId);
        await InsertQuestionsAsync(parsedQuestions, categoryId, topicId, sectionId);
        await _context.SaveChangesAsync();
    }

    private async Task ClearCategoryLastAddedAsync(int categoryId)
    {
        var lastAdded = await _context.Questions
            .Where(q => q.CategoryId == categoryId && q.IsLastAdded)
            .ToListAsync();

        foreach (var q in lastAdded)
        {
            q.IsLastAdded = false;
        }
    }

    private async Task InsertQuestionsAsync(List<ParsedQuestion> parsedQuestions, int categoryId, int? topicId, int? sectionId)
    {
        int count = parsedQuestions.Count;
        for (int i = 0; i < count; i++)
        {
            await InsertSingleQuestionAsync(parsedQuestions[i], categoryId, topicId, sectionId, i == count - 1);
        }
    }

    private async Task InsertSingleQuestionAsync(ParsedQuestion parsed, int categoryId, int? topicId, int? sectionId, bool isLast)
    {
        var stats = new StatisticsEntity { Failures = 0 };
        await _context.Statistics.AddAsync(stats);

        var question = new QuestionEntity
        {
            QuestionText = parsed.QuestionText,
            AnswerText = parsed.AnswerText,
            CategoryId = categoryId,
            TopicId = topicId,
            SectionId = sectionId,
            Statistics = stats,
            Status = QuestionStatus.UNCHECKED,
            IsProblematic = false,
            IsLastAdded = isLast,
            IsNotion = false,
            NextReview = DateTime.Today,
            Interval = 1
        };

        await _context.Questions.AddAsync(question);
    }
}

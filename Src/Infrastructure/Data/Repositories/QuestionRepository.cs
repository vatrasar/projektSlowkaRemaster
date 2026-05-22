using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;
using ProjektSlowkaRemasterd.Src.Core.Domain.RepositoryContracts;
using ProjektSlowkaRemasterd.Src.Infrastructure.Data.Entities;

namespace ProjektSlowkaRemasterd.Src.Infrastructure.Data.Repositories;

public class QuestionRepository : IQuestionRepository
{
    private readonly NameOfAppDbContext _context;

    public QuestionRepository(NameOfAppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Question>> GetAllAsync()
    {
        var entities = await _context.Questions.ToListAsync();
        return entities.Select(MapToDomain);
    }

    /// <inheritdoc />
    public async Task<Question?> GetByIdAsync(int id)
    {
        var entity = await _context.Questions.FindAsync(id);
        return entity == null ? null : MapToDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Question>> GetByCategoryIdAsync(int categoryId)
    {
        var entities = await _context.Questions.Where(q => q.CategoryId == categoryId).ToListAsync();
        return entities.Select(MapToDomain);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Question>> GetByTopicIdAsync(int topicId)
    {
        var entities = await _context.Questions.Where(q => q.TopicId == topicId).ToListAsync();
        return entities.Select(MapToDomain);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Question>> GetBySectionIdAsync(int sectionId)
    {
        var entities = await _context.Questions.Where(q => q.SectionId == sectionId).ToListAsync();
        return entities.Select(MapToDomain);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Question>> GetReviewQuestionsAsync(DateTime maxDate)
    {
        var entities = await _context.Questions
            .Where(q => q.NextReview <= maxDate)
            .ToListAsync();
        return entities.Select(MapToDomain);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Question>> GetReviewQuestionsByCategoryAsync(int categoryId, DateTime maxDate)
    {
        var entities = await _context.Questions
            .Where(q => q.CategoryId == categoryId && q.NextReview <= maxDate)
            .ToListAsync();
        return entities.Select(MapToDomain);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Question>> GetReviewQuestionsByTopicAsync(int topicId, DateTime maxDate)
    {
        var entities = await _context.Questions
            .Where(q => q.TopicId == topicId && q.NextReview <= maxDate)
            .ToListAsync();
        return entities.Select(MapToDomain);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Question>> GetByGroupIdAsync(int groupId)
    {
        var entities = await _context.Questions.Where(q => q.GroupId == groupId).ToListAsync();
        return entities.Select(MapToDomain);
    }

    /// <inheritdoc />
    public async Task<Question> AddAsync(Question question)
    {
        var entity = MapToEntity(question);
        await _context.Questions.AddAsync(entity);
        await _context.SaveChangesAsync();
        question.Id = entity.Id;
        return question;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Question question)
    {
        var entity = await _context.Questions.FindAsync(question.Id);
        if (entity != null)
        {
            entity.QuestionText = question.QuestionText;
            entity.AnswerText = question.AnswerText;
            entity.CategoryId = question.CategoryId;
            entity.TopicId = question.TopicId;
            entity.SectionId = question.SectionId;
            entity.StatisticsId = question.StatisticsId;
            entity.GroupId = question.GroupId;
            entity.Status = question.Status;
            entity.IsProblematic = question.IsProblematic;
            entity.IsLastAdded = question.IsLastAdded;
            entity.IsNotion = question.IsNotion;
            entity.NextReview = question.NextReview;
            entity.Interval = question.Interval;

            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Questions.FindAsync(id);
        if (entity != null)
        {
            _context.Questions.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task<Question?> GetLastAddedAsync(int categoryId)
    {
        var entity = await _context.Questions.FirstOrDefaultAsync(q => q.CategoryId == categoryId && q.IsLastAdded);
        return entity == null ? null : MapToDomain(entity);
    }

    /// <inheritdoc />
    public async Task ClearLastAddedFlagAsync(int categoryId)
    {
        var lastAdded = await _context.Questions.Where(q => q.CategoryId == categoryId && q.IsLastAdded).ToListAsync();
        foreach (var q in lastAdded)
        {
            q.IsLastAdded = false;
        }
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<int> GenerateNewGroupIdAsync()
    {
        var maxId = await _context.Questions.MaxAsync(q => (int?)q.GroupId) ?? 0;
        return maxId + 1;
    }

    private static Question MapToDomain(QuestionEntity entity) => new()
    {
        Id = entity.Id,
        QuestionText = entity.QuestionText,
        AnswerText = entity.AnswerText,
        CategoryId = entity.CategoryId,
        TopicId = entity.TopicId,
        SectionId = entity.SectionId,
        StatisticsId = entity.StatisticsId,
        GroupId = entity.GroupId,
        Status = entity.Status,
        IsProblematic = entity.IsProblematic,
        IsLastAdded = entity.IsLastAdded,
        IsNotion = entity.IsNotion,
        NextReview = entity.NextReview,
        Interval = entity.Interval
    };

    private static QuestionEntity MapToEntity(Question domain) => new()
    {
        Id = domain.Id,
        QuestionText = domain.QuestionText,
        AnswerText = domain.AnswerText,
        CategoryId = domain.CategoryId,
        TopicId = domain.TopicId,
        SectionId = domain.SectionId,
        StatisticsId = domain.StatisticsId,
        GroupId = domain.GroupId,
        Status = domain.Status,
        IsProblematic = domain.IsProblematic,
        IsLastAdded = domain.IsLastAdded,
        IsNotion = domain.IsNotion,
        NextReview = domain.NextReview,
        Interval = domain.Interval
    };
}

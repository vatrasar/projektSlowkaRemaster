using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;

namespace ProjektSlowkaRemasterd.Src.Core.Domain.RepositoryContracts;

public interface IQuestionRepository
{
    /// <summary>
    /// Retrieves all questions.
    /// </summary>
    Task<IEnumerable<Question>> GetAllAsync();

    /// <summary>
    /// Retrieves a question by its unique identifier.
    /// </summary>
    Task<Question?> GetByIdAsync(int id);

    /// <summary>
    /// Retrieves all questions in a specific category.
    /// </summary>
    Task<IEnumerable<Question>> GetByCategoryIdAsync(int categoryId);

    /// <summary>
    /// Retrieves all questions in a specific topic.
    /// </summary>
    Task<IEnumerable<Question>> GetByTopicIdAsync(int topicId);

    /// <summary>
    /// Retrieves all questions in a specific section.
    /// </summary>
    Task<IEnumerable<Question>> GetBySectionIdAsync(int sectionId);

    /// <summary>
    /// Retrieves all questions scheduled for review (nextReview &lt;= maxDate).
    /// </summary>
    Task<IEnumerable<Question>> GetReviewQuestionsAsync(DateTime maxDate);

    /// <summary>
    /// Retrieves questions scheduled for review in a category (nextReview &lt;= maxDate).
    /// </summary>
    Task<IEnumerable<Question>> GetReviewQuestionsByCategoryAsync(int categoryId, DateTime maxDate);

    /// <summary>
    /// Retrieves questions scheduled for review in a topic (nextReview &lt;= maxDate).
    /// </summary>
    Task<IEnumerable<Question>> GetReviewQuestionsByTopicAsync(int topicId, DateTime maxDate);

    /// <summary>
    /// Retrieves all questions that belong to a specific group (matching GroupId).
    /// </summary>
    Task<IEnumerable<Question>> GetByGroupIdAsync(int groupId);

    /// <summary>
    /// Adds a new question.
    /// </summary>
    Task<Question> AddAsync(Question question);

    /// <summary>
    /// Updates an existing question.
    /// </summary>
    Task UpdateAsync(Question question);

    /// <summary>
    /// Deletes a question by its identifier.
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// Retrieves the last added question in a category (where IsLastAdded is true).
    /// </summary>
    Task<Question?> GetLastAddedAsync(int categoryId);

    /// <summary>
    /// Resets the IsLastAdded flag to false for all questions in the category.
    /// </summary>
    Task ClearLastAddedFlagAsync(int categoryId);

    /// <summary>
    /// Generates a new unique group identifier.
    /// </summary>
    Task<int> GenerateNewGroupIdAsync();
}

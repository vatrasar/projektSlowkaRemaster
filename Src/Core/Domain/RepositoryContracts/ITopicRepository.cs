using System.Collections.Generic;
using System.Threading.Tasks;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;

namespace ProjektSlowkaRemasterd.Src.Core.Domain.RepositoryContracts;

public interface ITopicRepository
{
    /// <summary>
    /// Retrieves a topic by its unique identifier.
    /// </summary>
    Task<Topic?> GetByIdAsync(int id);

    /// <summary>
    /// Retrieves all topics belonging to a category.
    /// </summary>
    Task<IEnumerable<Topic>> GetByCategoryIdAsync(int categoryId);

    /// <summary>
    /// Retrieves a topic by name within a specific category.
    /// </summary>
    Task<Topic?> GetByNameAndCategoryAsync(int categoryId, string name);

    /// <summary>
    /// Adds a new topic.
    /// </summary>
    Task<Topic> AddAsync(Topic topic);

    /// <summary>
    /// Updates an existing topic.
    /// </summary>
    Task UpdateAsync(Topic topic);
}

using System.Collections.Generic;
using System.Threading.Tasks;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;

namespace ProjektSlowkaRemasterd.Src.Core.Domain.RepositoryContracts;

public interface ISectionRepository
{
    /// <summary>
    /// Retrieves a section by its identifier.
    /// </summary>
    Task<Section?> GetByIdAsync(int id);

    /// <summary>
    /// Retrieves sections belonging to a specific topic.
    /// </summary>
    Task<IEnumerable<Section>> GetByTopicIdAsync(int topicId);

    /// <summary>
    /// Retrieves a section by name within a topic.
    /// </summary>
    Task<Section?> GetByNameAndTopicAsync(int topicId, string name);

    /// <summary>
    /// Adds a new section.
    /// </summary>
    Task<Section> AddAsync(Section section);

    /// <summary>
    /// Updates an existing section.
    /// </summary>
    Task UpdateAsync(Section section);

    /// <summary>
    /// Deletes a section by its identifier.
    /// </summary>
    Task DeleteAsync(int id);
}

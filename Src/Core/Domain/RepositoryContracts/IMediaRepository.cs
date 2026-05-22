using System.Collections.Generic;
using System.Threading.Tasks;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;

namespace ProjektSlowkaRemasterd.Src.Core.Domain.RepositoryContracts;

public interface IMediaRepository
{
    /// <summary>
    /// Retrieves a media record by id.
    /// </summary>
    Task<Media?> GetByIdAsync(int id);

    /// <summary>
    /// Retrieves all media associated with a question.
    /// </summary>
    Task<IEnumerable<Media>> GetByQuestionIdAsync(int questionId);

    /// <summary>
    /// Adds a new media record.
    /// </summary>
    Task<Media> AddAsync(Media media);

    /// <summary>
    /// Deletes a media record by id.
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// Deletes all media records associated with a question.
    /// </summary>
    Task DeleteByQuestionIdAsync(int questionId);

    /// <summary>
    /// Updates an existing media record.
    /// </summary>
    Task UpdateAsync(Media media);
}

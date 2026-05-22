using System.Threading.Tasks;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;

namespace ProjektSlowkaRemasterd.Src.Core.Domain.RepositoryContracts;

public interface IStatisticsRepository
{
    /// <summary>
    /// Retrieves statistics by id.
    /// </summary>
    Task<Statistics?> GetByIdAsync(int id);

    /// <summary>
    /// Adds a new statistics record.
    /// </summary>
    Task<Statistics> AddAsync(Statistics statistics);

    /// <summary>
    /// Updates an existing statistics record.
    /// </summary>
    Task UpdateAsync(Statistics statistics);

    /// <summary>
    /// Deletes a statistics record.
    /// </summary>
    Task DeleteAsync(int id);
}

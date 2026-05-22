using System.Collections.Generic;
using System.Threading.Tasks;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;

namespace ProjektSlowkaRemasterd.Src.Core.Domain.RepositoryContracts;

public interface ICategoryRepository
{
    /// <summary>
    /// Retrieves a category by its unique identifier.
    /// </summary>
    Task<Category?> GetByIdAsync(int id);

    /// <summary>
    /// Retrieves a category by its unique name.
    /// </summary>
    Task<Category?> GetByNameAsync(string name);

    /// <summary>
    /// Retrieves all categories.
    /// </summary>
    Task<IEnumerable<Category>> GetAllAsync();

    /// <summary>
    /// Adds a new category to the repository.
    /// </summary>
    Task<Category> AddAsync(Category category);

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    Task UpdateAsync(Category category);

    /// <summary>
    /// Deletes a category by its identifier.
    /// </summary>
    Task DeleteAsync(int id);
}

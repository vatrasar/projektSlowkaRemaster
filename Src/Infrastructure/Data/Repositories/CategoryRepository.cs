using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;
using ProjektSlowkaRemasterd.Src.Core.Domain.RepositoryContracts;
using ProjektSlowkaRemasterd.Src.Infrastructure.Data.Entities;

namespace ProjektSlowkaRemasterd.Src.Infrastructure.Data.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly NameOfAppDbContext _context;

    public CategoryRepository(NameOfAppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Category?> GetByIdAsync(int id)
    {
        var entity = await _context.Categories.FindAsync(id);
        return entity == null ? null : MapToDomain(entity);
    }

    /// <inheritdoc />
    public async Task<Category?> GetByNameAsync(string name)
    {
        var entity = await _context.Categories.FirstOrDefaultAsync(c => c.Name == name);
        return entity == null ? null : MapToDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        var entities = await _context.Categories.ToListAsync();
        return entities.Select(MapToDomain);
    }

    /// <inheritdoc />
    public async Task<Category> AddAsync(Category category)
    {
        var entity = MapToEntity(category);
        await _context.Categories.AddAsync(entity);
        await _context.SaveChangesAsync();
        category.Id = entity.Id;
        return category;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Category category)
    {
        var entity = await _context.Categories.FindAsync(category.Id);
        if (entity != null)
        {
            entity.Name = category.Name;
            entity.Reverse = category.Reverse;
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Categories.FindAsync(id);
        if (entity != null)
        {
            _context.Categories.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    private static Category MapToDomain(CategoryEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Reverse = entity.Reverse,
        CreatedAt = entity.CreatedAt
    };

    private static CategoryEntity MapToEntity(Category domain) => new()
    {
        Id = domain.Id,
        Name = domain.Name,
        Reverse = domain.Reverse,
        CreatedAt = domain.CreatedAt
    };
}

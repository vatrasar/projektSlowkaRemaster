using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;
using ProjektSlowkaRemasterd.Src.Core.Domain.RepositoryContracts;
using ProjektSlowkaRemasterd.Src.Infrastructure.Data.Entities;

namespace ProjektSlowkaRemasterd.Src.Infrastructure.Data.Repositories;

public class TopicRepository : ITopicRepository
{
    private readonly NameOfAppDbContext _context;

    public TopicRepository(NameOfAppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Topic?> GetByIdAsync(int id)
    {
        var entity = await _context.Topics.FindAsync(id);
        return entity == null ? null : MapToDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Topic>> GetByCategoryIdAsync(int categoryId)
    {
        var entities = await _context.Topics.Where(t => t.CategoryId == categoryId).ToListAsync();
        return entities.Select(MapToDomain);
    }

    /// <inheritdoc />
    public async Task<Topic?> GetByNameAndCategoryAsync(int categoryId, string name)
    {
        var entity = await _context.Topics.FirstOrDefaultAsync(t => t.CategoryId == categoryId && t.Name == name);
        return entity == null ? null : MapToDomain(entity);
    }

    /// <inheritdoc />
    public async Task<Topic> AddAsync(Topic topic)
    {
        var entity = MapToEntity(topic);
        await _context.Topics.AddAsync(entity);
        await _context.SaveChangesAsync();
        topic.Id = entity.Id;
        return topic;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Topic topic)
    {
        var entity = await _context.Topics.FindAsync(topic.Id);
        if (entity != null)
        {
            entity.Name = topic.Name;
            await _context.SaveChangesAsync();
        }
    }

    private static Topic MapToDomain(TopicEntity entity) => new()
    {
        Id = entity.Id,
        CategoryId = entity.CategoryId,
        Name = entity.Name
    };

    private static TopicEntity MapToEntity(Topic domain) => new()
    {
        Id = domain.Id,
        CategoryId = domain.CategoryId,
        Name = domain.Name
    };
}

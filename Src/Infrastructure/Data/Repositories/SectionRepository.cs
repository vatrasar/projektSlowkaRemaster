using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;
using ProjektSlowkaRemasterd.Src.Core.Domain.RepositoryContracts;
using ProjektSlowkaRemasterd.Src.Infrastructure.Data.Entities;

namespace ProjektSlowkaRemasterd.Src.Infrastructure.Data.Repositories;

public class SectionRepository : ISectionRepository
{
    private readonly NameOfAppDbContext _context;

    public SectionRepository(NameOfAppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Section?> GetByIdAsync(int id)
    {
        var entity = await _context.Sections.FindAsync(id);
        return entity == null ? null : MapToDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Section>> GetByTopicIdAsync(int topicId)
    {
        var entities = await _context.Sections.Where(s => s.TopicId == topicId).ToListAsync();
        return entities.Select(MapToDomain);
    }

    /// <inheritdoc />
    public async Task<Section?> GetByNameAndTopicAsync(int topicId, string name)
    {
        var entity = await _context.Sections.FirstOrDefaultAsync(s => s.TopicId == topicId && s.Name == name);
        return entity == null ? null : MapToDomain(entity);
    }

    /// <inheritdoc />
    public async Task<Section> AddAsync(Section section)
    {
        var entity = MapToEntity(section);
        await _context.Sections.AddAsync(entity);
        await _context.SaveChangesAsync();
        section.Id = entity.Id;
        return section;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Section section)
    {
        var entity = await _context.Sections.FindAsync(section.Id);
        if (entity != null)
        {
            entity.Name = section.Name;
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Sections.FindAsync(id);
        if (entity != null)
        {
            _context.Sections.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    private static Section MapToDomain(SectionEntity entity) => new()
    {
        Id = entity.Id,
        TopicId = entity.TopicId,
        Name = entity.Name
    };

    private static SectionEntity MapToEntity(Section domain) => new()
    {
        Id = domain.Id,
        TopicId = domain.TopicId,
        Name = domain.Name
    };
}

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;
using ProjektSlowkaRemasterd.Src.Core.Domain.RepositoryContracts;
using ProjektSlowkaRemasterd.Src.Infrastructure.Data.Entities;

namespace ProjektSlowkaRemasterd.Src.Infrastructure.Data.Repositories;

public class MediaRepository : IMediaRepository
{
    private readonly NameOfAppDbContext _context;

    public MediaRepository(NameOfAppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Media?> GetByIdAsync(int id)
    {
        var entity = await _context.Media.FindAsync(id);
        return entity == null ? null : MapToDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Media>> GetByQuestionIdAsync(int questionId)
    {
        var entities = await _context.Media.Where(m => m.QuestionId == questionId).ToListAsync();
        return entities.Select(MapToDomain);
    }

    /// <inheritdoc />
    public async Task<Media> AddAsync(Media media)
    {
        var entity = MapToEntity(media);
        await _context.Media.AddAsync(entity);
        await _context.SaveChangesAsync();
        media.Id = entity.Id;
        return media;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Media.FindAsync(id);
        if (entity != null)
        {
            _context.Media.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task DeleteByQuestionIdAsync(int questionId)
    {
        var entities = await _context.Media.Where(m => m.QuestionId == questionId).ToListAsync();
        if (entities.Count > 0)
        {
            _context.Media.RemoveRange(entities);
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Media media)
    {
        var entity = await _context.Media.FindAsync(media.Id);
        if (entity != null)
        {
            entity.Filename = media.Filename;
            entity.QuestionId = media.QuestionId;
            entity.Status = media.Status;
            await _context.SaveChangesAsync();
        }
    }


    private static Media MapToDomain(MediaEntity entity) => new()
    {
        Id = entity.Id,
        Filename = entity.Filename,
        QuestionId = entity.QuestionId,
        Status = entity.Status
    };

    private static MediaEntity MapToEntity(Media domain) => new()
    {
        Id = domain.Id,
        Filename = domain.Filename,
        QuestionId = domain.QuestionId,
        Status = domain.Status
    };
}

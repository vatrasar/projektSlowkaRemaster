using System.Threading.Tasks;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;
using ProjektSlowkaRemasterd.Src.Core.Domain.RepositoryContracts;
using ProjektSlowkaRemasterd.Src.Infrastructure.Data.Entities;

namespace ProjektSlowkaRemasterd.Src.Infrastructure.Data.Repositories;

public class StatisticsRepository : IStatisticsRepository
{
    private readonly NameOfAppDbContext _context;

    public StatisticsRepository(NameOfAppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Statistics?> GetByIdAsync(int id)
    {
        var entity = await _context.Statistics.FindAsync(id);
        return entity == null ? null : MapToDomain(entity);
    }

    /// <inheritdoc />
    public async Task<Statistics> AddAsync(Statistics statistics)
    {
        var entity = MapToEntity(statistics);
        await _context.Statistics.AddAsync(entity);
        await _context.SaveChangesAsync();
        statistics.Id = entity.Id;
        return statistics;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Statistics statistics)
    {
        var entity = await _context.Statistics.FindAsync(statistics.Id);
        if (entity != null)
        {
            entity.Failures = statistics.Failures;
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Statistics.FindAsync(id);
        if (entity != null)
        {
            _context.Statistics.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    private static Statistics MapToDomain(StatisticsEntity entity) => new()
    {
        Id = entity.Id,
        Failures = entity.Failures
    };

    private static StatisticsEntity MapToEntity(Statistics domain) => new()
    {
        Id = domain.Id,
        Failures = domain.Failures
    };
}

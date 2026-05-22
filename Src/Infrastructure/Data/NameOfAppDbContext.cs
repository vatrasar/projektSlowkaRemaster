using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using ProjektSlowkaRemasterd.Src.Infrastructure.Data.Entities;

namespace ProjektSlowkaRemasterd.Src.Infrastructure.Data;

public class NameOfAppDbContext : DbContext
{
    public DbSet<CategoryEntity> Categories => Set<CategoryEntity>();
    public DbSet<TopicEntity> Topics => Set<TopicEntity>();
    public DbSet<SectionEntity> Sections => Set<SectionEntity>();
    public DbSet<QuestionEntity> Questions => Set<QuestionEntity>();
    public DbSet<StatisticsEntity> Statistics => Set<StatisticsEntity>();
    public DbSet<MediaEntity> Media => Set<MediaEntity>();

    public NameOfAppDbContext()
    {
    }

    public NameOfAppDbContext(DbContextOptions<NameOfAppDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "slowka.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Category configurations
        modelBuilder.Entity<CategoryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).IsRequired();
        });

        // Topic configurations
        modelBuilder.Entity<TopicEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.CategoryId, e.Name }).IsUnique();
            entity.Property(e => e.Name).IsRequired();

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Topics)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Section configurations
        modelBuilder.Entity<SectionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TopicId, e.Name }).IsUnique();
            entity.Property(e => e.Name).IsRequired();

            entity.HasOne(e => e.Topic)
                .WithMany(t => t.Sections)
                .HasForeignKey(e => e.TopicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Statistics configurations
        modelBuilder.Entity<StatisticsEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        // Question configurations
        modelBuilder.Entity<QuestionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AnswerText).HasMaxLength(10000).IsRequired();

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Questions)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Topic)
                .WithMany(t => t.Questions)
                .HasForeignKey(e => e.TopicId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Section)
                .WithMany(s => s.Questions)
                .HasForeignKey(e => e.SectionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Statistics)
                .WithOne(s => s.Question)
                .HasForeignKey<QuestionEntity>(e => e.StatisticsId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Media configurations
        modelBuilder.Entity<MediaEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Filename).IsRequired();

            entity.HasOne(e => e.Question)
                .WithMany(q => q.Media)
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

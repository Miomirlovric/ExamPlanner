using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Persistence;

public class ExamPlannerDbContext(DbContextOptions<ExamPlannerDbContext> options) : DbContext(options)
{
    public DbSet<ExamEntity> Exams => Set<ExamEntity>();
    public DbSet<ExamSection> ExamSections => Set<ExamSection>();
    public DbSet<GraphEntity> Graphs => Set<GraphEntity>();
    public DbSet<GraphRelation> GraphRelations => Set<GraphRelation>();
    public DbSet<FileEntity> Files => Set<FileEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExamEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired();

            entity.HasMany(e => e.Sections)
                  .WithOne(s => s.ExamEntity)
                  .HasForeignKey(s => s.ExamEntityId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExamSection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.Question).IsRequired();

            entity.HasOne(s => s.GraphEntity)
                  .WithMany()
                  .HasForeignKey(s => s.GraphEntityId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GraphEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.HasOne(g => g.File)
                  .WithOne(f => f.Graph)
                  .HasForeignKey<GraphEntity>(g => g.FileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(g => g.GraphRelations)
                  .WithOne(r => r.GraphEntity)
                  .HasForeignKey(r => r.GraphEntityId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GraphRelation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.A).IsRequired();
            entity.Property(e => e.B).IsRequired();
        });

        modelBuilder.Entity<FileEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Path).IsRequired();
        });
    }
}

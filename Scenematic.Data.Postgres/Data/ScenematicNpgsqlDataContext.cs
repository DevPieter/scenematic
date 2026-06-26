using Microsoft.EntityFrameworkCore;
using Scenematic.Data.Models.Entities.Cover;

namespace Scenematic.Data.Postgres.Data;

public class ScenematicNpgsqlDataContext(DbContextOptions<ScenematicNpgsqlDataContext> options) : DbContext(options)
{
    public DbSet<CoverJob> GenerateCoverJobs { get; set; } = null!;
    public DbSet<CoverJobSuccess> GenerateCoverJobSuccessResults { get; set; } = null!;
    public DbSet<CoverJobFailure> GenerateCoverJobFailureResults { get; set; } = null!;

    public DbSet<CoverMeta> CoverMetas { get; set; } = null!;
    public DbSet<CoverVoteMeta> CoverVoteMetas { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CoverJob>()
            .HasOne(j => j.CoverMeta)
            .WithOne()
            .HasForeignKey<CoverJob>(j => j.CoverMetaId);

        modelBuilder.Entity<CoverVoteMeta>()
            .HasOne(j => j.CoverMeta)
            .WithMany(cm => cm.CoverVotes)
            .HasForeignKey(j => j.CoverMetaId);

        modelBuilder.Entity<CoverJobSuccess>()
            .HasKey(sa => sa.JobId);

        modelBuilder.Entity<CoverJobSuccess>()
            .Property(sa => sa.JobId)
            .ValueGeneratedNever();

        modelBuilder.Entity<CoverJobFailure>()
            .HasKey(fa => fa.JobId);

        modelBuilder.Entity<CoverJobFailure>()
            .Property(fa => fa.JobId)
            .ValueGeneratedNever();

        modelBuilder.Entity<CoverJob>()
            .HasOne(j => j.SuccessResult)
            .WithOne()
            .HasForeignKey<CoverJobSuccess>(sa => sa.JobId);

        modelBuilder.Entity<CoverJob>()
            .HasOne(j => j.FailureResult)
            .WithOne()
            .HasForeignKey<CoverJobFailure>(fa => fa.JobId);
    }
}
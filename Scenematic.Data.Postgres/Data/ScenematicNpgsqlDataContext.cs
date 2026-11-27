using Microsoft.EntityFrameworkCore;
using Scenematic.Data.Models.Entities.Cover;
using Scenematic.Data.Models.Entities.Webhooks;

namespace Scenematic.Data.Postgres.Data;

public class ScenematicNpgsqlDataContext(DbContextOptions<ScenematicNpgsqlDataContext> options) : DbContext(options)
{
    public DbSet<CoverJob> CoverJobs { get; set; } = null!;
    public DbSet<CoverJobSuccess> CoverJobSuccessResults { get; set; } = null!;
    public DbSet<CoverJobFailure> CoverJobFailureResults { get; set; } = null!;

    public DbSet<CoverMeta> CoverMetas { get; set; } = null!;
    public DbSet<CoverVoteMeta> CoverVoteMetas { get; set; } = null!;

    public DbSet<Webhook> Webhooks { get; set; } = null!;
    public DbSet<CoverJobWebhook> CoverJobWebhooks { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CoverJob>()
            .HasOne(j => j.CoverMeta)
            .WithOne()
            .HasForeignKey<CoverJob>(j => j.CoverMetaId);

        // modelBuilder.Entity<CoverVoteMeta>()
            // .HasOne(j => j.CoverMeta)
            // .WithMany(cm => cm.CoverVotes)
            // .HasForeignKey(j => j.CoverMetaId);

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

        modelBuilder.Entity<CoverJob>()
            .HasMany(j => j.Webhooks)
            .WithOne(w => w.Job)
            .HasForeignKey(w => w.JobId);

        modelBuilder.Entity<CoverJobWebhook>()
            .HasKey(wj => new { wj.JobId, wj.WebhookId });

        modelBuilder.Entity<CoverJobWebhook>()
            .HasOne(wj => wj.Job)
            .WithMany(j => j.Webhooks)
            .HasForeignKey(wj => wj.JobId);

        modelBuilder.Entity<CoverJobWebhook>()
            .HasOne(wj => wj.Webhook)
            .WithMany()
            .HasForeignKey(wj => wj.WebhookId);
    }
}
using Microsoft.EntityFrameworkCore;

namespace Closeoutflow.Api.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<JobRow> Jobs => Set<JobRow>();
    public DbSet<CloseoutRecordRow> CloseoutRecords => Set<CloseoutRecordRow>();
    public DbSet<ProofItemRow> ProofItems => Set<ProofItemRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobRow>(builder =>
        {
            builder.ToTable("jobs");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(x => x.Status)
                .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();
        });

        modelBuilder.Entity<CloseoutRecordRow>(builder =>
        {
            builder.ToTable("closeout_records");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Summary)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.HasIndex(x => x.JobId)
                .IsUnique()
                .HasDatabaseName("UX_closeout_records_JobId");

            builder.HasMany(x => x.ProofItems)
                .WithOne(x => x.CloseoutRecord)
                .HasForeignKey(x => x.CloseoutRecordId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProofItemRow>(builder =>
        {
            builder.ToTable("proof_items");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Type)
                .IsRequired();

            builder.Property(x => x.Value)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();
        });
    }
}

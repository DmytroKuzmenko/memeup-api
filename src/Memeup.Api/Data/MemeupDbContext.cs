using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Memeup.Api.Domain.Auth;
using Memeup.Api.Domain.Sections;
using Memeup.Api.Domain.Levels;
using Memeup.Api.Domain.Tasks;
using Memeup.Api.Domain.Game;

namespace Memeup.Api.Data;

public class MemeupDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public MemeupDbContext(DbContextOptions<MemeupDbContext> options) : base(options) { }

    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Level> Levels => Set<Level>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<UserTaskProgress> UserTaskProgress => Set<UserTaskProgress>();
    public DbSet<TaskAttemptLog> TaskAttemptLogs => Set<TaskAttemptLog>();
    public DbSet<UserLevelProgress> UserLevelProgress => Set<UserLevelProgress>();
    public DbSet<UserSectionProgress> UserSectionProgress => Set<UserSectionProgress>();
    public DbSet<LeaderboardEntry> LeaderboardEntries => Set<LeaderboardEntry>();
    public DbSet<ActiveTaskAttempt> ActiveTaskAttempts => Set<ActiveTaskAttempt>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder b)
{
    base.OnModelCreating(b);

    var taskEntity = b.Entity<TaskItem>();

    taskEntity.Property(t => t.ImageUrl)
        .HasMaxLength(1024);

    taskEntity.Property(t => t.ResultImagePath)
        .HasMaxLength(1024);

    taskEntity.Property(t => t.ResultImageSource)
        .HasMaxLength(1024);

    taskEntity.Property(t => t.TaskImageSource)
        .HasMaxLength(1024);

    taskEntity.OwnsMany(t => t.Options, owned =>
    {
        owned.ToTable("TaskOptions");

        // FK к владельцу
        owned.WithOwner()
             .HasForeignKey("TaskItemId");

        // Ключ зависимой
        owned.HasKey(o => o.Id);

        // Генерация Id на клиенте допустима (мы задаём Guid.NewGuid() в коде)
        // ValueGeneratedOnAdd оставляем — EF не будет конфликтовать.
        owned.Property(o => o.Id)
             .ValueGeneratedOnAdd();

        owned.Property(o => o.Label)
             .HasMaxLength(1024)
             .IsRequired();

        owned.Property(o => o.IsCorrect);

        owned.Property(o => o.ImageUrl)
             .HasMaxLength(1024);

        owned.Property(o => o.CorrectAnswer)
             .HasMaxLength(1024);
    });

    taskEntity.Navigation(t => t.Options).AutoInclude();

    b.Entity<UserTaskProgress>(entity =>
    {
        entity.HasIndex(x => new { x.UserId, x.TaskId }).IsUnique();
        entity.Property(x => x.PointsEarned).HasDefaultValue(0);
        entity.Property(x => x.AttemptsUsed).HasDefaultValue(0);
    });

    b.Entity<TaskAttemptLog>(entity =>
    {
        entity.HasIndex(x => new { x.UserId, x.TaskId });
        entity.Property(x => x.ClientAgent).HasMaxLength(1024);
        entity.Property(x => x.ClientTz).HasMaxLength(128);
        entity.Property(x => x.IpHash).HasMaxLength(256);
    });

    b.Entity<UserLevelProgress>(entity =>
    {
        entity.HasIndex(x => new { x.UserId, x.LevelId }).IsUnique();
        entity.Property(x => x.Status).HasMaxLength(32);
    });

    b.Entity<UserSectionProgress>(entity =>
    {
        entity.HasIndex(x => new { x.UserId, x.SectionId }).IsUnique();
    });

    b.Entity<LeaderboardEntry>(entity =>
    {
        entity.HasIndex(x => new { x.UserId, x.Period }).IsUnique();
        entity.Property(x => x.Period).HasMaxLength(32);
    });

    b.Entity<ActiveTaskAttempt>(entity =>
    {
        entity.HasIndex(x => x.Token).IsUnique();
        entity.HasIndex(x => new { x.UserId, x.TaskId, x.IsFinalized });
    });

    b.Entity<RefreshToken>(entity =>
    {
        entity.HasIndex(x => x.TokenHash).IsUnique();
        entity.Property(x => x.TokenHash).HasMaxLength(256).IsRequired();
        entity.Property(x => x.UsageCount).HasDefaultValue(0);
        entity.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    });
}

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        TouchTimestamps();
        return base.SaveChangesAsync(ct);
    }

    public override int SaveChanges()
    {
        TouchTimestamps();
        return base.SaveChanges();
    }

    private void TouchTimestamps()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
                continue;

            // Есть ли у сущности поле UpdatedAt?
            var hasUpdatedAt = entry.Metadata.FindProperty("UpdatedAt") is not null;
            if (hasUpdatedAt)
            {
                entry.CurrentValues["UpdatedAt"] = now;
            }

            // Есть ли у сущности поле CreatedAt? (только для Added)
            if (entry.State == EntityState.Added)
            {
                var hasCreatedAt = entry.Metadata.FindProperty("CreatedAt") is not null;
                if (hasCreatedAt)
                {
                    entry.CurrentValues["CreatedAt"] = now;
                }
            }

            var hasRowVersion = entry.Metadata.FindProperty("RowVersion") is not null;
            if (hasRowVersion)
            {
                entry.CurrentValues["RowVersion"] = Guid.NewGuid().ToByteArray();
            }
        }
    }
}

using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Memeup.Api.Domain.Auth;
using Memeup.Api.Domain.Sections;
using Memeup.Api.Domain.Levels;
using Memeup.Api.Domain.Tasks;

namespace Memeup.Api.Data;

public class MemeupDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public MemeupDbContext(DbContextOptions<MemeupDbContext> options) : base(options) { }

    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Level> Levels => Set<Level>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder b)
{
    base.OnModelCreating(b);

    var taskEntity = b.Entity<TaskItem>();

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
    });

    taskEntity.Navigation(t => t.Options).AutoInclude();
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

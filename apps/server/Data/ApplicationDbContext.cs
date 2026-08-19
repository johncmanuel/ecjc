using Microsoft.EntityFrameworkCore;
using server.Data.Models;

namespace server.Data;

public class ApplicationDbContext : DbContext
{
    private readonly TimeProvider _timeProvider;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, TimeProvider? timeProvider = null) : base(options)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }
    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupUser> GroupUsers => Set<GroupUser>();
    public DbSet<Entry> Entries => Set<Entry>();
    public DbSet<MediaAttachment> MediaAttachments => Set<MediaAttachment>();
    public DbSet<Reaction> Reactions => Set<Reaction>();
    public DbSet<GroupInvite> GroupInvites => Set<GroupInvite>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).IsRequired().HasMaxLength(320);
            e.Property(u => u.FirstName).HasMaxLength(128);
            e.Property(u => u.LastName).HasMaxLength(128);
            e.Property(u => u.FriendCode).IsRequired().HasMaxLength(32);
            e.HasIndex(u => u.FriendCode).IsUnique();
            e.Property(u => u.Image).HasMaxLength(2048);
        });

        modelBuilder.Entity<Group>(e =>
        {
            e.HasKey(g => g.Id);
            e.Property(g => g.Id).HasDefaultValueSql("gen_random_uuid()");
        });

        // GroupUser, the join table between User and Group
        modelBuilder.Entity<GroupUser>(e =>
        {
            e.HasKey(gu => new { gu.UserId, gu.GroupId });
            e.HasOne(gu => gu.User)
                .WithMany(u => u.GroupUsers)
                .HasForeignKey(gu => gu.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(gu => gu.Group)
                .WithMany(g => g.GroupUsers)
                .HasForeignKey(gu => gu.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Entry>(e =>
        {
            e.HasKey(en => en.Id);
            e.Property(en => en.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(en => en.TextContent).IsRequired();
            e.HasOne(en => en.Author)
                .WithMany(u => u.Entries)
                .HasForeignKey(en => en.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(en => en.Group)
                .WithMany(g => g.Entries)
                .HasForeignKey(en => en.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MediaAttachment>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(m => m.FilePath).IsRequired().HasMaxLength(2048);
            e.HasOne(m => m.Entry)
                .WithMany(en => en.MediaAttachments)
                .HasForeignKey(m => m.EntryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Reaction>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(r => r.EmojiCode).IsRequired().HasMaxLength(64);
            e.HasOne(r => r.Entry)
                .WithMany(en => en.Reactions)
                .HasForeignKey(r => r.EntryId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.User)
                .WithMany(u => u.Reactions)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            // Prevent dupe reactions based on the variables like same user, same emoji, same entry
            e.HasIndex(r => new { r.EntryId, r.UserId, r.EmojiCode }).IsUnique();
        });

        modelBuilder.Entity<GroupInvite>(e =>
        {
            e.HasKey(gi => gi.Id);
            e.Property(gi => gi.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(gi => gi.Status)
                .HasConversion<string>()
                .HasMaxLength(32);
            e.HasOne(gi => gi.Group)
                .WithMany()
                .HasForeignKey(gi => gi.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(gi => gi.Inviter)
                .WithMany()
                .HasForeignKey(gi => gi.InviterId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(gi => gi.Invitee)
                .WithMany()
                .HasForeignKey(gi => gi.InviteeId)
                .OnDelete(DeleteBehavior.Restrict);
            // Prevent duplicate pending invites from the same inviter to the same invitee in the same group
            e.HasIndex(gi => new { gi.GroupId, gi.InviterId, gi.InviteeId })
                .HasFilter("\"Status\" = 'Pending'")
                .IsUnique();
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is User u) { u.CreatedAt = now; u.UpdatedAt = now; }
                else if (entry.Entity is Group g) { g.CreatedAt = now; g.UpdatedAt = now; }
                else if (entry.Entity is Entry en) { en.CreatedAt = now; en.UpdatedAt = now; }
                else if (entry.Entity is GroupUser gu) { gu.JoinedAt = now; }
                else if (entry.Entity is MediaAttachment m) { m.UploadedAt = now; }
                else if (entry.Entity is Reaction r) { r.CreatedAt = now; }
                else if (entry.Entity is GroupInvite gi) { gi.CreatedAt = now; gi.UpdatedAt = now; }
            }
            else if (entry.State == EntityState.Modified)
            {
                if (entry.Entity is User u) { u.UpdatedAt = now; }
                else if (entry.Entity is Group g) { g.UpdatedAt = now; }
                else if (entry.Entity is Entry en) { en.UpdatedAt = now; }
                else if (entry.Entity is GroupInvite gi) { gi.UpdatedAt = now; }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}

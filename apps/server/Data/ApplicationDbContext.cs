using Microsoft.EntityFrameworkCore;
using server.Data.Models;

namespace server.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, TimeProvider? timeProvider = null) : DbContext(options)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupUser> GroupUsers => Set<GroupUser>();
    public DbSet<Entry> Entries => Set<Entry>();
    public DbSet<MediaAttachment> MediaAttachments => Set<MediaAttachment>();
    public DbSet<Reaction> Reactions => Set<Reaction>();
    public DbSet<GroupInvite> GroupInvites => Set<GroupInvite>();

    // Better Auth auxiliary tables
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<server.Data.Models.Account> Accounts => Set<server.Data.Models.Account>();
    public DbSet<Verification> Verifications => Set<Verification>();
    public DbSet<Jwks> Jwks => Set<Jwks>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // https://better-auth.com/docs/concepts/database#core-schema
        modelBuilder.Entity<User>(e =>
        {
            // Map to lowercase "user" table for better-auth compatibility
            e.ToTable("user");

            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasColumnName("id");
            e.Property(u => u.Email).HasColumnName("email").IsRequired().HasMaxLength(320);
            e.Property(u => u.Name).HasColumnName("name");
            e.Property(u => u.EmailVerified).HasColumnName("emailVerified");
            e.Property(u => u.FirstName).HasColumnName("firstName").HasMaxLength(128);
            e.Property(u => u.LastName).HasColumnName("lastName").HasMaxLength(128);
            e.Property(u => u.FriendCode).HasColumnName("friendCode").IsRequired().HasMaxLength(32);
            e.HasIndex(u => u.FriendCode).IsUnique();
            e.Property(u => u.Image).HasColumnName("image").HasMaxLength(2048);
            e.Property(u => u.CreatedAt).HasColumnName("createdAt");
            e.Property(u => u.UpdatedAt).HasColumnName("updatedAt");
            e.Property(u => u.StripeCustomerId).HasColumnName("stripeCustomerId");
            e.Property(u => u.IsPenaltyEnabled).HasColumnName("isPenaltyEnabled");
            e.Property(u => u.PenaltyAmount).HasColumnName("penaltyAmount");
        });

        // auxiliary table mappings
        modelBuilder.Entity<Session>(e =>
        {
            e.ToTable("session");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("id");
            e.Property(s => s.ExpiresAt).HasColumnName("expiresAt");
            e.Property(s => s.Token).HasColumnName("token");
            e.Property(s => s.CreatedAt).HasColumnName("createdAt");
            e.Property(s => s.UpdatedAt).HasColumnName("updatedAt");
            e.Property(s => s.IpAddress).HasColumnName("ipAddress");
            e.Property(s => s.UserAgent).HasColumnName("userAgent");
            e.Property(s => s.UserId).HasColumnName("userId");
            e.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Account>(e =>
        {
            e.ToTable("account");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasColumnName("id");
            e.Property(a => a.AccountId).HasColumnName("accountId");
            e.Property(a => a.ProviderId).HasColumnName("providerId");
            e.Property(a => a.UserId).HasColumnName("userId");
            e.Property(a => a.AccessToken).HasColumnName("accessToken");
            e.Property(a => a.RefreshToken).HasColumnName("refreshToken");
            e.Property(a => a.IdToken).HasColumnName("idToken");
            e.Property(a => a.AccessTokenExpiresAt).HasColumnName("accessTokenExpiresAt");
            e.Property(a => a.RefreshTokenExpiresAt).HasColumnName("refreshTokenExpiresAt");
            e.Property(a => a.Scope).HasColumnName("scope");
            e.Property(a => a.Password).HasColumnName("password");
            e.Property(a => a.CreatedAt).HasColumnName("createdAt");
            e.Property(a => a.UpdatedAt).HasColumnName("updatedAt");
            e.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Verification>(e =>
        {
            e.ToTable("verification");
            e.HasKey(v => v.Id);
            e.Property(v => v.Id).HasColumnName("id");
            e.Property(v => v.Identifier).HasColumnName("identifier");
            e.Property(v => v.Value).HasColumnName("value");
            e.Property(v => v.ExpiresAt).HasColumnName("expiresAt");
            e.Property(v => v.CreatedAt).HasColumnName("createdAt");
            e.Property(v => v.UpdatedAt).HasColumnName("updatedAt");
        });

        modelBuilder.Entity<Jwks>(e =>
        {
            e.ToTable("jwks");
            e.HasKey(j => j.Id);
            e.Property(j => j.Id).HasColumnName("id");
            e.Property(j => j.PublicKey).HasColumnName("publicKey");
            e.Property(j => j.PrivateKey).HasColumnName("privateKey");
            e.Property(j => j.CreatedAt).HasColumnName("createdAt");
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

        modelBuilder.Entity<ApiKey>(e =>
        {
            e.ToTable("apiKeys");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasColumnName("id");
            e.Property(a => a.UserId).HasColumnName("userId");
            e.Property(a => a.Name).HasColumnName("name");
            e.Property(a => a.KeyHash).HasColumnName("keyHash");
            e.Property(a => a.Prefix).HasColumnName("prefix");
            e.Property(a => a.CreatedAt).HasColumnName("createdAt");
            
            e.HasOne(a => a.User).WithMany(u => u.ApiKeys).HasForeignKey(a => a.UserId);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                // set CreatedAt and UpdatedAt for new entities
                // this fixes issue where CreatedAt and UpdatedAt are unconditionally being overrided
                // this was needed since during testing for streaks and payments where the timestamps are overwritten by current system time
                // which can break testing
                if (entry.Entity is User u) { if (u.CreatedAt == default) u.CreatedAt = now; u.UpdatedAt = now; }
                else if (entry.Entity is Group g) { if (g.CreatedAt == default) g.CreatedAt = now; g.UpdatedAt = now; }
                else if (entry.Entity is Entry en) { if (en.CreatedAt == default) en.CreatedAt = now; en.UpdatedAt = now; }
                else if (entry.Entity is GroupUser gu) { if (gu.JoinedAt == default) gu.JoinedAt = now; }
                else if (entry.Entity is MediaAttachment m) { if (m.UploadedAt == default) m.UploadedAt = now; }
                else if (entry.Entity is Reaction r) { if (r.CreatedAt == default) r.CreatedAt = now; }
                else if (entry.Entity is GroupInvite gi) { if (gi.CreatedAt == default) gi.CreatedAt = now; gi.UpdatedAt = now; }
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

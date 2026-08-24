namespace server.Data.Models;

public class User
{
    // id, email, name, and image are provided by the authentication provider (e.g., Google, GitHub, etc.)
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    // Note: Google OAuth inherently verifies emails, but better-auth strictly requires
    // the emailVerified column in its core schema, so it must be tracked by EF Core.
    public bool EmailVerified { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string FriendCode { get; set; } = string.Empty;
    public string? Image { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public string? StripeCustomerId { get; set; }
    public bool IsPenaltyEnabled { get; set; } = false;
    public int PenaltyAmount { get; set; } = 500; // in cents, default $5.00
    
    public string? VenmoHandle { get; set; }
    public string? CashAppHandle { get; set; }
    public string? PayPalHandle { get; set; }

    // Navigation properties
    public ICollection<GroupUser> GroupUsers { get; set; } = [];
    public ICollection<Entry> Entries { get; set; } = [];
    public ICollection<Reaction> Reactions { get; set; } = [];
    public ICollection<ApiKey> ApiKeys { get; set; } = [];
}

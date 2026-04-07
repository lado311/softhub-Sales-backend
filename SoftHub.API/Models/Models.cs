namespace SoftHub.API.Models;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Manager"; // Admin | Manager
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Lead> AssignedLeads { get; set; } = new List<Lead>();
    public ICollection<Note> Notes { get; set; } = new List<Note>();
}

public class Lead
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = "";
    public string Industry { get; set; } = "";
    public string ContactPersonName { get; set; } = "";
    public string ContactPersonPosition { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Location { get; set; } = "";
    public string CompanySize { get; set; } = "Small"; // Small | Medium
    public string Source { get; set; } = "";
    public string Status { get; set; } = "New";
    public string InterestLevel { get; set; } = "Medium"; // Low | Medium | High
    public decimal PotentialValue { get; set; }
    public string? NextFollowUpDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastContactedAt { get; set; }

    // FK
    public int? AssignedToId { get; set; }
    public User? AssignedTo { get; set; }

    public ICollection<Note> Notes { get; set; } = new List<Note>();
    public ICollection<ActivityLog> Activities { get; set; } = new List<ActivityLog>();
}

public class Note
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int LeadId { get; set; }
    public Lead Lead { get; set; } = null!;

    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;
}

public class ActivityLog
{
    public int Id { get; set; }
    public string Action { get; set; } = ""; // created | status_changed | note_added | assigned | updated
    public string Description { get; set; } = "";
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int LeadId { get; set; }
    public Lead Lead { get; set; } = null!;

    public int? UserId { get; set; }
    public User? User { get; set; }
}

public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

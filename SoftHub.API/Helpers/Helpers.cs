using SoftHub.API.DTOs;
using SoftHub.API.Models;

namespace SoftHub.API.Helpers;

public static class LeadScoreHelper
{
    public static int ComputeScore(Lead lead)
    {
        int score = 0;

        // Interest level (0–30)
        score += lead.InterestLevel switch
        {
            "High" => 30,
            "Medium" => 18,
            "Low" => 6,
            _ => 0
        };

        // Potential value (0–25)
        score += lead.PotentialValue switch
        {
            >= 50000 => 25,
            >= 30000 => 20,
            >= 15000 => 14,
            >= 5000 => 8,
            _ => 3
        };

        // Pipeline stage (0–20)
        score += lead.Status switch
        {
            "New" => 4,
            "Contacted" => 8,
            "Demo Scheduled" => 12,
            "Proposal Sent" => 16,
            "Negotiation" => 20,
            "Won" => 20,
            _ => 0
        };

        // Source quality (0–10)
        score += lead.Source switch
        {
            "Referral" => 10,
            "Event" => 8,
            "Website" => 6,
            "LinkedIn" => 6,
            "Facebook" => 4,
            "Cold Call" => 3,
            _ => 2
        };

        // Recency of contact (0–10)
        if (lead.LastContactedAt.HasValue)
        {
            var daysSince = (DateTime.UtcNow - lead.LastContactedAt.Value).TotalDays;
            score += daysSince switch
            {
                <= 3 => 10,
                <= 7 => 7,
                <= 14 => 4,
                <= 30 => 2,
                _ => 0
            };
        }

        // Company size (0–5)
        score += lead.CompanySize == "Medium" ? 5 : 2;

        return Math.Min(100, score);
    }
}

public static class MappingHelper
{
    public static UserDto ToDto(User u) => new(
        u.Id, u.FullName, u.Email, u.Role, u.IsActive, u.CreatedAt);

    public static NoteDto ToDto(Note n) => new(
        n.Id, n.Text, n.CreatedAt, n.Author?.FullName ?? "", n.AuthorId);

    public static ActivityDto ToDto(ActivityLog a) => new(
        a.Id, a.Action, a.Description, a.OldValue, a.NewValue,
        a.CreatedAt, a.User?.FullName);

    public static LeadListItemDto ToListDto(Lead l) => new(
    l.Id, l.CompanyName, l.Industry,
    l.ContactPersonName, l.ContactPersonPosition,
    l.Email, l.Phone, l.Location, l.CompanySize, l.Source,
    l.Status, l.InterestLevel, l.PotentialValue,
    l.NextFollowUpDate, l.CreatedAt, l.LastContactedAt,
    l.AssignedTo?.FullName, l.AssignedToId,
    LeadScoreHelper.ComputeScore(l));

    public static LeadDto ToDetailDto(Lead l) => new(
        l.Id, l.CompanyName, l.Industry,
        l.ContactPersonName, l.ContactPersonPosition,
        l.Phone, l.Email, l.Location,
        l.CompanySize, l.Source, l.Status, l.InterestLevel,
        l.PotentialValue, l.NextFollowUpDate,
        l.CreatedAt, l.LastContactedAt,
        l.AssignedTo != null ? ToDto(l.AssignedTo) : null,
        l.Notes.OrderBy(n => n.CreatedAt).Select(ToDto).ToList(),
        l.Activities.OrderByDescending(a => a.CreatedAt).Select(ToDto).ToList(),
        LeadScoreHelper.ComputeScore(l));
}

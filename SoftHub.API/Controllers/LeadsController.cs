using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftHub.API.Data;
using SoftHub.API.DTOs;
using SoftHub.API.Helpers;
using SoftHub.API.Models;

namespace SoftHub.API.Controllers;

[ApiController]
[Route("api/leads")]
[Authorize]
public class LeadsController : ControllerBase
{
    private readonly AppDbContext _db;

    public LeadsController(AppDbContext db) => _db = db;

    private int CurrentUserId =>
        int.Parse(User.FindFirst("userId")!.Value);

    // ─── GET /api/leads ──────────────────────────────────
    [HttpGet]
    public async Task<ActionResult<PagedResult<LeadListItemDto>>> GetLeads(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? industry,
        [FromQuery] int? assignedToId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string sortBy = "CreatedAt",
        [FromQuery] string sortDir = "desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _db.Leads
            .Include(l => l.AssignedTo)
            .AsQueryable();

        // Filters
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(l =>
                l.CompanyName.ToLower().Contains(s) ||
                l.ContactPersonName.ToLower().Contains(s) ||
                l.Email.ToLower().Contains(s) ||
                l.Phone.Contains(s));
        }
        if (!string.IsNullOrWhiteSpace(status) && status != "All")
            query = query.Where(l => l.Status == status);
        if (!string.IsNullOrWhiteSpace(industry) && industry != "All")
            query = query.Where(l => l.Industry == industry);
        if (assignedToId.HasValue)
            query = query.Where(l => l.AssignedToId == assignedToId);
        if (dateFrom.HasValue)
            query = query.Where(l => l.CreatedAt >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(l => l.CreatedAt <= dateTo.Value);

        // Sort
        query = (sortBy.ToLower(), sortDir.ToLower()) switch
        {
            ("companyname", "asc") => query.OrderBy(l => l.CompanyName),
            ("companyname", _) => query.OrderByDescending(l => l.CompanyName),
            ("potentialvalue", "asc") => query.OrderBy(l => l.PotentialValue),
            ("potentialvalue", _) => query.OrderByDescending(l => l.PotentialValue),
            ("status", "asc") => query.OrderBy(l => l.Status),
            ("status", _) => query.OrderByDescending(l => l.Status),
            ("lastcontactedat", "asc") => query.OrderBy(l => l.LastContactedAt),
            ("lastcontactedat", _) => query.OrderByDescending(l => l.LastContactedAt),
            ("nextfollowupdate", "asc") => query.OrderBy(l => l.NextFollowUpDate),
            ("nextfollowupdate", _) => query.OrderByDescending(l => l.NextFollowUpDate),
            (_, "asc") => query.OrderBy(l => l.CreatedAt),
            _ => query.OrderByDescending(l => l.CreatedAt)
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => MappingHelper.ToListDto(l))
            .ToListAsync();

        return Ok(new PagedResult<LeadListItemDto>(
            items, totalCount, page, pageSize,
            (int)Math.Ceiling(totalCount / (double)pageSize)));
    }

    // ─── GET /api/leads/{id} ─────────────────────────────
    [HttpGet("{id}")]
    public async Task<ActionResult<LeadDto>> GetLead(int id)
    {
        var lead = await _db.Leads
            .Include(l => l.AssignedTo)
            .Include(l => l.Notes).ThenInclude(n => n.Author)
            .Include(l => l.Activities).ThenInclude(a => a.User)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lead == null) return NotFound();
        return Ok(MappingHelper.ToDetailDto(lead));
    }

    // ─── POST /api/leads ─────────────────────────────────
    [HttpPost]
    public async Task<ActionResult<LeadDto>> CreateLead([FromBody] CreateLeadRequest req)
    {
        var lead = new Lead
        {
            CompanyName = req.CompanyName,
            Industry = req.Industry,
            ContactPersonName = req.ContactPersonName,
            ContactPersonPosition = req.ContactPersonPosition,
            Phone = req.Phone,
            Email = req.Email,
            Location = req.Location,
            CompanySize = req.CompanySize,
            Source = req.Source,
            Status = req.Status,
            InterestLevel = req.InterestLevel,
            PotentialValue = req.PotentialValue,
            AssignedToId = req.AssignedToId,
            NextFollowUpDate = req.NextFollowUpDate,
        };

        _db.Leads.Add(lead);
        await _db.SaveChangesAsync();

        // Log activity
        await LogActivity(lead.Id, "created", $"Lead created: {lead.CompanyName}");

        // Initial note
        if (!string.IsNullOrWhiteSpace(req.InitialNote))
        {
            _db.Notes.Add(new Note
            {
                LeadId = lead.Id,
                AuthorId = CurrentUserId,
                Text = req.InitialNote
            });
            await _db.SaveChangesAsync();
        }

        return await GetLead(lead.Id);
    }

    // ─── PUT /api/leads/{id} ─────────────────────────────
    [HttpPut("{id}")]
    public async Task<ActionResult<LeadDto>> UpdateLead(int id, [FromBody] UpdateLeadRequest req)
    {
        var lead = await _db.Leads.FindAsync(id);
        if (lead == null) return NotFound();

        var oldStatus = lead.Status;

        if (req.CompanyName != null) lead.CompanyName = req.CompanyName;
        if (req.Industry != null) lead.Industry = req.Industry;
        if (req.ContactPersonName != null) lead.ContactPersonName = req.ContactPersonName;
        if (req.ContactPersonPosition != null) lead.ContactPersonPosition = req.ContactPersonPosition;
        if (req.Phone != null) lead.Phone = req.Phone;
        if (req.Email != null) lead.Email = req.Email;
        if (req.Location != null) lead.Location = req.Location;
        if (req.CompanySize != null) lead.CompanySize = req.CompanySize;
        if (req.Source != null) lead.Source = req.Source;
        if (req.InterestLevel != null) lead.InterestLevel = req.InterestLevel;
        if (req.PotentialValue.HasValue) lead.PotentialValue = req.PotentialValue.Value;
        if (req.AssignedToId.HasValue) lead.AssignedToId = req.AssignedToId;
        if (req.NextFollowUpDate != null) lead.NextFollowUpDate = req.NextFollowUpDate;

        if (req.Status != null && req.Status != oldStatus)
        {
            lead.Status = req.Status;
            lead.LastContactedAt = DateTime.UtcNow;
            await LogActivity(id, "status_changed",
                $"Status changed: {oldStatus} → {req.Status}",
                oldStatus, req.Status);
        }
        else
        {
            await LogActivity(id, "updated", "Lead details updated");
        }

        await _db.SaveChangesAsync();
        return await GetLead(id);
    }

    // ─── PATCH /api/leads/{id}/move ──────────────────────
    [HttpPatch("{id}/move")]
    public async Task<ActionResult<LeadDto>> MoveLead(int id, [FromBody] MoveLeadRequest req)
    {
        var lead = await _db.Leads.FindAsync(id);
        if (lead == null) return NotFound();

        var oldStatus = lead.Status;
        lead.Status = req.Status;
        lead.LastContactedAt = DateTime.UtcNow;

        await LogActivity(id, "status_changed",
            $"Moved: {oldStatus} → {req.Status}", oldStatus, req.Status);
        await _db.SaveChangesAsync();

        return await GetLead(id);
    }

    // ─── DELETE /api/leads/{id} ──────────────────────────
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLead(int id)
    {
        var lead = await _db.Leads.FindAsync(id);
        if (lead == null) return NotFound();
        _db.Leads.Remove(lead);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ─── POST /api/leads/bulk ────────────────────────────
    [HttpPost("bulk")]
    public async Task<IActionResult> BulkUpdate([FromBody] BulkUpdateRequest req)
    {
        var leads = await _db.Leads
            .Where(l => req.LeadIds.Contains(l.Id))
            .ToListAsync();

        foreach (var lead in leads)
        {
            if (req.Status != null)
            {
                var old = lead.Status;
                lead.Status = req.Status;
                lead.LastContactedAt = DateTime.UtcNow;
                await LogActivity(lead.Id, "status_changed",
                    $"Bulk status change: {old} → {req.Status}", old, req.Status);
            }
            if (req.AssignedToId.HasValue)
            {
                lead.AssignedToId = req.AssignedToId;
                await LogActivity(lead.Id, "assigned",
                    $"Bulk reassigned to user #{req.AssignedToId}");
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { updated = leads.Count });
    }

    // ─── POST /api/leads/{id}/notes ──────────────────────
    [HttpPost("{id}/notes")]
    public async Task<ActionResult<NoteDto>> AddNote(int id, [FromBody] CreateNoteRequest req)
    {
        var lead = await _db.Leads.FindAsync(id);
        if (lead == null) return NotFound();

        var note = new Note
        {
            LeadId = id,
            AuthorId = CurrentUserId,
            Text = req.Text
        };
        _db.Notes.Add(note);
        lead.LastContactedAt = DateTime.UtcNow;

        await LogActivity(id, "note_added", "Note added");
        await _db.SaveChangesAsync();

        await _db.Entry(note).Reference(n => n.Author).LoadAsync();
        return CreatedAtAction(nameof(GetLead), new { id }, MappingHelper.ToDto(note));
    }

    // ─── DELETE /api/leads/{leadId}/notes/{noteId} ───────
    [HttpDelete("{leadId}/notes/{noteId}")]
    public async Task<IActionResult> DeleteNote(int leadId, int noteId)
    {
        var note = await _db.Notes
            .FirstOrDefaultAsync(n => n.Id == noteId && n.LeadId == leadId);
        if (note == null) return NotFound();

        // Only author or admin can delete
        if (note.AuthorId != CurrentUserId &&
            !User.IsInRole("Admin"))
            return Forbid();

        _db.Notes.Remove(note);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ─── Helpers ─────────────────────────────────────────
    private async Task LogActivity(
        int leadId, string action, string description,
        string? oldValue = null, string? newValue = null)
    {
        _db.ActivityLogs.Add(new ActivityLog
        {
            LeadId = leadId,
            UserId = CurrentUserId,
            Action = action,
            Description = description,
            OldValue = oldValue,
            NewValue = newValue
        });
        // SaveChanges called by caller
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftHub.API.Data;
using SoftHub.API.DTOs;
using SoftHub.API.Helpers;
using SoftHub.API.Models;

namespace SoftHub.API.Controllers;

// ═══════════════════════════════════════════════════════
//  Users Controller  (Admin only)
// ═══════════════════════════════════════════════════════
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    public UsersController(AppDbContext db) => _db = db;

    // GET /api/users
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetUsers()
    {
        var users = await _db.Users
            .OrderBy(u => u.FullName)
            .Select(u => MappingHelper.ToDto(u))
            .ToListAsync();
        return Ok(users);
    }

    // GET /api/users/{id}
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await _db.Users.FindAsync(id);
        return user == null ? NotFound() : Ok(MappingHelper.ToDto(user));
    }

    // PUT /api/users/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> UpdateUser(int id, [FromBody] UpdateUserRequest req)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        if (req.FullName != null) user.FullName = req.FullName;
        if (req.Email != null) user.Email = req.Email;
        if (req.Role != null) user.Role = req.Role;
        if (req.IsActive.HasValue) user.IsActive = req.IsActive.Value;
        if (req.Password != null)
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);

        await _db.SaveChangesAsync();
        return Ok(MappingHelper.ToDto(user));
    }

    // DELETE /api/users/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var userId = int.Parse(User.FindFirst("userId")!.Value);
        if (id == userId)
            return BadRequest(new { message = "Cannot delete your own account." });

        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        // Unassign their leads instead of deleting
        await _db.Leads
            .Where(l => l.AssignedToId == id)
            .ExecuteUpdateAsync(l => l.SetProperty(x => x.AssignedToId, (int?)null));

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // GET /api/users/{id}/leads  — leads assigned to a manager
    [HttpGet("{id}/leads")]
    public async Task<ActionResult<List<LeadListItemDto>>> GetUserLeads(int id)
    {
        var leads = await _db.Leads
            .Include(l => l.AssignedTo)
            .Where(l => l.AssignedToId == id)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => MappingHelper.ToListDto(l))
            .ToListAsync();
        return Ok(leads);
    }
}

// ═══════════════════════════════════════════════════════
//  Dashboard Controller
// ═══════════════════════════════════════════════════════
[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;
    public DashboardController(AppDbContext db) => _db = db;

    // GET /api/dashboard/stats
    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats()
    {
        var leads = await _db.Leads
            .Include(l => l.AssignedTo)
            .ToListAsync();

        var won = leads.Where(l => l.Status == "Won").ToList();
        var lost = leads.Where(l => l.Status == "Lost").ToList();
        var wonRevenue = won.Sum(l => l.PotentialValue);
        var totalClosed = won.Count + lost.Count;
        var conversionRate = totalClosed > 0
            ? (int)Math.Round((double)won.Count / totalClosed * 100)
            : 0;

        var pipelineValue = leads
            .Where(l => l.Status != "Won" && l.Status != "Lost")
            .Sum(l => l.PotentialValue);

        var byStage = leads
            .GroupBy(l => l.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        var byManager = leads
            .Where(l => l.AssignedTo != null)
            .GroupBy(l => l.AssignedTo!.FullName)
            .Select(g => new ManagerStatDto(
                g.Key,
                g.Count(),
                g.Count(l => l.Status == "Won"),
                g.Where(l => l.Status == "Won").Sum(l => l.PotentialValue)
            ))
            .OrderByDescending(m => m.WonRevenue)
            .ToList();

        return Ok(new DashboardStatsDto(
            leads.Count,
            wonRevenue,
            won.Count,
            conversionRate,
            pipelineValue,
            byStage,
            byManager));
    }

    // GET /api/dashboard/followups  — overdue + upcoming
    [HttpGet("followups")]
    public async Task<ActionResult> GetFollowUps()
    {
        var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
        var soon = DateTime.UtcNow.AddDays(2).Date.ToString("yyyy-MM-dd");

        var active = await _db.Leads
            .Include(l => l.AssignedTo)
            .Where(l => l.NextFollowUpDate != null &&
                        l.Status != "Won" && l.Status != "Lost")
            .OrderBy(l => l.NextFollowUpDate)
            .Select(l => MappingHelper.ToListDto(l))
            .ToListAsync();

        var overdue = active.Where(l => string.Compare(l.NextFollowUpDate, today) < 0).ToList();
        var dueSoon = active.Where(l =>
            string.Compare(l.NextFollowUpDate, today) >= 0 &&
            string.Compare(l.NextFollowUpDate, soon) <= 0).ToList();
        var upcoming = active.Where(l =>
            string.Compare(l.NextFollowUpDate!, soon) > 0).ToList();

        return Ok(new { overdue, dueSoon, upcoming });
    }
}

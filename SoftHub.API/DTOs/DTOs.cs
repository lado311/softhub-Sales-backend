namespace SoftHub.API.DTOs;

// ─── Auth ────────────────────────────────────────────────
public record LoginRequest(string Email, string Password);

public record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    string Role = "Manager"
);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    UserDto User
);

public record RefreshRequest(string RefreshToken);

// ─── User ────────────────────────────────────────────────
public record UserDto(
    int Id,
    string FullName,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAt
);

public record UpdateUserRequest(
    string? FullName,
    string? Email,
    string? Password,
    string? Role,
    bool? IsActive
);

// ─── Lead ────────────────────────────────────────────────
public record CreateLeadRequest(
    string CompanyName,
    string Industry,
    string ContactPersonName,
    string ContactPersonPosition,
    string Phone,
    string Email,
    string Location,
    string CompanySize,
    string Source,
    string Status,
    string InterestLevel,
    decimal PotentialValue,
    int? AssignedToId,
    string? NextFollowUpDate,
    string? InitialNote
);

public record UpdateLeadRequest(
    string? CompanyName,
    string? Industry,
    string? ContactPersonName,
    string? ContactPersonPosition,
    string? Phone,
    string? Email,
    string? Location,
    string? CompanySize,
    string? Source,
    string? Status,
    string? InterestLevel,
    decimal? PotentialValue,
    int? AssignedToId,
    string? NextFollowUpDate
);

public record LeadDto(
    int Id,
    string CompanyName,
    string Industry,
    string ContactPersonName,
    string ContactPersonPosition,
    string Phone,
    string Email,
    string Location,
    string CompanySize,
    string Source,
    string Status,
    string InterestLevel,
    decimal PotentialValue,
    string? NextFollowUpDate,
    DateTime CreatedAt,
    DateTime? LastContactedAt,
    UserDto? AssignedTo,
    List<NoteDto> Notes,
    List<ActivityDto> Activities,
    int LeadScore
);

public record LeadListItemDto(
    int Id,
    string CompanyName,
    string Industry,
    string ContactPersonName,
    string ContactPersonPosition,
    string Email,
    string Phone,
    string Location,
    string CompanySize,
    string Source,
    string Status,
    string InterestLevel,
    decimal PotentialValue,
    string? NextFollowUpDate,
    DateTime CreatedAt,
    DateTime? LastContactedAt,
    string? AssignedToName,
    int? AssignedToId,
    int LeadScore
);
public record MoveLeadRequest(string Status);

public record BulkUpdateRequest(
    List<int> LeadIds,
    string? Status,
    int? AssignedToId
);

// ─── Note ────────────────────────────────────────────────
public record CreateNoteRequest(string Text);

public record NoteDto(
    int Id,
    string Text,
    DateTime CreatedAt,
    string AuthorName,
    int AuthorId
);

// ─── Activity ────────────────────────────────────────────
public record ActivityDto(
    int Id,
    string Action,
    string Description,
    string? OldValue,
    string? NewValue,
    DateTime CreatedAt,
    string? UserName
);

// ─── Filters & Pagination ────────────────────────────────
public record LeadFilterRequest(
    string? Search,
    string? Status,
    string? Industry,
    int? AssignedToId,
    DateTime? DateFrom,
    DateTime? DateTo,
    string SortBy = "CreatedAt",
    string SortDir = "desc",
    int Page = 1,
    int PageSize = 50
);

public record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

// ─── Dashboard ───────────────────────────────────────────
public record DashboardStatsDto(
    int TotalLeads,
    decimal WonRevenue,
    int WonCount,
    int ConversionRate,
    decimal PipelineValue,
    Dictionary<string, int> ByStage,
    List<ManagerStatDto> ByManager
);

public record ManagerStatDto(
    string Name,
    int TotalLeads,
    int WonLeads,
    decimal WonRevenue
);

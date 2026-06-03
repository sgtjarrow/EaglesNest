using EaglesNest.Core.Domain;

namespace EaglesNest.Web.Services.Roles;

public sealed record RoleMemberListItem(
    Guid Id,
    string DisplayName,
    string LegalName,
    string ChapterName,
    string ChapterAbbreviation,
    MemberStatus Status);

public sealed record RoleAssignmentItem(
    Guid Id,
    Guid MemberId,
    OfficerPosition Position,
    Guid OrganizationUnitId,
    string OrganizationName,
    string OrganizationAbbreviation,
    DateTimeOffset AssignedAt,
    DateTimeOffset? ExpiresAt);

public sealed record RoleAssignmentHistoryItem(
    Guid Id,
    Guid MemberId,
    OfficerPosition Position,
    Guid OrganizationUnitId,
    string OrganizationName,
    string OrganizationAbbreviation,
    DateTimeOffset AssignedAt,
    DateTimeOffset? ExpiresAt,
    string? AssignedBy,
    string? EndedBy);

public sealed record RoleChapterOption(
    Guid Id,
    string Name,
    string Abbreviation,
    OrganizationLevel Level,
    string? StateName,
    string? StateAbbreviation);

public sealed record RoleSaveResult(bool Succeeded, string[] Errors)
{
    public static RoleSaveResult Success() => new(true, []);
    public static RoleSaveResult Failure(params string[] errors) => new(false, errors);
}

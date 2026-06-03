using EaglesNest.Core.Domain;

namespace EaglesNest.Web.Services.Chapters;

public sealed record ChapterOptionItem(
    Guid Id,
    string Abbreviation,
    string Name,
    OrganizationLevel Level,
    Guid? ParentOrganizationUnitId,
    string? StateName,
    string? StateAbbreviation);

public sealed record ChapterOptionGroup(string Label, IReadOnlyList<ChapterOptionItem> Chapters);

namespace EaglesNest.Web.Services.Members;

public sealed record MemberChapterAssignmentItem
{
    public Guid Id { get; init; }
    public string ChapterName { get; init; } = string.Empty;
    public string ChapterAbbreviation { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public bool IsPrimary { get; init; }
}

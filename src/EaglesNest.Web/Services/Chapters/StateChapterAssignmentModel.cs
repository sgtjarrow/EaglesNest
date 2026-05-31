namespace EaglesNest.Web.Services.Chapters;

public class StateChapterAssignmentModel
{
    public Guid Id { get; set; }
    public Guid StateOrganizationUnitId { get; set; }
    public Guid LocalChapterOrganizationUnitId { get; set; }
    public string LocalChapterName { get; set; } = string.Empty;
    public string LocalChapterAbbreviation { get; set; } = string.Empty;
    public DateOnly StartsOn { get; set; }
    public DateOnly? EndsOn { get; set; }
    public string? Notes { get; set; }
}

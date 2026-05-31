using EaglesNest.Core.Domain;

namespace EaglesNest.Web.Services.Organizations;

public class OrganizationTreeItem
{
    public Guid Id { get; set; }
    public Guid? ParentOrganizationUnitId { get; set; }
    public OrganizationLevel Level { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? StateCode { get; set; }
    public string? MailingCity { get; set; }
    public string? MailingStateCode { get; set; }
    public OrganizationStatus Status { get; set; }
    public Guid? ActingStateChapterId { get; set; }
    public string? ActingStateChapterAbbreviation { get; set; }
    public string? ActingStateChapterName { get; set; }
    public List<OrganizationTreeItem> Children { get; set; } = [];
}

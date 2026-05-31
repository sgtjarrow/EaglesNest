using System.ComponentModel.DataAnnotations;
using EaglesNest.Core.Domain;

namespace EaglesNest.Web.Services.Organizations;

public class OrganizationEditModel
{
    public Guid? Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(25)]
    public string Abbreviation { get; set; } = string.Empty;

    public OrganizationLevel Level { get; set; } = OrganizationLevel.LocalChapter;

    public Guid? ParentOrganizationUnitId { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(10)]
    public string? StateCode { get; set; }

    [StringLength(50)]
    public string? CharterNumber { get; set; }

    public bool IsActive { get; set; } = true;
}

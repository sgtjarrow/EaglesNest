namespace EaglesNest.Web.Services.Members;

public sealed class MilitaryServiceEditModel
{
    public Guid? Id { get; set; }
    public string Branch { get; set; } = string.Empty;
    public string? Rank { get; set; }
    public DateOnly? ServiceStartDate { get; set; }
    public DateOnly? ServiceEndDate { get; set; }
    public string? DischargeType { get; set; }
    public string? ConflictTab { get; set; }
    public string? ServiceNotes { get; set; }
}

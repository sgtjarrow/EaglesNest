namespace EaglesNest.Infrastructure.Identity;

public sealed class RolePermission
{
    public Guid Id { get; set; }
    public string RoleId { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;
}

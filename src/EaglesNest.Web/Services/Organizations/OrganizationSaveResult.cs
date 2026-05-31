namespace EaglesNest.Web.Services.Organizations;

public class OrganizationSaveResult
{
    private OrganizationSaveResult(bool succeeded, Guid? organizationId, IReadOnlyList<string> errors)
    {
        Succeeded = succeeded;
        OrganizationId = organizationId;
        Errors = errors;
    }

    public bool Succeeded { get; }
    public Guid? OrganizationId { get; }
    public IReadOnlyList<string> Errors { get; }

    public static OrganizationSaveResult Success(Guid organizationId)
    {
        return new OrganizationSaveResult(true, organizationId, []);
    }

    public static OrganizationSaveResult Failure(params string[] errors)
    {
        return new OrganizationSaveResult(false, null, errors);
    }
}

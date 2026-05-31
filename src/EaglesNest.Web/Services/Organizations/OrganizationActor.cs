namespace EaglesNest.Web.Services.Organizations;

public sealed record OrganizationActor(string? ApplicationUserId, string ActorName, string ActorSource)
{
    public static OrganizationActor System(string source)
    {
        return new OrganizationActor(null, source, source);
    }
}

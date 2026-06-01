namespace EaglesNest.Web.Services.Members;

public sealed record MemberSaveResult(bool Succeeded, Guid? MemberId, IReadOnlyList<string> Errors)
{
    public static MemberSaveResult Success(Guid memberId)
    {
        return new MemberSaveResult(true, memberId, []);
    }

    public static MemberSaveResult Failure(params string[] errors)
    {
        return new MemberSaveResult(false, null, errors);
    }
}

namespace EaglesNest.Web.Services.Chapters;

public class ChapterSaveResult
{
    private ChapterSaveResult(bool succeeded, Guid? organizationId, IReadOnlyList<string> errors)
    {
        Succeeded = succeeded;
        ChapterId = organizationId;
        Errors = errors;
    }

    public bool Succeeded { get; }
    public Guid? ChapterId { get; }
    public IReadOnlyList<string> Errors { get; }

    public static ChapterSaveResult Success(Guid organizationId)
    {
        return new ChapterSaveResult(true, organizationId, []);
    }

    public static ChapterSaveResult Failure(params string[] errors)
    {
        return new ChapterSaveResult(false, null, errors);
    }
}

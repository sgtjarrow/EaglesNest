namespace EaglesNest.Web.Services.Chapters;

public sealed record ChapterActor(string? ApplicationUserId, string ActorName, string ActorSource)
{
    public static ChapterActor System(string source)
    {
        return new ChapterActor(null, source, source);
    }
}

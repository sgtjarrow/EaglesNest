using EaglesNest.Core.Domain;

namespace EaglesNest.Web.Services.Members;

public static class MemberOptionValues
{
    public static readonly MemberStatus[] Statuses =
    [
        MemberStatus.Prospect,
        MemberStatus.Probate,
        MemberStatus.PatchHolder,
        MemberStatus.Retired,
        MemberStatus.Suspended,
        MemberStatus.Out,
        MemberStatus.Deceased
    ];

    public static readonly MemberStatusOptionGroup[] StatusGroups =
    [
        new("Normal Flow", [MemberStatus.Prospect, MemberStatus.Probate, MemberStatus.PatchHolder, MemberStatus.Retired]),
        new("Alternate Paths", [MemberStatus.Suspended, MemberStatus.Out]),
        new("Final", [MemberStatus.Deceased])
    ];

    public static readonly string[] BloodTypes = ["A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-"];
    public static readonly string[] Genders = ["Male", "Female"];
    public static readonly string[] Branches = ["Army", "Navy", "Air Force", "Marine Corps", "Coast Guard", "Space Force"];
    public static readonly string[] DischargeTypes = ["Bad Conduct", "Dishonorable", "Entry-Level Separation", "General", "Honorable", "Medical", "OTH", "Other"];
    public static readonly string[] ConflictTabs = ["Afghanistan", "Iraq", "Korea Defense", "Kosovo"];

    public static string DisplayStatus(MemberStatus status)
    {
        return status == MemberStatus.PatchHolder ? "Patch Holder" : status.ToString();
    }
}

public sealed record MemberStatusOptionGroup(string Label, IReadOnlyList<MemberStatus> Statuses);

using EaglesNest.Core.Domain;

namespace EaglesNest.Web.Services.Members;

public static class MemberOptionValues
{
    public static readonly MemberStatus[] Statuses =
    [
        MemberStatus.Prospect,
        MemberStatus.Probate,
        MemberStatus.PatchHolder,
        MemberStatus.Suspended,
        MemberStatus.Out,
        MemberStatus.Retired,
        MemberStatus.Deceased
    ];

    public static readonly string[] BloodTypes = ["A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-"];
    public static readonly string[] Genders = ["Male", "Female"];
    public static readonly string[] Branches = ["Army", "Marine Corps", "Navy", "Air Force", "Space Force", "Coast Guard"];
    public static readonly string[] DischargeTypes = ["Honorable", "General", "OTH", "Bad Conduct", "Dishonorable", "Entry-Level Separation", "Medical", "Other"];
    public static readonly string[] ConflictTabs = ["Iraq", "Afghanistan", "Korea Defense", "Kosovo"];

    public static string DisplayStatus(MemberStatus status)
    {
        return status == MemberStatus.PatchHolder ? "Patch Holder" : status.ToString();
    }
}

using EaglesNest.Core.Domain;
using EaglesNest.Web.Services.Members;

namespace EaglesNest.Tests.Members;

public class MemberOptionValuesTests
{
    [Fact]
    public void StatusGroups_AreOrderedForClubWorkflow()
    {
        var statuses = MemberOptionValues.StatusGroups
            .SelectMany(group => group.Statuses)
            .ToArray();

        Assert.Equal(
            [
                MemberStatus.Prospect,
                MemberStatus.Probate,
                MemberStatus.PatchHolder,
                MemberStatus.Retired,
                MemberStatus.Suspended,
                MemberStatus.Out,
                MemberStatus.Deceased
            ],
            statuses);
    }

    [Fact]
    public void MilitaryServiceOptions_AreOrderedForDisplay()
    {
        Assert.Equal(
            ["Army", "Navy", "Air Force", "Marine Corps", "Coast Guard", "Space Force"],
            MemberOptionValues.Branches);
        Assert.Equal(MemberOptionValues.DischargeTypes.Order(StringComparer.OrdinalIgnoreCase), MemberOptionValues.DischargeTypes);
        Assert.Equal(MemberOptionValues.ConflictTabs.Order(StringComparer.OrdinalIgnoreCase), MemberOptionValues.ConflictTabs);
    }
}

using EaglesNest.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace EaglesNest.Web.Data.Seed;

public static class OrganizationSeeder
{
    private static readonly StateSeed[] States =
    [
        new("Alabama", "AL"),
        new("Alaska", "AK"),
        new("Arizona", "AZ"),
        new("Arkansas", "AR"),
        new("California", "CA"),
        new("Colorado", "CO"),
        new("Connecticut", "CT"),
        new("Delaware", "DE"),
        new("Florida", "FLA"),
        new("Georgia", "GA"),
        new("Hawaii", "HI"),
        new("Idaho", "ID"),
        new("Illinois", "IL"),
        new("Indiana", "IN"),
        new("Iowa", "IA"),
        new("Kansas", "KS"),
        new("Kentucky", "KY"),
        new("Louisiana", "LA"),
        new("Maine", "ME"),
        new("Maryland", "MD"),
        new("Massachusetts", "MA"),
        new("Michigan", "MI"),
        new("Minnesota", "MN"),
        new("Mississippi", "MS"),
        new("Missouri", "MO"),
        new("Montana", "MT"),
        new("Nebraska", "NE"),
        new("Nevada", "NV"),
        new("New Hampshire", "NH"),
        new("New Jersey", "NJ"),
        new("New Mexico", "NM"),
        new("New York", "NY"),
        new("North Carolina", "NC"),
        new("North Dakota", "ND"),
        new("Ohio", "OH"),
        new("Oklahoma", "OK"),
        new("Oregon", "OR"),
        new("Pennsylvania", "PA"),
        new("Rhode Island", "RI"),
        new("South Carolina", "SC"),
        new("South Dakota", "SD"),
        new("Tennessee", "TN"),
        new("Texas", "TX"),
        new("Utah", "UT"),
        new("Vermont", "VT"),
        new("Virginia", "VA"),
        new("Washington", "WA"),
        new("West Virginia", "WVA"),
        new("Wisconsin", "WI"),
        new("Wyoming", "WY")
    ];

    private static readonly ChapterSeed[] Chapters =
    [
        new("The Originals", "FLA-1", "FLA", null, "FLA"),
        new("South Broward", "FLA-2", "FLA", "Hialeah", "FL"),
        new("Fort Pierce", "FLA-4", "FLA", "Fort Pierce", "FL"),
        new("Daytona", "FLA-6", "FLA", "Holly Hill", "FL"),
        new("Tampa", "FLA-7", "FLA", "Pinellas Park", "FL"),
        new("Greater Orlando", "FLA-8", "FLA", "St. Cloud", "FL"),
        new("West Volusia", "FLA-9", "FLA", "Deland", "FL"),
        new("Lake Co", "FLA-10", "FLA", "Leesburg", "FL"),
        new("Sebring", "FLA-11", "FLA", null, "FLA"),
        new("Space Coast", "FLA-13", "FLA", "Grant", "FL"),
        new("Marion Co.", "FLA-14", "FLA", "Ocklawaha", "FL"),
        new("Okaloosa Co", "FLA-15", "FLA", "Crestview", "FL"),
        new("Martin Co.", "FLA-16", "FLA", "Stuart", "FL"),
        new("Alachua Co", "FLA-17", "FLA", "Lake Butler", "FL"),
        new("Hernando Co.", "FLA-18", "FLA", "Brooksville", "FL"),
        new("Pensacola", "FLA-20", "FLA", "Pace", "FL"),
        new("New Albany", "IN-3", "IN", "English", "IN"),
        new("Ft Wayne", "IN-4", "IN", "Fort Wayne", "IN"),
        new("Hoosier Hills", "IN-5", "IN", "Salem", "IN"),
        new("Richmond", "IN-7", "IN", "Connersville", "IN"),
        new("Columbus", "IN-8", "IN", "Columbus", "IN"),
        new("Kokomo", "IN-9", "IN", "Logansport", "IN"),
        new("Brazil", "IN-10", "IN", "Brazil", "IN"),
        new("Corpus Christi", "TX-1", "TX", "Corpus Christi", "TX"),
        new("Six Shooters", "TX-2", "TX", null, "TX"),
        new("Fort Worth", "TX-3", "TX", "Krum", "TX"),
        new("Central", "TX-4", "TX", "Killeen", "TX"),
        new("Dallas", "TX-5", "TX", "Nevada", "TX"),
        new("Eglin", "TX-6", "TX", "Elgin", "TX"),
        new("Houston", "TX-7", "TX", "Tombull", "TX"),
        new("Amarillo", "TX-8", "TX", "Canyon", "TX"),
        new("Atlanta", "GA-1", "GA", "Powder Springs", "GA"),
        new("Dalton Gang", "GA-2", "GA", "Chatsworth", "GA"),
        new("Warner Robins", "GA-3", "GA", "Warner Robins", "GA"),
        new("Savannah", "GA-4", "GA", "Savanna", "GA"),
        new("Cartersville", "GA-5", "GA", "Cartersville", "GA"),
        new("Augusta", "GA-6", "GA", "Augusta", "GA"),
        new("Surrency", "GA-7", "GA", "Surrency", "GA"),
        new("Statesboro", "GA-8", "GA", "Brooklet", "GA"),
        new("Lamar County", "GA-10", "GA", "Milner", "GA"),
        new("Cumming", "GA-11", "GA", "Cumming", "GA"),
        new("Valdosta", "GA-12", "GA", "Valdosta", "GA"),
        new("Dooley Co.", "GA-13", "GA", "Fort Valley", "GA"),
        new("Washington", "GA-14", "GA", "Warrenton", "GA"),
        new("Wheeler Co.", "GA-15", "GA", "Glenwood", "GA"),
        new("St Marys", "GA-16", "GA", "St. Mary's", "GA"),
        new("Bowdon", "GA-17", "GA", "Carrollton", "GA"),
        new("Young Harris", "GA-18", "GA", "Young Harris", "GA"),
        new("Nashville", "TN-2", "TN", "Lebanon", "TN"),
        new("Sevierville", "TN-3", "TN", "Sevierville", "TN"),
        new("Chattanooga", "TN-4", "TN", "Beersheba Springs", "TN"),
        new("Clarksville", "TN-5", "TN", "Clarksville", "TN"),
        new("Union City", "TN-6", "TN", "Kenton", "TN"),
        new("Eagleville", "TN-7", "TN", "Smyrna", "TN"),
        new("Greeneville", "TN-8", "TN", "Chuckey", "TN"),
        new("Sweetwater", "TN-9", "TN", "Sweetwater", "TN"),
        new("Mason Dixon", "PA-2", "PA", "Rouzerville", "PA"),
        new("Pittsburgh", "PA-3", "PA", "Industry", "PA"),
        new("South Central", "PA-4", "PA", "York", "PA"),
        new("Berks County", "PA-5", "PA", "Robesonia", "PA"),
        new("Lebanon", "PA-7", "PA", "Jonestown", "PA"),
        new("Westmoreland Co", "PA-9", "PA", "Blairsville", "PA"),
        new("Western MD", "MD-2", "MD", "Hagerstown", "MD"),
        new("Southern MD", "MD-3", "MD", "Leanardtown", "MD"),
        new("North East MD", "MD-4", "MD", "Jarrettsville", "MD"),
        new("Ocean City", "MD-5", "MD", "Salisbury", "MD"),
        new("North Central", "MD-6", "MD", "Thurmont", "MD"),
        new("Cambridge", "MD-7", "MD", "Hurlock", "MD"),
        new("Long Island", "NY-1", "NY", "Yaphank", "NY"),
        new("Smyrna", "NY-3", "NY", "Smyrna", "NY"),
        new("Central New York", "NY-4", "NY", "Middletown", "NY"),
        new("Watertown", "NY-5", "NY", "Carthage", "NY"),
        new("Finger Lakes", "NY-6", "NY", "Webster", "NY"),
        new("Mohawk Valley", "NY-7", "NY", "Johnstown", "NY"),
        new("Walking Dead", "WVA-1", "WVA", null, "WV"),
        new("Estrn Panhandle", "WVA-2", "WVA", "Harpers Ferry", "WV"),
        new("Charleston", "WVA-3", "WVA", "Buffalo", "WV"),
        new("Parkersburg", "WVA-4", "WVA", "Parkersburg", "WV"),
        new("Fort Ashby", "WVA-5", "WVA", "Hedgesville", "WV"),
        new("Muscoda", "WI-1", "WI", "Eastman", "WI"),
        new("Minutemen", "MA-1", "MA", null, "MA"),
        new("Worcester", "MA-2", "MA", "Blackstone", "MA"),
        new("Newport News", "VA-1", "VA", "Newport News", "VA"),
        new("Southern VA", "VA-2", "VA", "Danville", "VA"),
        new("Culpeper", "VA-4", "VA", "Stafford", "VA"),
        new("Virginia Beach", "VA-5", "VA", "Chesapeake", "VA"),
        new("Richmond", "VA-6", "VA", "Chester", "VA"),
        new("Leesburg", "VA-7", "VA", "Manassas", "VA"),
        new("Winchester", "VA-8", "VA", "Middletown", "VA"),
        new("Pulaski", "VA-10", "VA", "Salem", "VA"),
        new("Red Dirt", "OK-2", "OK", null, "OK"),
        new("Bitter Creek", "OK-3", "OK", "Altus", "OK"),
        new("Devil Wind", "OH-2", "OH", null, "OH"),
        new("Sandusky", "OH-3", "OH", "Oak Harbor", "OH"),
        new("Middletown", "OH-4", "OH", "Lebanon", "OH"),
        new("Perrysburg", "OH-6", "OH", "Walbridge", "OH"),
        new("Van Wert", "OH-7", "OH", "Van Wert", "OH"),
        new("Sidney", "OH-8", "OH", "Sidney", "OH"),
        new("Cincinnati", "OH-9", "OH", "Milford", "OH"),
        new("Alabama 2", "AL-2", "AL", "Prattville", "AL"),
        new("Enterprise", "AL-3", "AL", "Andalusia", "AL"),
        new("IRREGULARs", "AL-4", "AL", null, "AL"),
        new("Great Lakes", "MI-1", "MI", null, "MI"),
        new("Maybee", "MI-2", "MI", "Maybee", "MI"),
        new("Wayne Co.", "MI-4", "MI", "Westland", "MI"),
        new("Durand", "MI-5", "MI", "Swartz Creek", "MI"),
        new("Frozen Chosen", "ND-1", "ND", null, "ND"),
        new("Cumberland", "NC-1", "NC", "Cumberland", "NC"),
        new("Indian Trail", "NC-3", "NC", "Indian Trail", "NC"),
        new("Watertown", "SD-1", "SD", "Watertown", "SD"),
        new("Lennox", "SD-2", "SD", "Lennox", "SD"),
        new("Sturgis", "SD-3", "SD", null, "SD"),
        new("Greenville", "SC-1", "SC", "Easley", "SC"),
        new("Lexington", "SC-2", "SC", "Lexington", "SC"),
        new("The Lost Boys", "SC-3", "SC", null, "SC"),
        new("Walterboro", "SC-4", "SC", "Goose Creek", "SC"),
        new("De Soto Co.", "MS-2", "MS", "Southaven", "MS"),
        new("FUWFKY", "KY-1", "KY", null, "KY"),
        new("Mesa", "AZ-1", "AZ", "Queen Creek", "AZ"),
        new("JAB Crew", "AZ-2", "AZ", null, "AZ"),
        new("Buckeye", "AZ-3", "AZ", "El Mirage", "AZ"),
        new("Krewe Du Revenant", "LA-1", "LA", null, "LA"),
        new("Live Free or Die", "NH-1", "NH", null, "NH"),
        new("Dover", "NH-2", "NH", "Rochester", "NH"),
        new("Road Trolls", "DE-1", "DE", null, "DE"),
        new("Kent County", "DE-2", "DE", "Camden", "DE"),
        new("Maniacs", "ME-1", "ME", null, "ME"),
        new("Bonney Lake", "WA-1", "WA", null, "WA"),
        new("Bushwhackers", "MO-1", "MO", null, "MO"),
        new("Eternal Chapter", "US-100", "NAT", null, "US")
    ];

    public static async Task SeedAsync(ApplicationDbContext dbContext)
    {
        var national = await GetOrCreateAsync(dbContext, "National", "NAT", OrganizationLevel.National, null, null, null);

        foreach (var state in States)
        {
            await GetOrCreateAsync(dbContext, state.Name, state.Abbreviation, OrganizationLevel.State, national.Id, null, StateCodeFromAbbreviation(state.Abbreviation));
        }

        var parents = await dbContext.OrganizationUnits.ToDictionaryAsync(unit => unit.Abbreviation);
        foreach (var chapter in Chapters)
        {
            var parentId = parents.TryGetValue(chapter.ParentAbbreviation, out var parent)
                ? parent.Id
                : national.Id;

            await GetOrCreateAsync(dbContext, chapter.Name, chapter.Abbreviation, OrganizationLevel.LocalChapter, parentId, chapter.City, chapter.StateCode);
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task<OrganizationUnit> GetOrCreateAsync(
        ApplicationDbContext dbContext,
        string name,
        string abbreviation,
        OrganizationLevel level,
        Guid? parentId,
        string? city,
        string? stateCode)
    {
        var unit = await dbContext.OrganizationUnits.SingleOrDefaultAsync(existing => existing.Abbreviation == abbreviation);
        if (unit is not null)
        {
            return unit;
        }

        unit = new OrganizationUnit
        {
            Id = Guid.NewGuid(),
            Name = name,
            Abbreviation = abbreviation,
            Level = level,
            ParentOrganizationUnitId = parentId,
            City = city,
            StateCode = stateCode,
            IsActive = true
        };

        dbContext.OrganizationUnits.Add(unit);
        await dbContext.SaveChangesAsync();
        return unit;
    }

    private static string StateCodeFromAbbreviation(string abbreviation)
    {
        return abbreviation switch
        {
            "FLA" => "FL",
            "WVA" => "WV",
            _ => abbreviation
        };
    }

    private sealed record StateSeed(string Name, string Abbreviation);

    private sealed record ChapterSeed(string Name, string Abbreviation, string ParentAbbreviation, string? City, string? StateCode);
}

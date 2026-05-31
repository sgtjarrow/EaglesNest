using EaglesNest.Core.Domain;
using EaglesNest.Web.Services.Organizations;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

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
        new("IN", "IN-2", "IN", "New Palestine", "IN"),
        new("GA", "GA-9", "GA", "Milledgeville", "GA"),
        new("TN", "TN-1", "TN", "Millington", "TN"),
        new("PA", "PA-1", "PA", "New Castle", "PA"),
        new("MD", "MD-1", "MD", "Hagerstown", "MD"),
        new("NJ", "NJ-1", "NJ", "Cape May Court House", "NJ"),
        new("WI", "WI-2", "WI", "Poynette", "WI"),
        new("VA", "VA-3", "VA", "Woodbridge", "VA"),
        new("NC", "NC-2", "NC", "Barium Springs", "NC"),
        new("MS", "MS-1", "MS", "Ocean Springs", "MS"),
        new("CT", "CT-1", "CT", "Vernon", "CT"),
        new("IL", "IL-1", "IL", "Fithian", "IL"),
        new("NM", "NM-1", "NM", "McIntosh", "NM"),
        new("MN", "MN-1", "MN", "Alexandria", "MN"),
        new("Eternal Chapter", "Chapter-100", "NAT", null, null)
    ];

    private static readonly StateChapterSeed[] StateChapterAssignments =
    [
        new("FLA", "FLA-1"),
        new("IN", "IN-2"),
        new("TX", "TX-2"),
        new("GA", "GA-9"),
        new("TN", "TN-1"),
        new("PA", "PA-1"),
        new("MD", "MD-1"),
        new("NY", "NY-6"),
        new("WVA", "WVA-1"),
        new("NJ", "NJ-1"),
        new("WI", "WI-2"),
        new("MA", "MA-1"),
        new("VA", "VA-3"),
        new("OK", "OK-2"),
        new("OH", "OH-2"),
        new("AL", "AL-4"),
        new("MI", "MI-1"),
        new("ND", "ND-1"),
        new("NC", "NC-2"),
        new("SD", "SD-3"),
        new("SC", "SC-3"),
        new("MS", "MS-1"),
        new("CT", "CT-1"),
        new("KY", "KY-1"),
        new("AZ", "AZ-2"),
        new("LA", "LA-1"),
        new("NH", "NH-1"),
        new("DE", "DE-1"),
        new("IL", "IL-1"),
        new("ME", "ME-1"),
        new("NM", "NM-1"),
        new("WA", "WA-1"),
        new("MO", "MO-1"),
        new("MN", "MN-1")
    ];

    private static readonly AddressSeed[] MailingAddresses =
    [
        new("AL-2", @"1012 County Road 40 W", @"Prattville", @"AL", @"36067"),
        new("AL-3", @"20977 Edwards Rd.", @"Andalusia", @"AL", @"36421"),
        new("AL-4", @"18615 Jefferson St.", @"Athens", @"AL", @"35611"),
        new("AZ-1", @"22738 S 228th Pl", @"Queen Creek", @"AZ", @"85142"),
        new("AZ-2", @"996 Storm Cloud Dr.", @"Kingman", @"AZ", @"86409"),
        new("AZ-3", @"11918 W Corrine Dr.", @"El Mirage", @"AZ", @"85335"),
        new("CT-1", @"114 West St.", @"Vernon", @"CT", @"6066"),
        new("Chapter-100", null, null, null, null),
        new("DE-1", @"13 Marlin Court", @"New Castle", @"DE", @"19720"),
        new("DE-2", @"5 East St.", @"Camden", @"DE", @"19934"),
        new("FLA-1", @"12072 NW 27th Dr", @"Coral Springs", @"FL", @"33065"),
        new("FLA-10", @"36136 Emeralda Ave", @"Leesburg", @"FL", @"34788"),
        new("FLA-11", @"95 N Olivia Dr Avon Park Fl 33825", null, null, null),
        new("FLA-13", @"7489 Tourmaline Dr.", @"Grant", @"FL", @"32949"),
        new("FLA-14", @"PO Box 148", @"Ocklawaha", @"FL", @"32183"),
        new("FLA-15", @"5601 Highway 393", @"Crestview", @"FL", @"32539"),
        new("FLA-16", @"PO Box 1514", @"Stuart", @"FL", @"34995"),
        new("FLA-17", @"4650 SW 107th Ln", @"Lake Butler", @"FL", @"32054"),
        new("FLA-18", @"18427 Success Rd", @"Brooksville", @"FL", @"34604"),
        new("FLA-2", @"19065 NW 85th Ave.", @"Hialeah", @"FL", @"33015-5375"),
        new("FLA-20", @"4780 La Casa Cir", @"Pace", @"FL", @"32571"),
        new("FLA-4", @"3475 Douglas St.", @"Fort Pierce", @"FL", @"34981"),
        new("FLA-6", @"358 8th St.", @"Holly Hill", @"FL", @"32117"),
        new("FLA-7", @"P.O. Box 1281", @"Pinellas Park", @"FL", @"33780"),
        new("FLA-8", @"PO.Box  702193", @"St. Cloud", @"FL", @"34770"),
        new("FLA-9", @"273 Huntington Dr.", @"Deland", @"FL", @"32724"),
        new("GA-1", @"PO Box 2343", @"Powder Springs", @"GA", @"30127"),
        new("GA-10", @"104 Greenwood St #614", @"Milner", @"GA", @"30257"),
        new("GA-11", @"PO Box 1314", @"Cumming", @"GA", @"30040"),
        new("GA-12", @"2417 S Patterson St", @"Valdosta", @"GA", @"31601"),
        new("GA-13", @"73 Ridgeway Dr.", @"Fort Valley", @"GA", @"31030"),
        new("GA-14", @"615 Main St. Box 701", @"Warrenton", @"GA", @"30828-9998"),
        new("GA-15", @"PO Box 453", @"Glenwood", @"GA", @"30428"),
        new("GA-16", @"P.O. Box 5682", @"St. Mary's", @"GA", @"31558-5682"),
        new("GA-17", @"1768 Four Notch Rd", @"Carrollton", @"GA", @"30116"),
        new("GA-18", @"556 Ivylog Creek Rd", @"Young Harris", @"GA", @"30582"),
        new("GA-2", @"102 n 2nd ave", @"Chatsworth", @"GA", @"30705"),
        new("GA-3", @"201 Wyler Ave.", @"Warner Robins", @"GA", @"31093"),
        new("GA-4", @"PO Box 3022", @"Savanna", @"GA", @"31402"),
        new("GA-5", @"142 Cowan Dr. SW", @"Cartersville", @"GA", @"30120-5303"),
        new("GA-6", @"PO Box 211755", @"Augusta", @"GA", @"30917"),
        new("GA-7", @"PO Box 91", @"Surrency", @"GA", @"31563"),
        new("GA-8", @"PO Box 702", @"Brooklet", @"GA", @"30415"),
        new("GA-9", @"PO Box 404", @"Milledgeville", @"GA", @"31059"),
        new("IL-1", @"6669 E. Lincoln Trail", @"Fithian", @"IL", @"61844"),
        new("IN-10", @"PO Box 514", @"Brazil", @"IN", @"47834"),
        new("IN-2", @"7331 W. Beyers Ct", @"New Palestine", @"IN", @"46163"),
        new("IN-3", @"8791 S. Old Union Church Rd.", @"English", @"IN", @"47118-6028"),
        new("IN-4", @"2920 Connett Ave.", @"Fort Wayne", @"IN", @"46802"),
        new("IN-5", @"3186 W Nubian Rd", @"Salem", @"IN", @"47167"),
        new("IN-7", @"8287 W Johnson School Rd.", @"Connersville", @"IN", @"47331"),
        new("IN-8", @"221 S Beaty St.", @"Columbus", @"IN", @"47201"),
        new("IN-9", @"228 Humphery St.", @"Logansport", @"IN", @"46947"),
        new("KY-1", @"414 Highland Ave. PO Box 341", @"Vine Grove", @"KY", @"40175"),
        new("LA-1", @"29070 E Ruth St", @"Lacombe", @"LA", @"70445"),
        new("MA-1", @"30 Blakeley St.", @"Lynn", @"MA", @"1915"),
        new("MA-2", @"12 New York Ave", @"Blackstone", @"MA", @"1504"),
        new("MD-1", @"14616 Barkdoll Rd", @"Hagerstown", @"MD", @"21742"),
        new("MD-2", @"11206 Hollywood rd", @"Hagerstown", @"MD", @"21740"),
        new("MD-3", @"27520 Point Lookout Rd", @"Leanardtown", @"MD", null),
        new("MD-4", @"1714 Morse Rd", @"Jarrettsville", @"MD", @"21084"),
        new("MD-5", @"404 S. Kaywood Dr.", @"Salisbury", @"MD", @"21804"),
        new("MD-6", @"11072 Powell Rd.", @"Thurmont", @"MD", @"21788"),
        new("MD-7", @"500 Academy St.", @"Hurlock", @"MD", @"21643"),
        new("ME-1", @"10 Old Falls Rd", @"Kennebunk", @"ME", @"4043"),
        new("MI-1", @"3744 E. Michigan ave", @"Jackson", @"MI", @"49202"),
        new("MI-2", @"PO Box 26", @"Maybee", @"MI", @"48159"),
        new("MI-4", @"33300 Warren Rd #24", @"Westland", @"MI", @"48185"),
        new("MI-5", @"5177 Durwood Dr", @"Swartz Creek", @"MI", @"48437"),
        new("MN-1", @"PO Box 711", @"Alexandria", @"MN", @"56308"),
        new("MO-1", @"141 Shady Path", @"Cape Girardeau", @"MO", @"63701"),
        new("MS-1", @"PO Box 1144", @"Ocean Springs", @"MS", @"39566"),
        new("MS-2", @"676 Old Forge Rd.", @"Southaven", @"MS", null),
        new("NAT", @"310 E. Jefferson St.", @"Brooksville", @"FL", @"34601-2626"),
        new("NC-1", @"PO Box 48171", @"Cumberland", @"NC", @"28331"),
        new("NC-2", @"PO Box 33", @"Barium Springs", @"NC", @"28010"),
        new("NC-3", @"P.O. Box 1724", @"Indian Trail", @"NC", @"28079"),
        new("ND-1", @"PO Box 513", @"West Fargo", @"ND", @"58078"),
        new("NH-1", @"PO Box 359", @"Center Sandwich", @"NH", @"3227"),
        new("NH-2", @"21 Davis Blvd", @"Rochester", @"NH", @"3868"),
        new("NJ-1", @"21 Lomurno Lane", @"Cape May Court House", @"NJ", @"8210"),
        new("NM-1", @"7 Rio Vista Ave.", @"McIntosh", @"NM", @"87032"),
        new("NY-1", @"16 Thornton Commons", @"Yaphank", @"NY", @"11980"),
        new("NY-3", @"286 John Graham Rd", @"Smyrna", @"NY", @"13464"),
        new("NY-4", @"56 Dundee Cir", @"Middletown", @"NY", @"10941"),
        new("NY-5", @"35940 NYS Rte 26", @"Carthage", @"NY", @"13619"),
        new("NY-6", @"290 Burnett Rd", @"Webster", @"NY", @"14580"),
        new("NY-7", @"1351 Hwy 67", @"Johnstown", @"NY", @"12095"),
        new("OH-2", @"PO Box 514", @"Xenia", @"OH", @"45385"),
        new("OH-3", @"145 E. Ottawa St.", @"Oak Harbor", @"OH", @"43449"),
        new("OH-4", @"1792 Greentree Meadows Dr", @"Lebanon", @"OH", @"45036"),
        new("OH-6", @"29565 E. Broadway St", @"Walbridge", @"OH", @"43465"),
        new("OH-7", @"PO Box 134", @"Van Wert", @"OH", @"45891"),
        new("OH-8", @"135 N. Ohio ave. PO Box 715", @"Sidney", @"OH", @"45365"),
        new("OH-9", @"P.O. Box 154", @"Milford", @"OH", @"45150"),
        new("OK-2", @"906 SW 5th St", @"Lawton", @"OK", @"73505"),
        new("OK-3", @"P.O. Box 41 217 W. Cypress St.", @"Altus", @"OK", @"73521"),
        new("PA-1", @"114 Brewster Rd", @"New Castle", @"PA", @"16102"),
        new("PA-2", @"PO Box 33", @"Rouzerville", @"PA", @"17250"),
        new("PA-3", @"131 Riddgemont Dr", @"Industry", @"PA", @"15052"),
        new("PA-4", @"520 Mundis Mill Rd.", @"York", @"PA", @"17406"),
        new("PA-5", @"PO Box 132", @"Robesonia", @"PA", @"19551"),
        new("PA-7", @"PO Box 116", @"Jonestown", @"PA", @"17038"),
        new("PA-9", @"459 Maple", @"Blairsville", @"PA", @"15717"),
        new("SC-1", @"1726 Jameson Rd", @"Easley", @"SC", @"29640"),
        new("SC-2", @"108 Dusty Ct.", @"Lexington", @"SC", @"29073"),
        new("SC-3", @"81 Kendall Dr.", @"Bluffton", @"SC", @"29910"),
        new("SC-4", @"21 Plainfield Ave.", @"Goose Creek", null, null),
        new("SD-1", @"P.O. Box 1921", @"Watertown", @"SD", @"57201"),
        new("SD-2", @"118 S Main St.", @"Lennox", @"SD", @"57028"),
        new("SD-3", @"825 14th St", @"Sturgis", @"SD", @"57785"),
        new("TN-1", @"PO Box 681", @"Millington", @"TX", @"38083"),
        new("TN-2", @"6971 E. Richmond Shop Rd", @"Lebanon", @"TN", @"37090"),
        new("TN-3", @"PO Box 5703", @"Sevierville", @"TN", @"37864"),
        new("TN-4", @"18738 TN-56", @"Beersheba Springs", @"TN", @"37305"),
        new("TN-5", @"1477 Tiny Town Rd.", @"Clarksville", @"TN", @"37043"),
        new("TN-6", @"4458 Hurt Rd", @"Kenton", @"TN", @"38233"),
        new("TN-7", @"236 Jackson Ave.", @"Smyrna", @"TN", @"37167"),
        new("TN-8", @"PO Box 175", @"Chuckey", @"TN", @"37641"),
        new("TN-9", @"1240 Christianburg Ln.", @"Sweetwater", @"TN", @"37874"),
        new("TX-1", @"11113 Mulholland Dr.", @"Corpus Christi", @"TX", @"78410"),
        new("TX-2", @"615 E. Houston St #2511", @"San Antonio", @"TX", @"78205"),
        new("TX-3", @"5212 Meadow Ln", @"Krum", @"TX", @"76249"),
        new("TX-4", @"2302 Amethyst Dr", @"Killeen", @"TX", @"76549"),
        new("TX-5", @"427 Brooks Dr.", @"Nevada", @"TX", @"75173"),
        new("TX-6", @"1417 US 290 E, Lot 2", @"Elgin", @"TX", @"78361"),
        new("TX-7", @"12935 Taper Reach Dr", @"Tombull", @"TX", @"77377"),
        new("TX-8", @"2912 Greg St.", @"Canyon", @"TX", @"79015"),
        new("VA-1", @"PO Box 16114", @"Newport News", @"VA", @"23608"),
        new("VA-10", @"P.O. Box 12", @"Salem", @"VA", @"24153"),
        new("VA-2", @"PO Box 10131", @"Danville", @"VA", @"24543"),
        new("VA-3", @"4201 Eldorado Dr", @"Woodbridge", @"VA", @"22193"),
        new("VA-4", @"195 Choptank Rd", @"Stafford", @"VA", @"22556"),
        new("VA-5", @"610 Elmhurst Ave", @"Chesapeake", @"VA", @"23322"),
        new("VA-6", @"4936 Empire Pkwy", @"Chester", @"VA", @"23831"),
        new("VA-7", @"10561 Winged Elm Cr", @"Manassas", @"VA", @"20110"),
        new("VA-8", @"390 Ogden Ln", @"Middletown", @"VA", @"22645"),
        new("WA-1", @"3628 Arbors Dr. SE", @"Lacy", @"WA", @"98503"),
        new("WI-1", @"27308 Juniper Ln", @"Eastman", @"WI", @"54626"),
        new("WI-2", @"526 E North St", @"Poynette", @"WI", @"53944"),
        new("WVA-1", @"PO Box 4110", @"Huntington", @"WV", @"25729-4110"),
        new("WVA-2", @"302 General Early Dr.", @"Harpers Ferry", @"WV", @"25425"),
        new("WVA-3", @"2281 Dunlap Ridge Rd", @"Buffalo", @"WV", @"25033"),
        new("WVA-4", @"424 Winding Heights Rd", @"Parkersburg", @"WV", @"26101"),
        new("WVA-5", @"389 Tomahawk Run Rd", @"Hedgesville", @"WV", @"25427")
    ];

    public static async Task SeedAsync(ApplicationDbContext dbContext)
    {
        var addresses = MailingAddresses.ToDictionary(address => address.Abbreviation);
        await RenameLegacyEternalChapterAsync(dbContext);

        var nationalAddress = addresses["NAT"];
        var national = await GetOrCreateAsync(dbContext, "National", "NAT", OrganizationLevel.National, null, null, null, nationalAddress);

        var requiredStateAbbreviations = Chapters
            .Where(chapter => chapter.ParentAbbreviation != "NAT")
            .Select(chapter => chapter.ParentAbbreviation)
            .Distinct()
            .ToHashSet();

        foreach (var state in States.Where(state => requiredStateAbbreviations.Contains(state.Abbreviation)))
        {
            await GetOrCreateAsync(dbContext, state.Name, state.Abbreviation, OrganizationLevel.State, national.Id, null, StateCodeFromAbbreviation(state.Abbreviation), null);
        }

        var parents = await dbContext.OrganizationUnits.ToDictionaryAsync(unit => unit.Abbreviation);
        foreach (var chapter in Chapters)
        {
            var parentId = parents.TryGetValue(chapter.ParentAbbreviation, out var parent)
                ? parent.Id
                : national.Id;

            addresses.TryGetValue(chapter.Abbreviation, out var address);
            await GetOrCreateAsync(dbContext, chapter.Name, chapter.Abbreviation, OrganizationLevel.LocalChapter, parentId, chapter.City, chapter.StateCode, address);
        }

        await SoftCloseEmptyStatesAsync(dbContext, requiredStateAbbreviations);
        await SeedStateChapterAssignmentsAsync(dbContext);

        await dbContext.SaveChangesAsync();
    }

    private static async Task<OrganizationUnit> GetOrCreateAsync(
        ApplicationDbContext dbContext,
        string name,
        string abbreviation,
        OrganizationLevel level,
        Guid? parentId,
        string? city,
        string? stateCode,
        AddressSeed? mailingAddress)
    {
        var unit = await dbContext.OrganizationUnits.SingleOrDefaultAsync(existing => existing.Abbreviation == abbreviation);
        if (unit is not null)
        {
            ApplySeedValues(unit, name, level, parentId, city, stateCode, mailingAddress);
            await dbContext.SaveChangesAsync();
            return unit;
        }

        unit = new OrganizationUnit
        {
            Id = Guid.NewGuid(),
            Name = ToTitleCase(name),
            Abbreviation = abbreviation,
            Level = level,
            ParentOrganizationUnitId = parentId,
            City = city,
            StateCode = stateCode,
            Status = OrganizationStatus.Operating
        };
        ApplySeedValues(unit, name, level, parentId, city, stateCode, mailingAddress);

        dbContext.OrganizationUnits.Add(unit);
        await dbContext.SaveChangesAsync();
        return unit;
    }

    private static void ApplySeedValues(
        OrganizationUnit unit,
        string name,
        OrganizationLevel level,
        Guid? parentId,
        string? city,
        string? stateCode,
        AddressSeed? mailingAddress)
    {
        unit.Name = ToTitleCase(name);
        unit.Level = level;
        unit.ParentOrganizationUnitId = parentId;
        unit.City = OrganizationAdminService.IsEternalChapter(unit.Abbreviation) || level == OrganizationLevel.National ? null : city;
        unit.StateCode = OrganizationAdminService.IsEternalChapter(unit.Abbreviation) || level == OrganizationLevel.National ? null : stateCode;

        if (OrganizationAdminService.IsEternalChapter(unit.Abbreviation))
        {
            unit.MailingAddressLine1 = null;
            unit.MailingAddressLine2 = null;
            unit.MailingCity = null;
            unit.MailingStateCode = null;
            unit.MailingPostalCode = null;
            return;
        }

        if (mailingAddress is not null)
        {
            unit.MailingAddressLine1 = mailingAddress.AddressLine1;
            unit.MailingAddressLine2 = null;
            unit.MailingCity = mailingAddress.City;
            unit.MailingStateCode = mailingAddress.StateCode;
            unit.MailingPostalCode = mailingAddress.PostalCode;
        }
    }

    private static async Task RenameLegacyEternalChapterAsync(ApplicationDbContext dbContext)
    {
        var legacy = await dbContext.OrganizationUnits.SingleOrDefaultAsync(unit => unit.Abbreviation == "US-100");
        if (legacy is null)
        {
            return;
        }

        legacy.Abbreviation = "Chapter-100";
        legacy.City = null;
        legacy.StateCode = null;
        legacy.MailingAddressLine1 = null;
        legacy.MailingAddressLine2 = null;
        legacy.MailingCity = null;
        legacy.MailingStateCode = null;
        legacy.MailingPostalCode = null;
        await dbContext.SaveChangesAsync();
    }

    private static async Task SoftCloseEmptyStatesAsync(ApplicationDbContext dbContext, HashSet<string> requiredStateAbbreviations)
    {
        var states = await dbContext.OrganizationUnits
            .Where(unit => unit.Level == OrganizationLevel.State)
            .ToListAsync();

        foreach (var state in states.Where(state => !requiredStateAbbreviations.Contains(state.Abbreviation)))
        {
            var hasChildren = await dbContext.OrganizationUnits.AnyAsync(unit => unit.ParentOrganizationUnitId == state.Id);
            if (!hasChildren)
            {
                state.Status = OrganizationStatus.Closed;
            }
        }
    }

    private static async Task SeedStateChapterAssignmentsAsync(ApplicationDbContext dbContext)
    {
        var units = await dbContext.OrganizationUnits.ToDictionaryAsync(unit => unit.Abbreviation);
        foreach (var assignment in StateChapterAssignments)
        {
            if (!units.TryGetValue(assignment.StateAbbreviation, out var state) ||
                !units.TryGetValue(assignment.ChapterAbbreviation, out var chapter))
            {
                continue;
            }

            var hasCurrentAssignment = await dbContext.StateChapterAssignments
                .AnyAsync(existing => existing.StateOrganizationUnitId == state.Id && existing.EndsOn == null);

            if (hasCurrentAssignment)
            {
                continue;
            }

            dbContext.StateChapterAssignments.Add(new StateChapterAssignment
            {
                Id = Guid.NewGuid(),
                StateOrganizationUnitId = state.Id,
                LocalChapterOrganizationUnitId = chapter.Id,
                StartsOn = DateOnly.FromDateTime(DateTime.UtcNow),
                Notes = "Seeded from roster state designation.",
                ActorName = "OrganizationSeeder",
                ActorSource = "DevelopmentSeeder"
            });
        }
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

    private sealed record StateChapterSeed(string StateAbbreviation, string ChapterAbbreviation);

    private sealed record AddressSeed(string Abbreviation, string? AddressLine1, string? City, string? StateCode, string? PostalCode);

    private static string ToTitleCase(string value)
    {
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLowerInvariant());
    }
}

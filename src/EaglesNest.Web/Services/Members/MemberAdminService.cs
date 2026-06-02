using System.Globalization;
using System.Text.Json;
using EaglesNest.Core.Domain;
using EaglesNest.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace EaglesNest.Web.Services.Members;

public class MemberAdminService(ApplicationDbContext dbContext)
{
    private readonly SemaphoreSlim dbContextGate = new(1, 1);

    private static readonly MemberStatus[] RoadNameConflictStatuses =
    [
        MemberStatus.Prospect,
        MemberStatus.Probate,
        MemberStatus.PatchHolder,
        MemberStatus.Suspended
    ];

    public async Task<IReadOnlyList<MemberListItem>> GetMembersAsync(string? search, Guid? chapterId, MemberStatus? status)
    {
        return await UseDbContextAsync(async () =>
        {
            var query = dbContext.Members
                .AsNoTracking()
                .Include(member => member.PrimaryChapter)
                .AsQueryable();

            if (chapterId is not null)
            {
                query = query.Where(member => member.PrimaryChapterId == chapterId);
            }

            if (status is not null)
            {
                query = query.Where(member => member.Status == status);
            }

            var text = search?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                query = query.Where(member =>
                    member.FirstName.Contains(text) ||
                    member.LastName.Contains(text) ||
                    (member.MiddleName != null && member.MiddleName.Contains(text)) ||
                    (member.PreferredName != null && member.PreferredName.Contains(text)) ||
                    (member.RoadName != null && member.RoadName.Contains(text)) ||
                    (member.Email != null && member.Email.Contains(text)) ||
                    (member.PhoneNumber != null && member.PhoneNumber.Contains(text)) ||
                    member.PrimaryChapter.Name.Contains(text) ||
                    member.PrimaryChapter.Abbreviation.Contains(text));
            }

            var members = await query
                .OrderBy(member => member.PrimaryChapter.Abbreviation)
                .ThenBy(member => member.RoadName ?? member.LastName)
                .ThenBy(member => member.LastName)
                .ThenBy(member => member.FirstName)
                .Take(250)
                .ToListAsync();

            return members.Select(member => new MemberListItem
            {
                Id = member.Id,
                DisplayName = DisplayName(member),
                LegalName = LegalName(member),
                RoadName = member.RoadName,
                ChapterName = member.PrimaryChapter.Name,
                ChapterAbbreviation = member.PrimaryChapter.Abbreviation,
                Status = member.Status,
                Email = member.Email,
                PhoneNumber = member.PhoneNumber
            })
            .ToList();
        });
    }

    public async Task<MemberEditModel?> GetMemberAsync(Guid id)
    {
        return await UseDbContextAsync(async () => await dbContext.Members
            .AsNoTracking()
            .Include(member => member.MilitaryServiceRecords)
            .Where(member => member.Id == id)
            .Select(member => new MemberEditModel
            {
                Id = member.Id,
                ApplicationUserId = member.ApplicationUserId,
                FirstName = member.FirstName,
                MiddleName = member.MiddleName,
                LastName = member.LastName,
                Suffix = member.Suffix,
                PreferredName = member.PreferredName,
                RoadName = member.RoadName,
                Email = member.Email,
                PhoneNumber = member.PhoneNumber,
                AddressLine1 = member.AddressLine1,
                AddressLine2 = member.AddressLine2,
                City = member.City,
                State = member.State,
                PostalCode = member.PostalCode,
                DateOfBirth = member.DateOfBirth,
                BloodType = member.BloodType,
                Gender = member.Gender,
                LifetimeDate = member.LifetimeDate,
                Status = member.Status,
                PrimaryChapterId = member.PrimaryChapterId,
                StatusEffectiveDate = DateOnly.FromDateTime(DateTime.Today),
                Notes = member.Notes,
                MilitaryServiceRecords = member.MilitaryServiceRecords
                    .OrderBy(record => record.ServiceStartDate)
                    .Select(record => new MilitaryServiceEditModel
                    {
                        Id = record.Id,
                        Branch = record.Branch,
                        Rank = record.Rank,
                        ServiceStartDate = record.ServiceStartDate,
                        ServiceEndDate = record.ServiceEndDate,
                        DischargeType = record.DischargeType,
                        ConflictTab = record.ConflictTab,
                        ServiceNotes = record.ServiceNotes
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync());
    }

    public async Task<MemberProfileViewModel?> GetProfileForUserAsync(string applicationUserId)
    {
        return await UseDbContextAsync(async () =>
        {
            var member = await dbContext.Members
                .AsNoTracking()
                .Include(existing => existing.PrimaryChapter)
                .Include(existing => existing.MilitaryServiceRecords)
                .SingleOrDefaultAsync(existing => existing.ApplicationUserId == applicationUserId);

            return member is null ? null : ToProfile(member);
        });
    }

    public async Task<string?> GetMemberProfileLabelAsync(string applicationUserId)
    {
        return await UseDbContextAsync(async () =>
        {
            var member = await dbContext.Members
                .AsNoTracking()
                .Where(existing => existing.ApplicationUserId == applicationUserId)
                .Select(existing => new
                {
                    existing.FirstName,
                    existing.LastName,
                    existing.PreferredName,
                    existing.RoadName
                })
                .SingleOrDefaultAsync();

            return member is null
                ? null
                : DisplayName(member.FirstName, member.LastName, member.PreferredName, member.RoadName);
        });
    }

    public async Task<IReadOnlyList<MemberLinkedAccountOption>> GetLinkableAccountsAsync(string? selectedApplicationUserId)
    {
        return await UseDbContextAsync(async () => await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == selectedApplicationUserId ||
                           !dbContext.Members.Any(member => member.ApplicationUserId == user.Id))
            .OrderBy(user => user.UserName)
            .Select(user => new MemberLinkedAccountOption(user.Id, user.UserName ?? user.Id, user.Email))
            .ToListAsync());
    }

    public async Task<IReadOnlyList<ChapterOption>> GetChapterOptionsAsync()
    {
        return await UseDbContextAsync(async () =>
        {
            var chapters = await dbContext.OrganizationUnits
                .AsNoTracking()
                .Where(unit => unit.Level == OrganizationLevel.LocalChapter &&
                               unit.Status == OrganizationStatus.Operating)
                .Select(unit => new
                {
                    unit.Id,
                    unit.Abbreviation,
                    unit.Name,
                    StateName = unit.ParentOrganizationUnit == null ? null : unit.ParentOrganizationUnit.Name,
                    StateAbbreviation = unit.ParentOrganizationUnit == null ? null : unit.ParentOrganizationUnit.Abbreviation
                })
                .ToListAsync();

            return chapters
                .Select(chapter => new ChapterOption(
                    chapter.Id,
                    chapter.Abbreviation,
                    chapter.Name,
                    chapter.StateName,
                    chapter.StateAbbreviation,
                    ChapterSortGroup(chapter.Abbreviation, chapter.StateName),
                    ChapterSortName(chapter.Abbreviation, chapter.Name)))
                .OrderBy(chapter => chapter.SortGroup)
                .ThenBy(chapter => chapter.SortName)
                .ToList();
        });
    }

    public async Task<IReadOnlyList<MemberChapterAssignmentItem>> GetChapterAssignmentsAsync(Guid memberId)
    {
        return await UseDbContextAsync(async () => await dbContext.MemberChapterAssignments
            .AsNoTracking()
            .Where(assignment => assignment.MemberId == memberId)
            .OrderByDescending(assignment => assignment.StartDate)
            .Select(assignment => new MemberChapterAssignmentItem
            {
                Id = assignment.Id,
                ChapterName = assignment.Chapter.Name,
                ChapterAbbreviation = assignment.Chapter.Abbreviation,
                StartDate = assignment.StartDate,
                EndDate = assignment.EndDate,
                IsPrimary = assignment.IsPrimary
            })
            .ToListAsync());
    }

    public async Task<IReadOnlyList<MemberStatusHistoryItem>> GetStatusHistoryAsync(Guid memberId)
    {
        return await UseDbContextAsync(async () => await dbContext.MemberStatusHistory
            .AsNoTracking()
            .Where(history => history.MemberId == memberId)
            .OrderByDescending(history => history.EffectiveDate)
            .ThenByDescending(history => history.CreatedAt)
            .Select(history => new MemberStatusHistoryItem
            {
                Id = history.Id,
                Status = history.Status,
                EffectiveDate = history.EffectiveDate,
                Notes = history.Notes,
                ActorName = history.ActorName,
                ActorSource = history.ActorSource,
                CreatedAt = history.CreatedAt
            })
            .ToListAsync());
    }

    public async Task<IReadOnlyList<MemberAuditLogItem>> GetAuditLogsAsync(Guid memberId, int take = 50)
    {
        return await UseDbContextAsync(async () =>
        {
            var logs = await dbContext.AuditLogs
                .AsNoTracking()
                .Where(log => log.EntityName == nameof(Member) && log.EntityId == memberId.ToString())
                .OrderByDescending(log => log.CreatedAt)
                .ThenByDescending(log => log.Id)
                .Take(take)
                .Select(log => new
                {
                    log.Id,
                    log.CreatedAt,
                    log.Action,
                    log.ActorName,
                    log.ActorSource,
                    log.DetailsJson
                })
                .ToListAsync();

            var auditValueLabels = await BuildAuditValueLabelsAsync(logs.Select(log => log.DetailsJson));

            return logs.Select(log => new MemberAuditLogItem
            {
                Id = log.Id,
                CreatedAt = log.CreatedAt,
                Action = log.Action,
                ActorName = log.ActorName,
                ActorSource = log.ActorSource,
                Summary = BuildAuditSummary(log.Action, log.DetailsJson, auditValueLabels)
            })
            .ToList();
        });
    }

    public async Task<MemberSaveResult> CreateAsync(MemberEditModel input, MemberActor actor)
    {
        return await UseDbContextAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();
            Normalize(input);
            var validation = await ValidateAsync(input, null);
            if (validation.Count > 0)
            {
                return MemberSaveResult.Failure([.. validation]);
            }

            var primaryChapterId = input.Status == MemberStatus.Deceased
                ? await GetEternalChapterIdAsync()
                : input.PrimaryChapterId;

            if (primaryChapterId is null)
            {
                return MemberSaveResult.Failure("Eternal Chapter was not found.");
            }

            var member = new Member
            {
                Id = Guid.NewGuid(),
                ApplicationUserId = input.ApplicationUserId,
                FirstName = input.FirstName,
                MiddleName = input.MiddleName,
                LastName = input.LastName,
                Suffix = input.Suffix,
                PreferredName = input.PreferredName,
                RoadName = input.RoadName,
                Email = input.Email,
                PhoneNumber = input.PhoneNumber,
                AddressLine1 = input.AddressLine1,
                AddressLine2 = input.AddressLine2,
                City = input.City,
                State = input.State,
                PostalCode = input.PostalCode,
                DateOfBirth = input.DateOfBirth,
                BloodType = input.BloodType,
                Gender = input.Gender,
                LifetimeDate = input.LifetimeDate,
                Status = input.Status,
                PrimaryChapterId = primaryChapterId.Value,
                Notes = input.Notes
            };

            dbContext.Members.Add(member);
            dbContext.MemberChapterAssignments.Add(new MemberChapterAssignment
            {
                Id = Guid.NewGuid(),
                MemberId = member.Id,
                ChapterId = member.PrimaryChapterId,
                StartDate = input.ChapterEffectiveDate,
                IsPrimary = true
            });
            AddStatusHistory(member, input.StatusEffectiveDate!.Value, input.StatusNotes, actor);

            AddMilitaryServiceRecords(member, input.MilitaryServiceRecords);
            AddAudit(AuditAction.Created, member, actor, member.PrimaryChapterId, new { MemberId = member.Id, Name = DisplayName(member) });
            AddAudit(AuditAction.MemberStatusChanged, member, actor, member.PrimaryChapterId, new { member.Status, EffectiveDate = input.StatusEffectiveDate, input.StatusNotes });

            if (member.MilitaryServiceRecords.Count > 0)
            {
                AddAudit(AuditAction.MilitaryServiceAdded, member, actor, member.PrimaryChapterId, new { Count = member.MilitaryServiceRecords.Count });
            }

            await dbContext.SaveChangesAsync();
            return MemberSaveResult.Success(member.Id);
        });
    }

    public async Task<MemberSaveResult> UpdateAsync(Guid id, MemberEditModel input, MemberActor actor)
    {
        return await UseDbContextAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();
            Normalize(input);
            var member = await dbContext.Members
                .Include(existing => existing.MilitaryServiceRecords)
                .SingleOrDefaultAsync(existing => existing.Id == id);

            if (member is null)
            {
                return MemberSaveResult.Failure("Member was not found.");
            }

            var statusChanged = member.Status != input.Status;
            var validation = await ValidateAsync(input, id, statusChanged);
            if (validation.Count > 0)
            {
                return MemberSaveResult.Failure([.. validation]);
            }

            var originalChapterId = member.PrimaryChapterId;
            var targetChapterId = input.Status == MemberStatus.Deceased
                ? await GetEternalChapterIdAsync()
                : input.PrimaryChapterId!.Value;

            if (targetChapterId is null)
            {
                return MemberSaveResult.Failure("Eternal Chapter was not found.");
            }

            var auditValueLabels = await BuildAuditValueLabelsForRawValuesAsync(
            [
                member.ApplicationUserId,
                input.ApplicationUserId,
                member.PrimaryChapterId.ToString(),
                targetChapterId.Value.ToString()
            ]);
            var changes = BuildMemberChangeSet(member, input, targetChapterId.Value, auditValueLabels);
            ApplyMemberFields(member, input);
            member.PrimaryChapterId = targetChapterId.Value;
            member.UpdatedAt = DateTimeOffset.UtcNow;

            if (originalChapterId != member.PrimaryChapterId)
            {
                await TransferPrimaryChapterAsync(member, input.ChapterEffectiveDate, actor, originalChapterId);
            }

            if (statusChanged)
            {
                AddStatusHistory(member, input.StatusEffectiveDate!.Value, input.StatusNotes, actor);
                AddAudit(AuditAction.MemberStatusChanged, member, actor, member.PrimaryChapterId, new { member.Status, EffectiveDate = input.StatusEffectiveDate, input.StatusNotes });
            }

            var militaryChanges = ReplaceMilitaryServiceRecords(dbContext, member, input.MilitaryServiceRecords);

            if (changes.Count > 0)
            {
                AddAudit(AuditAction.MemberUpdated, member, actor, member.PrimaryChapterId, new { Changes = changes });
            }

            if (militaryChanges.Added > 0)
            {
                AddAudit(AuditAction.MilitaryServiceAdded, member, actor, member.PrimaryChapterId, new { militaryChanges.Added });
            }

            if (militaryChanges.Updated > 0)
            {
                AddAudit(AuditAction.MilitaryServiceUpdated, member, actor, member.PrimaryChapterId, new { militaryChanges.Updated });
            }

            if (militaryChanges.Removed > 0)
            {
                AddAudit(AuditAction.MilitaryServiceRemoved, member, actor, member.PrimaryChapterId, new { militaryChanges.Removed });
            }

            await dbContext.SaveChangesAsync();
            return MemberSaveResult.Success(member.Id);
        });
    }

    private async Task<List<string>> ValidateAsync(MemberEditModel input, Guid? existingId, bool statusChanged = true)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(input.FirstName))
        {
            errors.Add("First name is required.");
        }

        if (string.IsNullOrWhiteSpace(input.LastName))
        {
            errors.Add("Last name is required.");
        }

        if (input.PrimaryChapterId is null)
        {
            errors.Add("Primary chapter is required.");
        }
        else
        {
            var chapter = await dbContext.OrganizationUnits
                .AsNoTracking()
                .SingleOrDefaultAsync(unit => unit.Id == input.PrimaryChapterId);

            if (chapter is null || chapter.Level != OrganizationLevel.LocalChapter)
            {
                errors.Add("Primary chapter must be a local chapter.");
            }
        }

        if (!string.IsNullOrWhiteSpace(input.ApplicationUserId))
        {
            var userExists = await dbContext.Users.AnyAsync(user => user.Id == input.ApplicationUserId);
            if (!userExists)
            {
                errors.Add("Linked login account was not found.");
            }

            var userAlreadyLinked = await dbContext.Members.AnyAsync(member =>
                member.ApplicationUserId == input.ApplicationUserId && member.Id != existingId);
            if (userAlreadyLinked)
            {
                errors.Add("Linked login account is already assigned to another member.");
            }
        }

        var targetChapterId = input.Status == MemberStatus.Deceased
            ? await GetEternalChapterIdAsync()
            : input.PrimaryChapterId;

        if (input.Status == MemberStatus.Deceased && targetChapterId is null)
        {
            errors.Add("Eternal Chapter was not found.");
        }

        if (statusChanged && input.StatusEffectiveDate is null)
        {
            errors.Add("Status effective date is required when creating a member or changing status.");
        }

        ValidateOption(errors, input.BloodType, MemberOptionValues.BloodTypes, "Blood type");
        ValidateOption(errors, input.Gender, MemberOptionValues.Genders, "Gender");

        if (!string.IsNullOrWhiteSpace(input.RoadName) && targetChapterId is not null)
        {
            var roadNameConflict = await dbContext.Members.AnyAsync(member =>
                member.Id != existingId &&
                member.PrimaryChapterId == targetChapterId &&
                member.RoadName == input.RoadName &&
                RoadNameConflictStatuses.Contains(member.Status));

            if (roadNameConflict)
            {
                errors.Add("Road name must be unique within the active primary chapter.");
            }
        }

        foreach (var serviceRecord in input.MilitaryServiceRecords)
        {
            if (string.IsNullOrWhiteSpace(serviceRecord.Branch))
            {
                errors.Add("Military service branch is required when adding a service record.");
                break;
            }

            ValidateOption(errors, serviceRecord.Branch, MemberOptionValues.Branches, "Military service branch");
            ValidateOption(errors, serviceRecord.DischargeType, MemberOptionValues.DischargeTypes, "Discharge type");
            ValidateOption(errors, serviceRecord.ConflictTab, MemberOptionValues.ConflictTabs, "Conflict tab");
        }

        return errors;
    }

    private static void ValidateOption(List<string> errors, string? value, IReadOnlyCollection<string> allowedValues, string label)
    {
        if (!string.IsNullOrWhiteSpace(value) && !allowedValues.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"{label} must be selected from the allowed list.");
        }
    }

    private async Task TransferPrimaryChapterAsync(Member member, DateOnly effectiveDate, MemberActor actor, Guid originalChapterId)
    {
        var activeAssignments = await dbContext.MemberChapterAssignments
            .Where(assignment => assignment.MemberId == member.Id &&
                                 assignment.IsPrimary &&
                                 assignment.EndDate == null)
            .ToListAsync();

        foreach (var assignment in activeAssignments)
        {
            assignment.EndDate = effectiveDate.AddDays(-1);
        }

        dbContext.MemberChapterAssignments.Add(new MemberChapterAssignment
        {
            Id = Guid.NewGuid(),
            MemberId = member.Id,
            ChapterId = member.PrimaryChapterId,
            StartDate = effectiveDate,
            IsPrimary = true
        });

        AddAudit(AuditAction.MemberChapterTransferred, member, actor, member.PrimaryChapterId, new
        {
            FromChapterId = originalChapterId,
            ToChapterId = member.PrimaryChapterId,
            EffectiveDate = effectiveDate,
            member.Status
        });
    }

    private async Task<Guid?> GetEternalChapterIdAsync()
    {
        return await dbContext.OrganizationUnits
            .Where(unit => unit.Level == OrganizationLevel.LocalChapter &&
                           unit.Abbreviation == "Chapter-100")
            .Select(unit => (Guid?)unit.Id)
            .SingleOrDefaultAsync();
    }

    private static void ApplyMemberFields(Member member, MemberEditModel input)
    {
        member.ApplicationUserId = input.ApplicationUserId;
        member.FirstName = input.FirstName;
        member.MiddleName = input.MiddleName;
        member.LastName = input.LastName;
        member.Suffix = input.Suffix;
        member.PreferredName = input.PreferredName;
        member.RoadName = input.RoadName;
        member.Email = input.Email;
        member.PhoneNumber = input.PhoneNumber;
        member.AddressLine1 = input.AddressLine1;
        member.AddressLine2 = input.AddressLine2;
        member.City = input.City;
        member.State = input.State;
        member.PostalCode = input.PostalCode;
        member.DateOfBirth = input.DateOfBirth;
        member.BloodType = input.BloodType;
        member.Gender = input.Gender;
        member.LifetimeDate = input.LifetimeDate;
        member.Status = input.Status;
        member.Notes = input.Notes;
    }

    private static MilitaryServiceChangeCount ReplaceMilitaryServiceRecords(
        ApplicationDbContext dbContext,
        Member member,
        List<MilitaryServiceEditModel> serviceRecords)
    {
        var existingById = member.MilitaryServiceRecords.ToDictionary(record => record.Id);
        var inputIds = serviceRecords.Where(record => record.Id is not null).Select(record => record.Id!.Value).ToHashSet();
        var recordsToRemove = member.MilitaryServiceRecords.Where(record => !inputIds.Contains(record.Id)).ToList();
        var removed = recordsToRemove.Count;
        var updated = 0;
        var added = 0;

        foreach (var record in recordsToRemove)
        {
            member.MilitaryServiceRecords.Remove(record);
        }

        dbContext.MilitaryServiceRecords.RemoveRange(recordsToRemove);

        foreach (var serviceRecord in serviceRecords)
        {
            if (serviceRecord.Id is null)
            {
                added++;
                dbContext.MilitaryServiceRecords.Add(new MilitaryServiceRecord
                {
                    Id = Guid.NewGuid(),
                    MemberId = member.Id,
                    Branch = serviceRecord.Branch,
                    Rank = serviceRecord.Rank,
                    ServiceStartDate = serviceRecord.ServiceStartDate,
                    ServiceEndDate = serviceRecord.ServiceEndDate,
                    DischargeType = serviceRecord.DischargeType,
                    ConflictTab = serviceRecord.ConflictTab,
                    ServiceNotes = serviceRecord.ServiceNotes
                });
                continue;
            }

            if (!existingById.TryGetValue(serviceRecord.Id.Value, out var existing))
            {
                added++;
                dbContext.MilitaryServiceRecords.Add(new MilitaryServiceRecord
                {
                    Id = Guid.NewGuid(),
                    MemberId = member.Id,
                    Branch = serviceRecord.Branch,
                    Rank = serviceRecord.Rank,
                    ServiceStartDate = serviceRecord.ServiceStartDate,
                    ServiceEndDate = serviceRecord.ServiceEndDate,
                    DischargeType = serviceRecord.DischargeType,
                    ConflictTab = serviceRecord.ConflictTab,
                    ServiceNotes = serviceRecord.ServiceNotes
                });
                continue;
            }

            if (HasMilitaryServiceChanges(existing, serviceRecord))
            {
                updated++;
                existing.Branch = serviceRecord.Branch;
                existing.Rank = serviceRecord.Rank;
                existing.ServiceStartDate = serviceRecord.ServiceStartDate;
                existing.ServiceEndDate = serviceRecord.ServiceEndDate;
                existing.DischargeType = serviceRecord.DischargeType;
                existing.ConflictTab = serviceRecord.ConflictTab;
                existing.ServiceNotes = serviceRecord.ServiceNotes;
            }
        }

        return new MilitaryServiceChangeCount(added, updated, removed);
    }

    private static void AddMilitaryServiceRecords(Member member, IEnumerable<MilitaryServiceEditModel> serviceRecords)
    {
        foreach (var serviceRecord in serviceRecords)
        {
            member.MilitaryServiceRecords.Add(new MilitaryServiceRecord
            {
                Id = Guid.NewGuid(),
                MemberId = member.Id,
                Branch = serviceRecord.Branch,
                Rank = serviceRecord.Rank,
                ServiceStartDate = serviceRecord.ServiceStartDate,
                ServiceEndDate = serviceRecord.ServiceEndDate,
                DischargeType = serviceRecord.DischargeType,
                ConflictTab = serviceRecord.ConflictTab,
                ServiceNotes = serviceRecord.ServiceNotes
            });
        }
    }

    private static bool HasMilitaryServiceChanges(MilitaryServiceRecord existing, MilitaryServiceEditModel input)
    {
        return existing.Branch != input.Branch ||
               existing.Rank != input.Rank ||
               existing.ServiceStartDate != input.ServiceStartDate ||
               existing.ServiceEndDate != input.ServiceEndDate ||
               existing.DischargeType != input.DischargeType ||
               existing.ConflictTab != input.ConflictTab ||
               existing.ServiceNotes != input.ServiceNotes;
    }

    private void AddStatusHistory(Member member, DateOnly effectiveDate, string? notes, MemberActor actor)
    {
        dbContext.MemberStatusHistory.Add(new MemberStatusHistory
        {
            Id = Guid.NewGuid(),
            MemberId = member.Id,
            Status = member.Status,
            EffectiveDate = effectiveDate,
            Notes = notes,
            CreatedByUserId = actor.ApplicationUserId,
            ActorName = actor.ActorName,
            ActorSource = actor.ActorSource
        });
    }

    private static List<AuditFieldChange> BuildMemberChangeSet(
        Member member,
        MemberEditModel input,
        Guid targetChapterId,
        IReadOnlyDictionary<string, string> auditValueLabels)
    {
        var changes = new List<AuditFieldChange>();
        AddChange(changes, "Linked Login", ResolveAuditValue(member.ApplicationUserId, auditValueLabels), ResolveAuditValue(input.ApplicationUserId, auditValueLabels));
        AddChange(changes, "First Name", member.FirstName, input.FirstName);
        AddChange(changes, "Middle Name", member.MiddleName, input.MiddleName);
        AddChange(changes, "Last Name", member.LastName, input.LastName);
        AddChange(changes, "Suffix", member.Suffix, input.Suffix);
        AddChange(changes, "Preferred Name", member.PreferredName, input.PreferredName);
        AddChange(changes, "Road Name", member.RoadName, input.RoadName);
        AddChange(changes, "Email", member.Email, input.Email);
        AddChange(changes, "Phone", member.PhoneNumber, input.PhoneNumber);
        AddChange(changes, "Address Line 1", member.AddressLine1, input.AddressLine1);
        AddChange(changes, "Address Line 2", member.AddressLine2, input.AddressLine2);
        AddChange(changes, "City", member.City, input.City);
        AddChange(changes, "State", member.State, input.State);
        AddChange(changes, "ZIP Code", member.PostalCode, input.PostalCode);
        AddChange(changes, "Date of Birth", FormatAuditValue(member.DateOfBirth), FormatAuditValue(input.DateOfBirth));
        AddChange(changes, "Blood Type", member.BloodType, input.BloodType);
        AddChange(changes, "Gender", member.Gender, input.Gender);
        AddChange(changes, "Lifetime Date", FormatAuditValue(member.LifetimeDate), FormatAuditValue(input.LifetimeDate));
        AddChange(changes, "Status", MemberOptionValues.DisplayStatus(member.Status), MemberOptionValues.DisplayStatus(input.Status));
        AddChange(changes, "Primary Chapter", ResolveAuditValue(member.PrimaryChapterId.ToString(), auditValueLabels), ResolveAuditValue(targetChapterId.ToString(), auditValueLabels));
        AddChange(changes, "Notes", member.Notes, input.Notes);
        return changes;
    }

    private static void AddChange(List<AuditFieldChange> changes, string field, string? oldValue, string? newValue)
    {
        if (!string.Equals(oldValue, newValue, StringComparison.Ordinal))
        {
            changes.Add(new AuditFieldChange(field, oldValue, newValue));
        }
    }

    private static void Normalize(MemberEditModel input)
    {
        input.ApplicationUserId = NormalizeOptional(input.ApplicationUserId);
        input.FirstName = ToTitleCase(input.FirstName.Trim());
        input.MiddleName = NormalizeName(input.MiddleName);
        input.LastName = ToTitleCase(input.LastName.Trim());
        input.Suffix = NormalizeOptional(input.Suffix);
        input.PreferredName = NormalizeName(input.PreferredName);
        input.RoadName = NormalizeOptional(input.RoadName);
        input.Email = NormalizeOptional(input.Email);
        input.PhoneNumber = NormalizeOptional(input.PhoneNumber);
        input.AddressLine1 = NormalizeOptional(input.AddressLine1);
        input.AddressLine2 = NormalizeOptional(input.AddressLine2);
        input.City = NormalizeName(input.City);
        input.State = NormalizeOptional(input.State)?.ToUpperInvariant();
        input.PostalCode = NormalizeOptional(input.PostalCode);
        input.BloodType = NormalizeOption(input.BloodType, MemberOptionValues.BloodTypes);
        input.Gender = NormalizeOption(input.Gender, MemberOptionValues.Genders);
        input.StatusNotes = NormalizeOptional(input.StatusNotes);
        input.Notes = NormalizeOptional(input.Notes);

        foreach (var serviceRecord in input.MilitaryServiceRecords)
        {
            serviceRecord.Branch = NormalizeOption(serviceRecord.Branch, MemberOptionValues.Branches) ?? string.Empty;
            serviceRecord.Rank = NormalizeOptional(serviceRecord.Rank);
            serviceRecord.DischargeType = NormalizeOption(serviceRecord.DischargeType, MemberOptionValues.DischargeTypes);
            serviceRecord.ConflictTab = NormalizeOption(serviceRecord.ConflictTab, MemberOptionValues.ConflictTabs);
            serviceRecord.ServiceNotes = NormalizeOptional(serviceRecord.ServiceNotes);
        }
    }

    private static string? NormalizeOption(string? value, IReadOnlyCollection<string> allowedValues)
    {
        var normalized = NormalizeOptional(value);
        return normalized is null
            ? null
            : allowedValues.FirstOrDefault(option => string.Equals(option, normalized, StringComparison.OrdinalIgnoreCase)) ?? normalized;
    }

    private static string? NormalizeName(string? value)
    {
        var normalized = NormalizeOptional(value);
        return normalized is null ? null : ToTitleCase(normalized);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string ToTitleCase(string value)
    {
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLowerInvariant());
    }

    private static MemberProfileViewModel ToProfile(Member member)
    {
        return new MemberProfileViewModel
        {
            Id = member.Id,
            DisplayName = DisplayName(member),
            LegalName = LegalName(member),
            RoadName = member.RoadName,
            PreferredName = member.PreferredName,
            Email = member.Email,
            PhoneNumber = member.PhoneNumber,
            AddressLine1 = member.AddressLine1,
            AddressLine2 = member.AddressLine2,
            City = member.City,
            State = member.State,
            PostalCode = member.PostalCode,
            DateOfBirth = member.DateOfBirth,
            BloodType = member.BloodType,
            Gender = member.Gender,
            LifetimeDate = member.LifetimeDate,
            Status = member.Status,
            ChapterName = member.PrimaryChapter.Name,
            ChapterAbbreviation = member.PrimaryChapter.Abbreviation,
            MilitaryServiceRecords = member.MilitaryServiceRecords
                .OrderBy(record => record.ServiceStartDate)
                .Select(record => new MilitaryServiceEditModel
                {
                    Id = record.Id,
                    Branch = record.Branch,
                    Rank = record.Rank,
                    ServiceStartDate = record.ServiceStartDate,
                    ServiceEndDate = record.ServiceEndDate,
                    DischargeType = record.DischargeType,
                    ConflictTab = record.ConflictTab,
                    ServiceNotes = record.ServiceNotes
                })
                .ToList()
        };
    }

    public static string DisplayName(Member member)
    {
        return DisplayName(member.FirstName, member.LastName, member.PreferredName, member.RoadName);
    }

    public static string DisplayName(string firstName, string lastName, string? preferredName, string? roadName)
    {
        if (!string.IsNullOrWhiteSpace(roadName))
        {
            return roadName;
        }

        if (!string.IsNullOrWhiteSpace(preferredName))
        {
            return preferredName;
        }

        return $"{firstName} {lastName}".Trim();
    }

    public static string LegalName(Member member)
    {
        return string.Join(" ", new[] { member.FirstName, member.MiddleName, member.LastName, member.Suffix }
            .Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static string ChapterSortGroup(string abbreviation, string? stateName)
    {
        return IsEternalChapter(abbreviation)
            ? "0000"
            : $"1000-{stateName ?? string.Empty}";
    }

    private static string ChapterSortName(string abbreviation, string name)
    {
        return IsEternalChapter(abbreviation) ? "0000" : name;
    }

    private static bool IsEternalChapter(string? abbreviation)
    {
        return string.Equals(abbreviation, "Chapter-100", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(abbreviation, "US-100", StringComparison.OrdinalIgnoreCase);
    }

    private static string? FormatAuditValue(DateOnly? value)
    {
        return value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private async Task<IReadOnlyDictionary<string, string>> BuildAuditValueLabelsAsync(IEnumerable<string?> detailsJsons)
    {
        var rawValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var detailsJson in detailsJsons)
        {
            if (string.IsNullOrWhiteSpace(detailsJson))
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(detailsJson);
                if (!document.RootElement.TryGetProperty("Changes", out var changes) ||
                    changes.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var change in changes.EnumerateArray())
                {
                    AddRawAuditValue(rawValues, GetJsonString(change, "From"));
                    AddRawAuditValue(rawValues, GetJsonString(change, "To"));
                }
            }
            catch (JsonException)
            {
                continue;
            }
        }

        return await BuildAuditValueLabelsForRawValuesAsync(rawValues);
    }

    private async Task<IReadOnlyDictionary<string, string>> BuildAuditValueLabelsForRawValuesAsync(IEnumerable<string?> rawValues)
    {
        var values = rawValues
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (values.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var userIds = values;
        if (userIds.Count > 0)
        {
            var users = await dbContext.Users
                .AsNoTracking()
                .Where(user => userIds.Contains(user.Id))
                .Select(user => new { user.Id, user.UserName, user.Email })
                .ToListAsync();

            foreach (var user in users)
            {
                labels[user.Id] = user.UserName ?? user.Email ?? user.Id;
            }
        }

        var chapterIds = values
            .Select(value => Guid.TryParse(value, out var id) ? id : (Guid?)null)
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        if (chapterIds.Count > 0)
        {
            var chapters = await dbContext.OrganizationUnits
                .AsNoTracking()
                .Where(chapter => chapterIds.Contains(chapter.Id))
                .Select(chapter => new { chapter.Id, chapter.Abbreviation })
                .ToListAsync();

            foreach (var chapter in chapters)
            {
                labels[chapter.Id.ToString()] = chapter.Abbreviation;
            }
        }

        return labels;
    }

    private static void AddRawAuditValue(HashSet<string> rawValues, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            rawValues.Add(value);
        }
    }

    private static string? ResolveAuditValue(string? value, IReadOnlyDictionary<string, string> auditValueLabels)
    {
        return value is not null && auditValueLabels.TryGetValue(value, out var label) ? label : value;
    }

    private static string BuildAuditSummary(AuditAction action, string? detailsJson, IReadOnlyDictionary<string, string> auditValueLabels)
    {
        return action switch
        {
            AuditAction.Created => "Member created.",
            AuditAction.MemberUpdated => BuildUpdatedSummary(detailsJson, auditValueLabels),
            AuditAction.MemberChapterTransferred => "Chapter assignment changed.",
            AuditAction.MilitaryServiceAdded => "Military service record added.",
            AuditAction.MilitaryServiceUpdated => "Military service record updated.",
            AuditAction.MilitaryServiceRemoved => "Military service record removed.",
            AuditAction.MemberStatusChanged => BuildStatusChangedSummary(detailsJson),
            _ => action.ToString()
        };
    }

    private static string BuildStatusChangedSummary(string? detailsJson)
    {
        if (string.IsNullOrWhiteSpace(detailsJson))
        {
            return "Member status changed.";
        }

        try
        {
            using var document = JsonDocument.Parse(detailsJson);
            var status = GetJsonString(document.RootElement, "Status");
            var effectiveDate = GetJsonString(document.RootElement, "EffectiveDate");
            var displayStatus = Enum.TryParse<MemberStatus>(status, out var parsedStatus)
                ? MemberOptionValues.DisplayStatus(parsedStatus)
                : status;

            return string.IsNullOrWhiteSpace(effectiveDate)
                ? $"Status changed to {displayStatus}."
                : $"Status changed to {displayStatus} effective {effectiveDate}.";
        }
        catch (JsonException)
        {
            return "Member status changed.";
        }
    }

    private static string BuildUpdatedSummary(string? detailsJson, IReadOnlyDictionary<string, string> auditValueLabels)
    {
        if (string.IsNullOrWhiteSpace(detailsJson))
        {
            return "Member details updated.";
        }

        try
        {
            using var document = JsonDocument.Parse(detailsJson);
            if (!document.RootElement.TryGetProperty("Changes", out var changes) ||
                changes.ValueKind != JsonValueKind.Array)
            {
                return "Member details updated.";
            }

            var summaries = changes.EnumerateArray()
                .Select(change =>
                {
                    var field = GetJsonString(change, "Field");
                    return string.IsNullOrWhiteSpace(field)
                        ? null
                        : $"{field} changed from {DisplayAuditValue(GetJsonString(change, "From"), auditValueLabels)} to {DisplayAuditValue(GetJsonString(change, "To"), auditValueLabels)}";
                })
                .Where(summary => !string.IsNullOrWhiteSpace(summary))
                .Take(3)
                .ToList();

            return summaries.Count == 0 ? "Member details updated." : string.Join("; ", summaries) + ".";
        }
        catch (JsonException)
        {
            return "Member details updated.";
        }
    }

    private static string? GetJsonString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null
            ? property.ToString()
            : null;
    }

    private static string DisplayAuditValue(string? value, IReadOnlyDictionary<string, string> auditValueLabels)
    {
        var displayValue = ResolveAuditValue(value, auditValueLabels);
        return string.IsNullOrWhiteSpace(displayValue) ? "(blank)" : $"\"{displayValue}\"";
    }

    private void AddAudit(AuditAction action, Member member, MemberActor actor, Guid? organizationUnitId, object details)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            ApplicationUserId = actor.ApplicationUserId,
            ActorName = actor.ActorName,
            ActorSource = actor.ActorSource,
            OrganizationUnitId = organizationUnitId,
            Action = action,
            EntityName = nameof(Member),
            EntityId = member.Id.ToString(),
            DetailsJson = JsonSerializer.Serialize(details)
        });
    }

    private async Task<T> UseDbContextAsync<T>(Func<Task<T>> operation)
    {
        await dbContextGate.WaitAsync();
        try
        {
            return await operation();
        }
        finally
        {
            dbContextGate.Release();
        }
    }

    private sealed record AuditFieldChange(string Field, string? From, string? To);
    private sealed record MilitaryServiceChangeCount(int Added, int Updated, int Removed);
}

public sealed record ChapterOption(
    Guid Id,
    string Abbreviation,
    string Name,
    string? StateName,
    string? StateAbbreviation,
    string SortGroup,
    string SortName);

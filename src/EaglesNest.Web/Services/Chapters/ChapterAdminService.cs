using System.Text.Json;
using System.Globalization;
using EaglesNest.Core.Domain;
using EaglesNest.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace EaglesNest.Web.Services.Chapters;

public class ChapterAdminService(ApplicationDbContext dbContext)
{
    private static readonly AuditAction[] ChapterAuditDisplayActions =
    [
        AuditAction.Created,
        AuditAction.Updated,
        AuditAction.Closed,
        AuditAction.Reopened,
        AuditAction.MemberAdded,
        AuditAction.MemberRemoved,
        AuditAction.MemberUpdated
    ];

    public async Task<IReadOnlyList<ChapterTreeItem>> GetHierarchyAsync(bool includeUnavailable = false)
    {
        var chapters = await dbContext.OrganizationUnits
            .AsNoTracking()
            .Where(unit => includeUnavailable || unit.Status == OrganizationStatus.Operating)
            .OrderBy(unit => unit.Level)
            .ThenBy(unit => unit.Abbreviation)
            .Select(unit => new ChapterTreeItem
            {
                Id = unit.Id,
                ParentOrganizationUnitId = unit.ParentOrganizationUnitId,
                Level = unit.Level,
                Name = unit.Name,
                Abbreviation = unit.Abbreviation,
                City = unit.City,
                StateCode = unit.StateCode,
                MailingCity = unit.MailingCity,
                MailingStateCode = unit.MailingStateCode,
                Status = unit.Status
            })
            .ToListAsync();

        var currentAssignments = await dbContext.StateChapterAssignments
            .AsNoTracking()
            .Where(assignment => assignment.EndsOn == null)
            .Select(assignment => new
            {
                assignment.StateOrganizationUnitId,
                assignment.LocalChapterOrganizationUnitId,
                assignment.LocalChapterOrganizationUnit.Abbreviation,
                assignment.LocalChapterOrganizationUnit.Name
            })
            .ToDictionaryAsync(assignment => assignment.StateOrganizationUnitId);

        foreach (var chapter in chapters.Where(item => item.Level == OrganizationLevel.State))
        {
            if (currentAssignments.TryGetValue(chapter.Id, out var assignment))
            {
                chapter.ActingStateChapterId = assignment.LocalChapterOrganizationUnitId;
                chapter.ActingStateChapterAbbreviation = assignment.Abbreviation;
                chapter.ActingStateChapterName = assignment.Name;
            }
        }

        var byParent = chapters
            .Where(unit => unit.ParentOrganizationUnitId is not null)
            .GroupBy(unit => unit.ParentOrganizationUnitId!.Value)
            .ToDictionary(group => group.Key, group => group.OrderBy(ChapterSortKey).ThenBy(unit => unit.Name).ToList());

        foreach (var chapter in chapters)
        {
            if (byParent.TryGetValue(chapter.Id, out var children))
            {
                chapter.Children = children;
            }
        }

        return chapters
            .Where(unit => unit.ParentOrganizationUnitId is null)
            .OrderBy(ChapterSortKey)
            .ThenBy(unit => unit.Name)
            .ToList();
    }

    public async Task<IReadOnlyList<ChapterTreeItem>> GetLocalChapterOptionsAsync(Guid stateOrganizationUnitId)
    {
        return await dbContext.OrganizationUnits
            .AsNoTracking()
            .Where(unit => unit.ParentOrganizationUnitId == stateOrganizationUnitId &&
                           unit.Level == OrganizationLevel.LocalChapter &&
                           unit.Status == OrganizationStatus.Operating)
            .OrderBy(unit => unit.Abbreviation)
            .Select(unit => new ChapterTreeItem
            {
                Id = unit.Id,
                ParentOrganizationUnitId = unit.ParentOrganizationUnitId,
                Level = unit.Level,
                Name = unit.Name,
                Abbreviation = unit.Abbreviation,
                Status = unit.Status
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyList<StateChapterAssignmentModel>> GetStateChapterAssignmentsAsync(Guid stateOrganizationUnitId)
    {
        return await dbContext.StateChapterAssignments
            .AsNoTracking()
            .Where(assignment => assignment.StateOrganizationUnitId == stateOrganizationUnitId)
            .OrderByDescending(assignment => assignment.StartsOn)
            .Select(assignment => new StateChapterAssignmentModel
            {
                Id = assignment.Id,
                StateOrganizationUnitId = assignment.StateOrganizationUnitId,
                LocalChapterOrganizationUnitId = assignment.LocalChapterOrganizationUnitId,
                LocalChapterName = assignment.LocalChapterOrganizationUnit.Name,
                LocalChapterAbbreviation = assignment.LocalChapterOrganizationUnit.Abbreviation,
                StartsOn = assignment.StartsOn,
                EndsOn = assignment.EndsOn,
                Notes = assignment.Notes
            })
            .ToListAsync();
    }

    public async Task<ChapterSuspension?> GetCurrentSuspensionAsync(Guid organizationUnitId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await dbContext.ChapterSuspensions
            .AsNoTracking()
            .Where(suspension => suspension.OrganizationUnitId == organizationUnitId &&
                                 (suspension.EndsOn == null || suspension.EndsOn >= today))
            .OrderByDescending(suspension => suspension.StartsOn)
            .FirstOrDefaultAsync();
    }

    public async Task<ChapterEditModel?> GetChapterAsync(Guid id)
    {
        return await dbContext.OrganizationUnits
            .AsNoTracking()
            .Where(unit => unit.Id == id)
            .Select(unit => new ChapterEditModel
            {
                Id = unit.Id,
                Name = unit.Name,
                Abbreviation = unit.Abbreviation,
                Level = unit.Level,
                ParentOrganizationUnitId = unit.ParentOrganizationUnitId,
                Status = unit.Status,
                City = unit.City,
                StateCode = unit.StateCode,
                MailingAddressLine1 = unit.MailingAddressLine1,
                MailingAddressLine2 = unit.MailingAddressLine2,
                MailingCity = unit.MailingCity,
                MailingStateCode = unit.MailingStateCode,
                MailingPostalCode = unit.MailingPostalCode,
                CharterDate = unit.CharterDate
            })
            .SingleOrDefaultAsync();
    }

    public async Task<IReadOnlyList<ChapterAuditLogItem>> GetChapterAuditLogsAsync(Guid organizationUnitId, int take = 25)
    {
        var logs = await dbContext.AuditLogs
            .AsNoTracking()
            .Where(log => log.OrganizationUnitId == organizationUnitId &&
                          ChapterAuditDisplayActions.Contains(log.Action))
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

        return logs.Select(log => new ChapterAuditLogItem
            {
                Id = log.Id,
                CreatedAt = log.CreatedAt,
                Action = log.Action,
                ActorName = log.ActorName,
                ActorSource = log.ActorSource,
                Summary = BuildAuditSummary(log.Action, log.DetailsJson)
            })
            .ToList();
    }

    public async Task<ChapterSaveResult> CreateAsync(ChapterEditModel input, ChapterActor actor)
    {
        Normalize(input);
        input.Status = OrganizationStatus.Operating;

        var validation = await ValidateAsync(input, null);
        if (validation.Count > 0)
        {
            return ChapterSaveResult.Failure([.. validation]);
        }

        var organization = new OrganizationUnit
        {
            Id = Guid.NewGuid(),
            Name = input.Name,
            Abbreviation = input.Abbreviation,
            Level = input.Level,
            ParentOrganizationUnitId = input.ParentOrganizationUnitId,
            Status = OrganizationStatus.Operating,
            City = input.City,
            StateCode = input.StateCode,
            MailingAddressLine1 = input.MailingAddressLine1,
            MailingAddressLine2 = input.MailingAddressLine2,
            MailingCity = input.MailingCity,
            MailingStateCode = input.MailingStateCode,
            MailingPostalCode = input.MailingPostalCode,
            CharterDate = input.CharterDate
        };

        dbContext.OrganizationUnits.Add(organization);
        AddAudit(AuditAction.Created, organization, actor, input);

        if (organization.Level == OrganizationLevel.LocalChapter &&
            organization.ParentOrganizationUnitId is Guid stateId)
        {
            var parentState = await dbContext.OrganizationUnits
                .SingleOrDefaultAsync(unit => unit.Id == stateId && unit.Level == OrganizationLevel.State);

            if (parentState?.Status == OrganizationStatus.Closed)
            {
                parentState.Status = OrganizationStatus.Operating;
                AddAudit(AuditAction.Reopened, parentState, actor, new
                {
                    parentState.Id,
                    parentState.Status,
                    Reason = "Reopened automatically because a local chapter was added.",
                    LocalChapterId = organization.Id,
                    organization.Abbreviation
                });

                await AddStateChapterAssignmentAsync(
                    parentState,
                    organization,
                    DateOnly.FromDateTime(DateTime.UtcNow),
                    "Automatically assigned when chapter was added to a closed State.",
                    actor);
            }
        }

        await dbContext.SaveChangesAsync();
        return ChapterSaveResult.Success(organization.Id);
    }

    public async Task<ChapterSaveResult> AbandonEmptyStateAsync(Guid stateOrganizationUnitId)
    {
        var state = await dbContext.OrganizationUnits
            .SingleOrDefaultAsync(unit => unit.Id == stateOrganizationUnitId);
        if (state is null)
        {
            return ChapterSaveResult.Success(stateOrganizationUnitId);
        }

        if (state.Level != OrganizationLevel.State)
        {
            return ChapterSaveResult.Failure("Only empty State records can be abandoned.");
        }

        var hasChildren = await dbContext.OrganizationUnits
            .AnyAsync(unit => unit.ParentOrganizationUnitId == state.Id);
        var hasAssignments = await dbContext.StateChapterAssignments
            .AnyAsync(assignment => assignment.StateOrganizationUnitId == state.Id);
        if (hasChildren || hasAssignments)
        {
            return ChapterSaveResult.Failure("State cannot be abandoned after a chapter has been added.");
        }

        var auditLogs = await dbContext.AuditLogs
            .Where(log => log.OrganizationUnitId == state.Id)
            .ToListAsync();
        dbContext.AuditLogs.RemoveRange(auditLogs);
        dbContext.OrganizationUnits.Remove(state);
        await dbContext.SaveChangesAsync();
        return ChapterSaveResult.Success(stateOrganizationUnitId);
    }

    public async Task<ChapterSaveResult> UpdateAsync(Guid id, ChapterEditModel input, ChapterActor actor)
    {
        Normalize(input);
        var organization = await dbContext.OrganizationUnits.SingleOrDefaultAsync(unit => unit.Id == id);
        if (organization is null)
        {
            return ChapterSaveResult.Failure("Chapter was not found.");
        }

        input.Status = organization.Status;
        var validation = await ValidateAsync(input, id);
        if (validation.Count > 0)
        {
            return ChapterSaveResult.Failure([.. validation]);
        }

        var changes = BuildChangeSet(organization, input);

        organization.Name = input.Name;
        organization.Abbreviation = input.Abbreviation;
        organization.Level = input.Level;
        organization.ParentOrganizationUnitId = input.ParentOrganizationUnitId;
        organization.City = input.City;
        organization.StateCode = input.StateCode;
        organization.MailingAddressLine1 = input.MailingAddressLine1;
        organization.MailingAddressLine2 = input.MailingAddressLine2;
        organization.MailingCity = input.MailingCity;
        organization.MailingStateCode = input.MailingStateCode;
        organization.MailingPostalCode = input.MailingPostalCode;
        organization.CharterDate = input.CharterDate;

        if (changes.Count > 0)
        {
            AddAudit(AuditAction.Updated, organization, actor, new { Changes = changes });
        }

        await dbContext.SaveChangesAsync();
        return ChapterSaveResult.Success(organization.Id);
    }

    public async Task<ChapterSaveResult> CloseAsync(Guid id, ChapterActor actor)
    {
        var organization = await dbContext.OrganizationUnits.SingleOrDefaultAsync(unit => unit.Id == id);
        if (organization is null)
        {
            return ChapterSaveResult.Failure("Chapter was not found.");
        }

        if (organization.Level == OrganizationLevel.National || IsEternalChapter(organization.Abbreviation))
        {
            return ChapterSaveResult.Failure($"{organization.Name} cannot be closed.");
        }

        organization.Status = OrganizationStatus.Closed;
        AddAudit(AuditAction.Closed, organization, actor, new { organization.Id, organization.Status });

        if (organization.Level == OrganizationLevel.LocalChapter &&
            organization.ParentOrganizationUnitId is Guid stateId)
        {
            await CloseStateIfLastChapterClosedAsync(stateId, organization, actor);
        }

        await dbContext.SaveChangesAsync();
        return ChapterSaveResult.Success(organization.Id);
    }

    public async Task<ChapterSaveResult> ReopenAsync(Guid id, ChapterActor actor)
    {
        var organization = await dbContext.OrganizationUnits.SingleOrDefaultAsync(unit => unit.Id == id);
        if (organization is null)
        {
            return ChapterSaveResult.Failure("Chapter was not found.");
        }

        organization.Status = OrganizationStatus.Operating;
        AddAudit(AuditAction.Reopened, organization, actor, new { organization.Id, organization.Status });

        if (organization.Level == OrganizationLevel.LocalChapter &&
            organization.ParentOrganizationUnitId is Guid stateId)
        {
            await ReopenParentStateAsync(stateId, organization, actor);
        }

        await dbContext.SaveChangesAsync();
        return ChapterSaveResult.Success(organization.Id);
    }

    public async Task<ChapterSaveResult> SuspendAsync(Guid id, DateOnly startsOn, DateOnly? endsOn, string? notes, ChapterActor actor)
    {
        var organization = await dbContext.OrganizationUnits.SingleOrDefaultAsync(unit => unit.Id == id);
        if (organization is null)
        {
            return ChapterSaveResult.Failure("Chapter was not found.");
        }

        if (organization.Level != OrganizationLevel.LocalChapter)
        {
            return ChapterSaveResult.Failure("Only local chapters can be suspended.");
        }

        if (IsEternalChapter(organization.Abbreviation))
        {
            return ChapterSaveResult.Failure("Eternal Chapter cannot be suspended.");
        }

        if (organization.Status == OrganizationStatus.Closed)
        {
            return ChapterSaveResult.Failure("Closed chapters cannot be suspended.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (await dbContext.ChapterSuspensions.AnyAsync(suspension =>
                suspension.OrganizationUnitId == id &&
                (suspension.EndsOn == null || suspension.EndsOn >= today)))
        {
            return ChapterSaveResult.Failure("This chapter already has an active suspension.");
        }

        organization.Status = OrganizationStatus.Suspended;
        dbContext.ChapterSuspensions.Add(new ChapterSuspension
        {
            Id = Guid.NewGuid(),
            OrganizationUnitId = organization.Id,
            StartsOn = startsOn,
            EndsOn = endsOn,
            Notes = notes,
            CreatedByUserId = actor.ApplicationUserId,
            ActorName = actor.ActorName,
            ActorSource = actor.ActorSource
        });

        AddAudit(AuditAction.Suspended, organization, actor, new { organization.Id, StartsOn = startsOn, EndsOn = endsOn, Notes = notes });
        await dbContext.SaveChangesAsync();
        return ChapterSaveResult.Success(organization.Id);
    }

    public async Task<ChapterSaveResult> EndSuspensionAsync(Guid id, DateOnly endsOn, ChapterActor actor)
    {
        var organization = await dbContext.OrganizationUnits.SingleOrDefaultAsync(unit => unit.Id == id);
        if (organization is null)
        {
            return ChapterSaveResult.Failure("Chapter was not found.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var suspension = await dbContext.ChapterSuspensions
            .Where(existing => existing.OrganizationUnitId == id &&
                               (existing.EndsOn == null || existing.EndsOn >= today))
            .OrderByDescending(existing => existing.StartsOn)
            .FirstOrDefaultAsync();

        if (suspension is null)
        {
            return ChapterSaveResult.Failure("This chapter does not have an active suspension.");
        }

        suspension.EndsOn = endsOn;
        organization.Status = OrganizationStatus.Operating;
        AddAudit(AuditAction.SuspensionEnded, organization, actor, new { organization.Id, EndsOn = endsOn });
        await dbContext.SaveChangesAsync();
        return ChapterSaveResult.Success(organization.Id);
    }

    public async Task<ChapterSaveResult> AssignStateChapterAsync(
        Guid stateOrganizationUnitId,
        Guid localChapterOrganizationUnitId,
        DateOnly startsOn,
        string? notes,
        ChapterActor actor)
    {
        var state = await dbContext.OrganizationUnits.SingleOrDefaultAsync(unit => unit.Id == stateOrganizationUnitId);
        var chapter = await dbContext.OrganizationUnits.SingleOrDefaultAsync(unit => unit.Id == localChapterOrganizationUnitId);
        if (state is null || state.Level != OrganizationLevel.State)
        {
            return ChapterSaveResult.Failure("State grouping was not found.");
        }

        if (chapter is null || chapter.Level != OrganizationLevel.LocalChapter || chapter.ParentOrganizationUnitId != state.Id)
        {
            return ChapterSaveResult.Failure("Acting State chapter must be a local chapter in the selected State.");
        }

        await AddStateChapterAssignmentAsync(state, chapter, startsOn, notes, actor);
        await dbContext.SaveChangesAsync();
        return ChapterSaveResult.Success(state.Id);
    }

    private async Task CloseStateIfLastChapterClosedAsync(Guid stateId, OrganizationUnit closedChapter, ChapterActor actor)
    {
        var hasNonClosedChapter = await dbContext.OrganizationUnits.AnyAsync(unit =>
            unit.ParentOrganizationUnitId == stateId &&
            unit.Level == OrganizationLevel.LocalChapter &&
            unit.Id != closedChapter.Id &&
            unit.Status != OrganizationStatus.Closed);

        if (hasNonClosedChapter)
        {
            return;
        }

        var state = await dbContext.OrganizationUnits.SingleOrDefaultAsync(unit =>
            unit.Id == stateId &&
            unit.Level == OrganizationLevel.State);

        if (state is null || state.Status == OrganizationStatus.Closed)
        {
            return;
        }

        state.Status = OrganizationStatus.Closed;
        AddAudit(AuditAction.Closed, state, actor, new
        {
            state.Id,
            state.Status,
            Reason = "Closed automatically because the last local chapter was closed.",
            LocalChapterId = closedChapter.Id,
            closedChapter.Abbreviation
        });
    }

    private async Task ReopenParentStateAsync(Guid stateId, OrganizationUnit reopenedChapter, ChapterActor actor)
    {
        var state = await dbContext.OrganizationUnits.SingleOrDefaultAsync(unit =>
            unit.Id == stateId &&
            unit.Level == OrganizationLevel.State);

        if (state is null || state.Status != OrganizationStatus.Closed)
        {
            return;
        }

        state.Status = OrganizationStatus.Operating;
        AddAudit(AuditAction.Reopened, state, actor, new
        {
            state.Id,
            state.Status,
            Reason = "Reopened automatically because a local chapter was reopened.",
            LocalChapterId = reopenedChapter.Id,
            reopenedChapter.Abbreviation
        });
    }

    private async Task AddStateChapterAssignmentAsync(
        OrganizationUnit state,
        OrganizationUnit chapter,
        DateOnly startsOn,
        string? notes,
        ChapterActor actor)
    {
        var currentAssignments = await dbContext.StateChapterAssignments
            .Where(assignment => assignment.StateOrganizationUnitId == state.Id && assignment.EndsOn == null)
            .ToListAsync();

        foreach (var current in currentAssignments)
        {
            current.EndsOn = startsOn.AddDays(-1);
        }

        dbContext.StateChapterAssignments.Add(new StateChapterAssignment
        {
            Id = Guid.NewGuid(),
            StateOrganizationUnitId = state.Id,
            LocalChapterOrganizationUnitId = chapter.Id,
            StartsOn = startsOn,
            Notes = notes,
            CreatedByUserId = actor.ApplicationUserId,
            ActorName = actor.ActorName,
            ActorSource = actor.ActorSource
        });

        AddAudit(AuditAction.StateChapterAssigned, state, actor, new
        {
            StateId = state.Id,
            LocalChapterId = chapter.Id,
            chapter.Abbreviation,
            StartsOn = startsOn,
            Notes = notes
        });
    }

    private async Task<List<string>> ValidateAsync(ChapterEditModel input, Guid? existingId)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(input.Name))
        {
            errors.Add("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(input.Abbreviation))
        {
            errors.Add("Abbreviation is required.");
        }

        if (await dbContext.OrganizationUnits.AnyAsync(unit => unit.Abbreviation == input.Abbreviation && unit.Id != existingId))
        {
            errors.Add("Abbreviation must be unique.");
        }

        if (input.Level == OrganizationLevel.National)
        {
            if (input.Status != OrganizationStatus.Operating)
            {
                errors.Add("National must remain operating.");
            }

            if (await dbContext.OrganizationUnits.AnyAsync(unit => unit.Level == OrganizationLevel.National && unit.Id != existingId))
            {
                errors.Add("Only one National chapter record is allowed.");
            }

            if (input.ParentOrganizationUnitId is not null)
            {
                errors.Add("National cannot have a parent.");
            }

            if (!string.Equals(input.Abbreviation, "NAT", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("National abbreviation must remain NAT.");
            }
        }
        else
        {
            if (input.ParentOrganizationUnitId is null)
            {
                errors.Add("A parent is required.");
            }
            else
            {
                var parent = await dbContext.OrganizationUnits.AsNoTracking().SingleOrDefaultAsync(unit => unit.Id == input.ParentOrganizationUnitId);
                if (parent is null)
                {
                    errors.Add("Parent was not found.");
                }
                else if (input.Level == OrganizationLevel.State && parent.Level != OrganizationLevel.National)
                {
                    errors.Add("State parent must be National.");
                }
                else if (input.Level == OrganizationLevel.LocalChapter &&
                         parent.Level != OrganizationLevel.State &&
                         !(IsEternalChapter(input.Abbreviation) && parent.Level == OrganizationLevel.National))
                {
                    errors.Add("Local chapter parent must be a State.");
                }
                else if (parent.Level == OrganizationLevel.LocalChapter)
                {
                    errors.Add("Local chapters cannot have child records.");
                }
            }
        }

        return errors;
    }

    private static void Normalize(ChapterEditModel input)
    {
        input.Name = ToTitleCase(input.Name.Trim());
        input.Abbreviation = input.Abbreviation.Trim().ToUpperInvariant();
        input.City = NormalizeOptional(input.City);
        input.StateCode = NormalizeOptional(input.StateCode)?.ToUpperInvariant();
        input.MailingAddressLine1 = NormalizeOptional(input.MailingAddressLine1);
        input.MailingAddressLine2 = NormalizeOptional(input.MailingAddressLine2);
        input.MailingCity = NormalizeOptional(input.MailingCity);
        input.MailingStateCode = NormalizeOptional(input.MailingStateCode)?.ToUpperInvariant();
        input.MailingPostalCode = NormalizeOptional(input.MailingPostalCode);

        if (IsEternalChapter(input.Abbreviation))
        {
            input.Abbreviation = "Chapter-100";
            input.City = null;
            input.StateCode = null;
            input.MailingAddressLine1 = null;
            input.MailingAddressLine2 = null;
            input.MailingCity = null;
            input.MailingStateCode = null;
            input.MailingPostalCode = null;
        }

        if (input.Level == OrganizationLevel.National)
        {
            input.ParentOrganizationUnitId = null;
            input.Status = OrganizationStatus.Operating;
            input.City = null;
            input.StateCode = null;
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string ToTitleCase(string value)
    {
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLowerInvariant());
    }

    private static string ChapterSortKey(ChapterTreeItem item)
    {
        if (item.Level == OrganizationLevel.National)
        {
            return "0000";
        }

        if (IsEternalChapter(item.Abbreviation))
        {
            return "0001";
        }

        return $"1000-{item.Name}";
    }

    public static bool IsEternalChapter(string? abbreviation)
    {
        return string.Equals(abbreviation, "Chapter-100", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(abbreviation, "US-100", StringComparison.OrdinalIgnoreCase);
    }

    private static List<AuditFieldChange> BuildChangeSet(OrganizationUnit organization, ChapterEditModel input)
    {
        var changes = new List<AuditFieldChange>();
        AddChange(changes, "Name", organization.Name, input.Name);
        AddChange(changes, "Abbreviation", organization.Abbreviation, input.Abbreviation);
        AddChange(changes, "City", organization.City, input.City);
        AddChange(changes, "State Code", organization.StateCode, input.StateCode);
        AddChange(changes, "Mailing Address Line 1", organization.MailingAddressLine1, input.MailingAddressLine1);
        AddChange(changes, "Mailing Address Line 2", organization.MailingAddressLine2, input.MailingAddressLine2);
        AddChange(changes, "Mailing City", organization.MailingCity, input.MailingCity);
        AddChange(changes, "Mailing State", organization.MailingStateCode, input.MailingStateCode);
        AddChange(changes, "Mailing ZIP Code", organization.MailingPostalCode, input.MailingPostalCode);
        AddChange(changes, "Charter Date", FormatAuditValue(organization.CharterDate), FormatAuditValue(input.CharterDate));
        return changes;
    }

    private static void AddChange(List<AuditFieldChange> changes, string field, string? oldValue, string? newValue)
    {
        if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
        {
            return;
        }

        changes.Add(new AuditFieldChange(field, oldValue, newValue));
    }

    private static string? FormatAuditValue(DateOnly? value)
    {
        return value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static string BuildAuditSummary(AuditAction action, string? detailsJson)
    {
        return action switch
        {
            AuditAction.Created => "Chapter created.",
            AuditAction.Updated => BuildUpdatedSummary(detailsJson),
            AuditAction.Closed => BuildReasonSummary(detailsJson, "Chapter closed."),
            AuditAction.Reopened => BuildReasonSummary(detailsJson, "Chapter reopened."),
            AuditAction.MemberAdded => "Member added.",
            AuditAction.MemberRemoved => "Member removed.",
            AuditAction.MemberUpdated => "Member updated.",
            _ => action.ToString()
        };
    }

    private static string BuildUpdatedSummary(string? detailsJson)
    {
        if (string.IsNullOrWhiteSpace(detailsJson))
        {
            return "Chapter details updated.";
        }

        try
        {
            using var document = JsonDocument.Parse(detailsJson);
            if (!document.RootElement.TryGetProperty("Changes", out var changes) ||
                changes.ValueKind != JsonValueKind.Array)
            {
                return "Chapter details updated.";
            }

            var summaries = changes.EnumerateArray()
                .Select(change =>
                {
                    var field = GetJsonString(change, "Field");
                    if (string.IsNullOrWhiteSpace(field))
                    {
                        return null;
                    }

                    return $"{field} changed from {DisplayAuditValue(GetJsonString(change, "From"))} to {DisplayAuditValue(GetJsonString(change, "To"))}";
                })
                .Where(summary => !string.IsNullOrWhiteSpace(summary))
                .Take(3)
                .ToList();

            return summaries.Count == 0
                ? "Chapter details updated."
                : string.Join("; ", summaries) + (changes.GetArrayLength() > summaries.Count ? "." : ".");
        }
        catch (JsonException)
        {
            return "Chapter details updated.";
        }
    }

    private static string BuildReasonSummary(string? detailsJson, string fallback)
    {
        if (string.IsNullOrWhiteSpace(detailsJson))
        {
            return fallback;
        }

        try
        {
            using var document = JsonDocument.Parse(detailsJson);
            var reason = GetJsonString(document.RootElement, "Reason");
            return string.IsNullOrWhiteSpace(reason) ? fallback : reason;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }

    private static string? GetJsonString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null
            ? property.ToString()
            : null;
    }

    private static string DisplayAuditValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "(blank)" : $"\"{value}\"";
    }

    private void AddAudit(AuditAction action, OrganizationUnit organization, ChapterActor actor, object details)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            ApplicationUserId = actor.ApplicationUserId,
            ActorName = actor.ActorName,
            ActorSource = actor.ActorSource,
            OrganizationUnitId = organization.Id,
            Action = action,
            EntityName = nameof(OrganizationUnit),
            EntityId = organization.Id.ToString(),
            DetailsJson = JsonSerializer.Serialize(details)
        });
    }

    private sealed record AuditFieldChange(string Field, string? From, string? To);
}

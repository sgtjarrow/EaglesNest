using System.Text.Json;
using EaglesNest.Core.Domain;
using EaglesNest.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace EaglesNest.Web.Services.Organizations;

public class OrganizationAdminService(ApplicationDbContext dbContext)
{
    public async Task<IReadOnlyList<OrganizationTreeItem>> GetHierarchyAsync()
    {
        var organizations = await dbContext.OrganizationUnits
            .AsNoTracking()
            .OrderBy(unit => unit.Level)
            .ThenBy(unit => unit.Abbreviation)
            .Select(unit => new OrganizationTreeItem
            {
                Id = unit.Id,
                ParentOrganizationUnitId = unit.ParentOrganizationUnitId,
                Level = unit.Level,
                Name = unit.Name,
                Abbreviation = unit.Abbreviation,
                City = unit.City,
                StateCode = unit.StateCode,
                IsActive = unit.IsActive
            })
            .ToListAsync();

        var byParent = organizations
            .Where(unit => unit.ParentOrganizationUnitId is not null)
            .GroupBy(unit => unit.ParentOrganizationUnitId!.Value)
            .ToDictionary(group => group.Key, group => group.OrderBy(SortKey).ThenBy(unit => unit.Name).ToList());

        foreach (var organization in organizations)
        {
            if (byParent.TryGetValue(organization.Id, out var children))
            {
                organization.Children = children;
            }
        }

        return organizations
            .Where(unit => unit.ParentOrganizationUnitId is null)
            .OrderBy(SortKey)
            .ThenBy(unit => unit.Name)
            .ToList();
    }

    public async Task<IReadOnlyList<OrganizationTreeItem>> GetParentOptionsAsync()
    {
        return await dbContext.OrganizationUnits
            .AsNoTracking()
            .Where(unit => unit.Level != OrganizationLevel.LocalChapter && unit.IsActive)
            .OrderBy(unit => unit.Level)
            .ThenBy(unit => unit.Abbreviation)
            .Select(unit => new OrganizationTreeItem
            {
                Id = unit.Id,
                ParentOrganizationUnitId = unit.ParentOrganizationUnitId,
                Level = unit.Level,
                Name = unit.Name,
                Abbreviation = unit.Abbreviation,
                IsActive = unit.IsActive
            })
            .ToListAsync();
    }

    public async Task<OrganizationEditModel?> GetOrganizationAsync(Guid id)
    {
        return await dbContext.OrganizationUnits
            .AsNoTracking()
            .Where(unit => unit.Id == id)
            .Select(unit => new OrganizationEditModel
            {
                Id = unit.Id,
                Name = unit.Name,
                Abbreviation = unit.Abbreviation,
                Level = unit.Level,
                ParentOrganizationUnitId = unit.ParentOrganizationUnitId,
                City = unit.City,
                StateCode = unit.StateCode,
                CharterNumber = unit.CharterNumber,
                IsActive = unit.IsActive
            })
            .SingleOrDefaultAsync();
    }

    public async Task<OrganizationSaveResult> CreateAsync(OrganizationEditModel input, string? userId)
    {
        Normalize(input);
        var validation = await ValidateAsync(input, null);
        if (validation.Count > 0)
        {
            return OrganizationSaveResult.Failure([.. validation]);
        }

        var organization = new OrganizationUnit
        {
            Id = Guid.NewGuid(),
            Name = input.Name,
            Abbreviation = input.Abbreviation,
            Level = input.Level,
            ParentOrganizationUnitId = input.ParentOrganizationUnitId,
            City = input.City,
            StateCode = input.StateCode,
            CharterNumber = input.CharterNumber,
            IsActive = input.IsActive
        };

        dbContext.OrganizationUnits.Add(organization);
        AddAudit(AuditAction.Created, organization, userId, input);
        await dbContext.SaveChangesAsync();
        return OrganizationSaveResult.Success(organization.Id);
    }

    public async Task<OrganizationSaveResult> UpdateAsync(Guid id, OrganizationEditModel input, string? userId)
    {
        Normalize(input);
        var organization = await dbContext.OrganizationUnits.SingleOrDefaultAsync(unit => unit.Id == id);
        if (organization is null)
        {
            return OrganizationSaveResult.Failure("Organization was not found.");
        }

        var validation = await ValidateAsync(input, id);
        if (validation.Count > 0)
        {
            return OrganizationSaveResult.Failure([.. validation]);
        }

        organization.Name = input.Name;
        organization.Abbreviation = input.Abbreviation;
        organization.Level = input.Level;
        organization.ParentOrganizationUnitId = input.ParentOrganizationUnitId;
        organization.City = input.City;
        organization.StateCode = input.StateCode;
        organization.CharterNumber = input.CharterNumber;
        organization.IsActive = input.IsActive;

        AddAudit(AuditAction.Updated, organization, userId, input);
        await dbContext.SaveChangesAsync();
        return OrganizationSaveResult.Success(organization.Id);
    }

    public async Task<OrganizationSaveResult> SetActiveAsync(Guid id, bool isActive, string? userId)
    {
        var organization = await dbContext.OrganizationUnits.SingleOrDefaultAsync(unit => unit.Id == id);
        if (organization is null)
        {
            return OrganizationSaveResult.Failure("Organization was not found.");
        }

        if (!isActive && organization.Level == OrganizationLevel.National)
        {
            return OrganizationSaveResult.Failure("National cannot be deactivated.");
        }

        organization.IsActive = isActive;
        AddAudit(AuditAction.Updated, organization, userId, new { organization.Id, IsActive = isActive });
        await dbContext.SaveChangesAsync();
        return OrganizationSaveResult.Success(organization.Id);
    }

    public async Task<bool> CanDeleteAsync(Guid id)
    {
        var organization = await dbContext.OrganizationUnits.AsNoTracking().SingleOrDefaultAsync(unit => unit.Id == id);
        if (organization is null || organization.Level == OrganizationLevel.National)
        {
            return false;
        }

        return !await dbContext.OrganizationUnits.AnyAsync(unit => unit.ParentOrganizationUnitId == id)
            && !await dbContext.Members.AnyAsync(member => member.PrimaryChapterId == id)
            && !await dbContext.MemberChapterAssignments.AnyAsync(assignment => assignment.ChapterId == id)
            && !await dbContext.RideEvents.AnyAsync(ride => ride.ChapterId == id)
            && !await dbContext.FinancialAssessments.AnyAsync(assessment => assessment.ChapterId == id)
            && !await dbContext.RoleAssignments.AnyAsync(role => role.OrganizationUnitId == id)
            && !await dbContext.ImportBatches.AnyAsync(batch => batch.OrganizationUnitId == id);
    }

    public async Task<OrganizationSaveResult> DeleteAsync(Guid id, string? userId)
    {
        var organization = await dbContext.OrganizationUnits.SingleOrDefaultAsync(unit => unit.Id == id);
        if (organization is null)
        {
            return OrganizationSaveResult.Failure("Organization was not found.");
        }

        if (!await CanDeleteAsync(id))
        {
            return OrganizationSaveResult.Failure("This organization cannot be deleted because it is protected or already has related records.");
        }

        var relatedAuditLogs = await dbContext.AuditLogs
            .Where(audit => audit.OrganizationUnitId == id)
            .ToListAsync();
        dbContext.AuditLogs.RemoveRange(relatedAuditLogs);
        dbContext.AuditLogs.Add(new AuditLog
        {
            ApplicationUserId = userId,
            Action = AuditAction.Deleted,
            EntityName = nameof(OrganizationUnit),
            EntityId = organization.Id.ToString(),
            DetailsJson = JsonSerializer.Serialize(new { organization.Id, organization.Name, organization.Abbreviation })
        });
        dbContext.OrganizationUnits.Remove(organization);
        await dbContext.SaveChangesAsync();
        return OrganizationSaveResult.Success(id);
    }

    private async Task<List<string>> ValidateAsync(OrganizationEditModel input, Guid? existingId)
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
            if (!input.IsActive)
            {
                errors.Add("National cannot be deactivated.");
            }

            if (await dbContext.OrganizationUnits.AnyAsync(unit => unit.Level == OrganizationLevel.National && unit.Id != existingId))
            {
                errors.Add("Only one National organization is allowed.");
            }

            if (input.ParentOrganizationUnitId is not null)
            {
                errors.Add("National cannot have a parent organization.");
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
                errors.Add("A parent organization is required.");
            }
            else
            {
                var parent = await dbContext.OrganizationUnits.AsNoTracking().SingleOrDefaultAsync(unit => unit.Id == input.ParentOrganizationUnitId);
                if (parent is null)
                {
                    errors.Add("Parent organization was not found.");
                }
                else if (input.Level == OrganizationLevel.State && parent.Level != OrganizationLevel.National)
                {
                    errors.Add("State parent must be National.");
                }
                else if (input.Level == OrganizationLevel.LocalChapter && parent.Level != OrganizationLevel.State)
                {
                    errors.Add("Local chapter parent must be a State.");
                }
                else if (parent.Level == OrganizationLevel.LocalChapter)
                {
                    errors.Add("Local chapters cannot have child organizations.");
                }
            }
        }

        return errors;
    }

    private static void Normalize(OrganizationEditModel input)
    {
        input.Name = input.Name.Trim();
        input.Abbreviation = input.Abbreviation.Trim().ToUpperInvariant();
        input.City = NormalizeOptional(input.City);
        input.StateCode = NormalizeOptional(input.StateCode)?.ToUpperInvariant();
        input.CharterNumber = NormalizeOptional(input.CharterNumber);

        if (input.Level == OrganizationLevel.National)
        {
            input.ParentOrganizationUnitId = null;
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static int SortKey(OrganizationTreeItem item)
    {
        var suffix = item.Abbreviation.Split('-').LastOrDefault();
        return int.TryParse(suffix, out var number) ? number : int.MaxValue;
    }

    private void AddAudit(AuditAction action, OrganizationUnit organization, string? userId, object details)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            ApplicationUserId = userId,
            OrganizationUnitId = organization.Id,
            Action = action,
            EntityName = nameof(OrganizationUnit),
            EntityId = organization.Id.ToString(),
            DetailsJson = JsonSerializer.Serialize(details)
        });
    }
}

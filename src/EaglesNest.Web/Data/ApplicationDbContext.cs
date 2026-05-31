using EaglesNest.Core.Domain;
using EaglesNest.Data;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EaglesNest.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<OrganizationUnit> OrganizationUnits => Set<OrganizationUnit>();
    public DbSet<ChapterSuspension> ChapterSuspensions => Set<ChapterSuspension>();
    public DbSet<StateChapterAssignment> StateChapterAssignments => Set<StateChapterAssignment>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<MemberChapterAssignment> MemberChapterAssignments => Set<MemberChapterAssignment>();
    public DbSet<MilitaryServiceRecord> MilitaryServiceRecords => Set<MilitaryServiceRecord>();
    public DbSet<MemberChangeRequest> MemberChangeRequests => Set<MemberChangeRequest>();
    public DbSet<RideEvent> RideEvents => Set<RideEvent>();
    public DbSet<RideAttendance> RideAttendance => Set<RideAttendance>();
    public DbSet<FinancialAssessment> FinancialAssessments => Set<FinancialAssessment>();
    public DbSet<FinancialPayment> FinancialPayments => Set<FinancialPayment>();
    public DbSet<RoleAssignment> RoleAssignments => Set<RoleAssignment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<ImportBatchRow> ImportBatchRows => Set<ImportBatchRow>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyEaglesNestModel();
    }
}

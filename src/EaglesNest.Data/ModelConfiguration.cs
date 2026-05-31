using EaglesNest.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace EaglesNest.Data;

public static class ModelConfiguration
{
    public static void ApplyEaglesNestModel(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrganizationUnit>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Abbreviation).HasMaxLength(25).IsRequired();
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.StateCode).HasMaxLength(10);
            entity.HasIndex(e => e.Abbreviation).IsUnique();
            entity.HasOne(e => e.ParentOrganizationUnit)
                .WithMany(e => e.Children)
                .HasForeignKey(e => e.ParentOrganizationUnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ChapterSuspension>(entity =>
        {
            entity.Property(e => e.ActorName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.ActorSource).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedByUserId).HasMaxLength(450);
            entity.HasOne(e => e.OrganizationUnit)
                .WithMany()
                .HasForeignKey(e => e.OrganizationUnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StateChapterAssignment>(entity =>
        {
            entity.Property(e => e.ActorName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.ActorSource).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedByUserId).HasMaxLength(450);
            entity.HasOne(e => e.StateOrganizationUnit)
                .WithMany()
                .HasForeignKey(e => e.StateOrganizationUnitId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.LocalChapterOrganizationUnit)
                .WithMany()
                .HasForeignKey(e => e.LocalChapterOrganizationUnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Member>(entity =>
        {
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PreferredName).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
            entity.Property(e => e.State).HasMaxLength(50);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.HasOne(e => e.PrimaryChapter)
                .WithMany()
                .HasForeignKey(e => e.PrimaryChapterId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MemberChapterAssignment>(entity =>
        {
            entity.HasOne(e => e.Chapter)
                .WithMany()
                .HasForeignKey(e => e.ChapterId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MilitaryServiceRecord>(entity =>
        {
            entity.Property(e => e.Branch).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Rank).HasMaxLength(100);
        });

        modelBuilder.Entity<MemberChangeRequest>(entity =>
        {
            entity.Property(e => e.SubmittedByUserId).HasMaxLength(450).IsRequired();
            entity.Property(e => e.ReviewedByUserId).HasMaxLength(450);
            entity.Property(e => e.RequestedChangesJson).IsRequired();
        });

        modelBuilder.Entity<RideEvent>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.PlannedMiles).HasPrecision(9, 1);
            entity.HasOne(e => e.Chapter)
                .WithMany()
                .HasForeignKey(e => e.ChapterId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RideAttendance>(entity =>
        {
            entity.Property(e => e.ActualMiles).HasPrecision(9, 1);
            entity.HasIndex(e => new { e.RideEventId, e.MemberId }).IsUnique();
        });

        modelBuilder.Entity<FinancialAssessment>(entity =>
        {
            entity.Property(e => e.Description).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(12, 2);
            entity.HasOne(e => e.Chapter)
                .WithMany()
                .HasForeignKey(e => e.ChapterId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FinancialPayment>(entity =>
        {
            entity.Property(e => e.Amount).HasPrecision(12, 2);
            entity.Property(e => e.PaymentMethod).HasMaxLength(100);
            entity.Property(e => e.ReferenceNumber).HasMaxLength(100);
        });

        modelBuilder.Entity<RoleAssignment>(entity =>
        {
            entity.Property(e => e.ApplicationUserId).HasMaxLength(450).IsRequired();
            entity.HasOne(e => e.OrganizationUnit)
                .WithMany()
                .HasForeignKey(e => e.OrganizationUnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(e => e.ApplicationUserId).HasMaxLength(450);
            entity.Property(e => e.ActorName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.ActorSource).HasMaxLength(100).IsRequired();
            entity.Property(e => e.EntityName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.EntityId).HasMaxLength(100);
            entity.HasOne(e => e.OrganizationUnit)
                .WithMany()
                .HasForeignKey(e => e.OrganizationUnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ImportBatch>(entity =>
        {
            entity.Property(e => e.FileName).HasMaxLength(260).IsRequired();
            entity.Property(e => e.UploadedByUserId).HasMaxLength(450).IsRequired();
        });

        modelBuilder.Entity<ImportBatchRow>(entity =>
        {
            entity.Property(e => e.RawJson).IsRequired();
        });
    }
}

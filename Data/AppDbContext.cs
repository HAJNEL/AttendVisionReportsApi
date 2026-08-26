namespace AttendVisionReportsApi.Data
{
    using AttendVisionReportsApi.Models;
    using Microsoft.EntityFrameworkCore;
    using System.Collections.Generic;

    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<AccessRecord> AccessRecords => Set<AccessRecord>();
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<DepartmentUser> DepartmentUsers => Set<DepartmentUser>();
        public DbSet<TimeOverride> TimeOverrides => Set<TimeOverride>();
        public DbSet<EmployeeLeave> EmployeeLeaves => Set<EmployeeLeave>();
        public DbSet<DepartmentPaymentRate> DepartmentPaymentRates => Set<DepartmentPaymentRate>();
        public DbSet<TimeManagementConfig> TimeManagementConfigs => Set<TimeManagementConfig>();
        public DbSet<ReportConfig> ReportConfigs => Set<ReportConfig>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<EmployeeAccessLevel> EmployeeAccessLevels => Set<EmployeeAccessLevel>();
        public DbSet<TempEmployee> TempEmployees => Set<TempEmployee>();
        public DbSet<TempEmployeeHistory> TempEmployeeHistories => Set<TempEmployeeHistory>();
        public DbSet<ImpersonationEvent> ImpersonationEvents => Set<ImpersonationEvent>();
        public DbSet<DeviceLicense> DeviceLicenses => Set<DeviceLicense>();
        public DbSet<AttendanceGroup> AttendanceGroups => Set<AttendanceGroup>();
        public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
        public DbSet<InvoiceSettings> InvoiceSettingsEntries => Set<InvoiceSettings>();
        public DbSet<InvoiceTemplate> InvoiceTemplates => Set<InvoiceTemplate>();
        public DbSet<InvoiceTemplateItem> InvoiceTemplateItems => Set<InvoiceTemplateItem>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // EmployeeLeave configuration
            modelBuilder.Entity<EmployeeLeave>()
                .ToTable("employee_leave")
                .HasKey(e => e.Id);

            modelBuilder.Entity<EmployeeLeave>()
                .Property(e => e.Type)
                .HasColumnName("type");
            modelBuilder.Entity<EmployeeLeave>()
                .Property(e => e.FromDate)
                .HasColumnName("from_date");
            modelBuilder.Entity<EmployeeLeave>()
                .Property(e => e.ToDate)
                .HasColumnName("to_date");
            modelBuilder.Entity<EmployeeLeave>()
                .Property(e => e.FromTime)
                .HasColumnName("from_time");
            modelBuilder.Entity<EmployeeLeave>()
                .Property(e => e.ToTime)
                .HasColumnName("to_time");
            modelBuilder.Entity<EmployeeLeave>()
                .Property(e => e.EmployeeId)
                .HasColumnName("employee_id");
            modelBuilder.Entity<EmployeeLeave>()
                .Property(e => e.FullName)
                .HasColumnName("full_name");
            modelBuilder.Entity<EmployeeLeave>()
                .HasOne<Department>()
                .WithMany()
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);

            // DepartmentUser configuration
            modelBuilder.Entity<DepartmentUser>()
                .ToTable("department_users")
                .HasKey(du => du.Id);

            modelBuilder.Entity<DepartmentUser>()
                .HasIndex(du => new { du.DepartmentId, du.UserId })
                .IsUnique();

            modelBuilder.Entity<DepartmentUser>()
                .HasOne(du => du.Department)
                .WithMany()
                .HasForeignKey(du => du.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DepartmentUser>()
                .HasOne(du => du.User)
                .WithMany()
                .HasForeignKey(du => du.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Permission hierarchy
            modelBuilder.Entity<Permission>()
                .HasMany(p => p.Children)
                .WithOne(p => p.Parent)
                .HasForeignKey(p => p.ParentId)
                .OnDelete(DeleteBehavior.SetNull);

            // RolePermission PK is now Id (no composite key)
            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // TimeOverride configuration
            modelBuilder.Entity<TimeOverride>()
                .ToTable("time_overrides")
                .HasKey(t => t.Id);

            modelBuilder.Entity<TimeOverride>()
                .HasOne(t => t.Department)
                .WithMany()
                .HasForeignKey(t => t.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TimeOverride>()
                .Property(t => t.FromTime)
                .HasColumnName("from_time");
            modelBuilder.Entity<TimeOverride>()
                .Property(t => t.ToTime)
                .HasColumnName("to_time");
            modelBuilder.Entity<TimeOverride>()
                .Property(t => t.OverrideTime)
                .HasColumnName("override_time");

            // DepartmentPaymentRate configuration
            modelBuilder.Entity<DepartmentPaymentRate>()
                .ToTable("department_payment_rates")
                .HasKey(r => r.Id);

            modelBuilder.Entity<DepartmentPaymentRate>()
                .Property(r => r.RateType)
                .HasColumnName("rate_type");
            modelBuilder.Entity<DepartmentPaymentRate>()
                .Property(r => r.Amount)
                .HasColumnName("amount");
            modelBuilder.Entity<DepartmentPaymentRate>()
                .Property(r => r.MatchKey)
                .HasColumnName("match_key");
            modelBuilder.Entity<DepartmentPaymentRate>()
                .Property(r => r.OtherLabel)
                .HasColumnName("other_label");
            modelBuilder.Entity<DepartmentPaymentRate>()
                .Property(r => r.AppliesTo)
                .HasColumnName("applies_to");
            modelBuilder.Entity<DepartmentPaymentRate>()
                .Property(r => r.DepartmentId)
                .HasColumnName("department_id");

            // TimeManagementConfig configuration (column names mapped via attributes)
            modelBuilder.Entity<TimeManagementConfig>()
                .ToTable("time_management_config")
                .HasKey(c => c.Id);

            modelBuilder.Entity<TimeManagementConfig>()
                .HasIndex(c => c.CompanyId)
                .IsUnique();

            modelBuilder.Entity<TimeManagementConfig>()
                .HasOne<Company>()
                .WithMany()
                .HasForeignKey(c => c.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // ReportConfig configuration (column names mapped via attributes)
            modelBuilder.Entity<ReportConfig>()
                .ToTable("report_config")
                .HasKey(c => c.Id);

            modelBuilder.Entity<ReportConfig>()
                .HasIndex(c => c.CompanyId)
                .IsUnique();

            modelBuilder.Entity<ReportConfig>()
                .HasOne<Company>()
                .WithMany()
                .HasForeignKey(c => c.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Employee configuration
            modelBuilder.Entity<Employee>()
                .ToTable("employees")
                .HasKey(e => e.Id);

            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.HikCentralPersonId)
                .IsUnique();

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany()
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<EmployeeAccessLevel>()
                .ToTable("employee_access_levels")
                .HasKey(a => a.Id);

            modelBuilder.Entity<EmployeeAccessLevel>()
                .HasOne<Employee>()
                .WithMany()
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // ImpersonationEvent configuration
            modelBuilder.Entity<ImpersonationEvent>()
                .ToTable("impersonation_events")
                .HasKey(e => e.Id);

            // Invoice configuration
            modelBuilder.Entity<Invoice>()
                .HasMany(i => i.LineItems)
                .WithOne()
                .HasForeignKey(l => l.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.InvoiceNumber)
                .IsUnique();

            // InvoiceTemplate configuration
            modelBuilder.Entity<InvoiceTemplate>()
                .HasMany(t => t.Items)
                .WithOne()
                .HasForeignKey(i => i.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

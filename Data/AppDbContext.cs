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
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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
        }
    }
}

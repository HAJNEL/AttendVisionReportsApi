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
    }
}

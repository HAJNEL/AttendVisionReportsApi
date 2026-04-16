using AttendVisionReportsApi.Data;
using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AttendVisionReportsApi.Services
{
    public class DepartmentUserService : IDepartmentUserService
    {
        private readonly AppDbContext db;
        public DepartmentUserService(AppDbContext db)
        {
            this.db = db;
        }

        public async Task<IEnumerable<DepartmentUserDto>> GetAllAsync()
        {
            return await db.DepartmentUsers
                .Select(du => new DepartmentUserDto
                {
                    Id = du.Id,
                    DepartmentId = du.DepartmentId,
                    UserId = du.UserId,
                    AssignedAt = du.AssignedAt
                })
                .ToListAsync();
        }

        public async Task<DepartmentUserDto?> GetByIdAsync(Guid id)
        {
            var du = await db.DepartmentUsers.FindAsync(id);
            if (du == null) return null;
            return new DepartmentUserDto
            {
                Id = du.Id,
                DepartmentId = du.DepartmentId,
                UserId = du.UserId,
                AssignedAt = du.AssignedAt
            };
        }

        public async Task<DepartmentUserDto> CreateAsync(CreateDepartmentUserDto dto)
        {
            var du = new DepartmentUser
            {
                Id = Guid.NewGuid(),
                DepartmentId = dto.DepartmentId,
                UserId = dto.UserId,
                AssignedAt = DateTime.UtcNow
            };
            db.DepartmentUsers.Add(du);
            await db.SaveChangesAsync();
            return new DepartmentUserDto
            {
                Id = du.Id,
                DepartmentId = du.DepartmentId,
                UserId = du.UserId,
                AssignedAt = du.AssignedAt
            };
        }

        public async Task<DepartmentUserDto?> UpdateAsync(Guid id, UpdateDepartmentUserDto dto)
        {
            var du = await db.DepartmentUsers.FindAsync(id);
            if (du == null) return null;
            du.DepartmentId = dto.DepartmentId;
            du.UserId = dto.UserId;
            await db.SaveChangesAsync();
            return new DepartmentUserDto
            {
                Id = du.Id,
                DepartmentId = du.DepartmentId,
                UserId = du.UserId,
                AssignedAt = du.AssignedAt
            };
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var du = await db.DepartmentUsers.FindAsync(id);
            if (du == null) return false;
            db.DepartmentUsers.Remove(du);
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<DepartmentUserDto>> GetUsersForDepartmentAsync(Guid departmentId)
        {
            return await db.DepartmentUsers
                .Where(du => du.DepartmentId == departmentId)
                .Select(du => new DepartmentUserDto
                {
                    Id = du.Id,
                    DepartmentId = du.DepartmentId,
                    UserId = du.UserId,
                    AssignedAt = du.AssignedAt
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<DepartmentUserDto>> GetDepartmentsForUserAsync(Guid userId)
        {
            return await db.DepartmentUsers
                .Where(du => du.UserId == userId)
                .Select(du => new DepartmentUserDto
                {
                    Id = du.Id,
                    DepartmentId = du.DepartmentId,
                    UserId = du.UserId,
                    AssignedAt = du.AssignedAt
                })
                .ToListAsync();
        }
    }
}
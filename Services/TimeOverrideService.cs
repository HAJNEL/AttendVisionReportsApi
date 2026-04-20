using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AttendVisionReportsApi.Data;
using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendVisionReportsApi.Services
{
    public class TimeOverrideService : ITimeOverrideService
    {
        private readonly AppDbContext _context;
        public TimeOverrideService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TimeOverrideDto>> GetAllAsync()
        {
            return await _context.TimeOverrides
                .Select(t => new TimeOverrideDto
                {
                    Id = t.Id,
                    DepartmentId = t.DepartmentId,
                    FromTime = t.FromTime,
                    ToTime = t.ToTime,
                    OverrideTime = t.OverrideTime
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<TimeOverrideDto>> GetByDepartmentIdAsync(Guid departmentId)
        {
            return await _context.TimeOverrides
                .Where(t => t.DepartmentId == departmentId)
                .Select(t => new TimeOverrideDto
                {
                    Id = t.Id,
                    DepartmentId = t.DepartmentId,
                    FromTime = t.FromTime,
                    ToTime = t.ToTime,
                    OverrideTime = t.OverrideTime
                })
                .ToListAsync();
        }

        public async Task<TimeOverrideDto> GetByIdAsync(Guid id)
        {
            var t = await _context.TimeOverrides.FindAsync(id);
            if (t == null) return null;
            return new TimeOverrideDto
            {
                Id = t.Id,
                DepartmentId = t.DepartmentId,
                FromTime = t.FromTime,
                ToTime = t.ToTime,
                OverrideTime = t.OverrideTime
            };
        }

        public async Task<TimeOverrideDto> CreateAsync(CreateTimeOverrideDto dto)
        {
            var entity = new TimeOverride
            {
                Id = Guid.NewGuid(),
                DepartmentId = dto.DepartmentId,
                FromTime = dto.FromTime,
                ToTime = dto.ToTime,
                OverrideTime = dto.OverrideTime
            };
            _context.TimeOverrides.Add(entity);
            await _context.SaveChangesAsync();
            return new TimeOverrideDto
            {
                Id = entity.Id,
                DepartmentId = entity.DepartmentId,
                FromTime = entity.FromTime,
                ToTime = entity.ToTime,
                OverrideTime = entity.OverrideTime
            };
        }

        public async Task<TimeOverrideDto> UpdateAsync(Guid id, UpdateTimeOverrideDto dto)
        {
            var entity = await _context.TimeOverrides.FindAsync(id);
            if (entity == null) return null;
            if (dto.FromTime.HasValue) entity.FromTime = dto.FromTime.Value;
            if (dto.ToTime.HasValue) entity.ToTime = dto.ToTime.Value;
            if (dto.OverrideTime.HasValue) entity.OverrideTime = dto.OverrideTime.Value;
            await _context.SaveChangesAsync();
            return new TimeOverrideDto
            {
                Id = entity.Id,
                DepartmentId = entity.DepartmentId,
                FromTime = entity.FromTime,
                ToTime = entity.ToTime,
                OverrideTime = entity.OverrideTime
            };
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.TimeOverrides.FindAsync(id);
            if (entity == null) return false;
            _context.TimeOverrides.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
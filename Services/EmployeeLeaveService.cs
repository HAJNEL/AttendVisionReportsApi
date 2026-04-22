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
    public class EmployeeLeaveService : IEmployeeLeaveService
    {
        private readonly AppDbContext _context;
        public EmployeeLeaveService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<EmployeeLeaveDto>> GetAllAsync()
        {
            return await _context.EmployeeLeaves
                .Select(e => new EmployeeLeaveDto
                {
                    Id = e.Id,
                    DepartmentId = e.DepartmentId,
                    Type = e.Type,
                    FromDate = e.FromDate,
                    ToDate = e.ToDate,
                    FromTime = e.FromTime,
                    ToTime = e.ToTime,
                    EmployeeId = e.EmployeeId,
                    FullName = e.FullName
                })
                .ToListAsync();
        }

        public async Task<EmployeeLeaveDto> GetByIdAsync(Guid id)
        {
            var e = await _context.EmployeeLeaves.FindAsync(id);
            if (e == null) return null!;
            return new EmployeeLeaveDto
            {
                Id = e.Id,
                DepartmentId = e.DepartmentId,
                Type = e.Type,
                FromDate = e.FromDate,
                ToDate = e.ToDate,
                FromTime = e.FromTime,
                ToTime = e.ToTime,
                EmployeeId = e.EmployeeId,
                FullName = e.FullName
            };
        }

        public async Task<IEnumerable<EmployeeLeaveDto>> CreateAsync(CreateEmployeeLeaveDto dto)
        {
            // Validate required fields
            if (dto == null || dto.DepartmentId == Guid.Empty || string.IsNullOrEmpty(dto.Type) || dto.FromDate == default || dto.ToDate == default || string.IsNullOrEmpty(dto.EmployeeId) || string.IsNullOrEmpty(dto.FullName))
            {
                throw new ArgumentException("Missing required fields for creating EmployeeLeave");
            }

            var entity = new EmployeeLeave
            {
                Id = Guid.NewGuid(),
                DepartmentId = dto.DepartmentId,
                Type = dto.Type,
                FromDate = dto.FromDate,
                ToDate = dto.ToDate,
                FromTime = dto.FromTime,
                ToTime = dto.ToTime,
                EmployeeId = dto.EmployeeId,
                FullName = dto.FullName
            };
            _context.EmployeeLeaves.Add(entity);
            await _context.SaveChangesAsync();
            return new List<EmployeeLeaveDto>
            {
                new EmployeeLeaveDto
                {
                    Id = entity.Id,
                    DepartmentId = entity.DepartmentId,
                    Type = entity.Type,
                    FromDate = entity.FromDate,
                    ToDate = entity.ToDate,
                    FromTime = entity.FromTime,
                    ToTime = entity.ToTime,
                    EmployeeId = entity.EmployeeId,
                    FullName = entity.FullName
                }
            };
        }

        public async Task<EmployeeLeaveDto> UpdateAsync(Guid id, UpdateEmployeeLeaveDto dto)
        {
            var entity = await _context.EmployeeLeaves.FindAsync(id);
            if (entity == null) return null!;
            if (dto.Type != null) entity.Type = dto.Type;
            if (dto.FullName != null) entity.FullName = dto.FullName;
            if (dto.FromTime.HasValue) entity.FromTime = dto.FromTime;
            if (dto.ToTime.HasValue) entity.ToTime = dto.ToTime;
            if (dto.FromDate != default) entity.FromDate = dto.FromDate;
            if (dto.ToDate != default) entity.ToDate = dto.ToDate;
            await _context.SaveChangesAsync();
            return new EmployeeLeaveDto
            {
                Id = entity.Id,
                DepartmentId = entity.DepartmentId,
                Type = entity.Type,
                FromDate = entity.FromDate,
                ToDate = entity.ToDate,
                FromTime = entity.FromTime,
                ToTime = entity.ToTime,
                EmployeeId = entity.EmployeeId,
                FullName = entity.FullName
            };
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.EmployeeLeaves.FindAsync(id);
            if (entity == null) return false;
            _context.EmployeeLeaves.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> DeleteRangeAsync(string userCode, DateOnly startDate, DateOnly endDate)
        {
            var toDelete = await _context.EmployeeLeaves
                .Where(e => e.EmployeeId == userCode && e.FromDate >= startDate && e.ToDate <= endDate)
                .ToListAsync();
            _context.EmployeeLeaves.RemoveRange(toDelete);
            await _context.SaveChangesAsync();
            return toDelete.Count;
        }

        public async Task<IEnumerable<EmployeeLeaveRangeDto>> GetRangesAsync(Guid? departmentId, string employeeId, DateOnly? startDate, DateOnly? endDate)
        {
            var query = _context.EmployeeLeaves.AsQueryable();
            if (departmentId.HasValue)
                query = query.Where(e => e.DepartmentId == departmentId.Value);
            if (!string.IsNullOrEmpty(employeeId))
                query = query.Where(e => e.EmployeeId == employeeId);
            if (startDate.HasValue)
                query = query.Where(e => e.FromDate >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(e => e.ToDate <= endDate.Value);
            var leaves = await query.ToListAsync();
            var departmentNames = _context.Departments
                .Where(d => leaves.Select(l => l.DepartmentId).Distinct().Contains(d.Id))
                .ToDictionary(d => d.Id, d => d.DepartmentName);

            var ranges = leaves
                .Select(e => new EmployeeLeaveRangeDto
                {
                    Id = e.Id,
                    EmployeeId = e.EmployeeId,
                    FullName = e.FullName,
                    DepartmentId = e.DepartmentId,
                    DepartmentName = departmentNames.TryGetValue(e.DepartmentId, out var name) ? name : string.Empty,
                    Type = e.Type,
                    FromDate = e.FromDate,
                    ToDate = e.ToDate,
                    FromTime = e.FromTime,
                    ToTime = e.ToTime
                })
                .OrderBy(x => x.FromDate)
                .ToList();
            return ranges;
        }
    }
}
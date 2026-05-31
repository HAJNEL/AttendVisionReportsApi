
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AttendVisionReportsApi.Data;
using DepartmentPaymentRateDto = AttendVisionReportsApi.DTOs.DepartmentPaymentRate;
using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendVisionReportsApi.Services
{
    public class DepartmentPaymentRateService
    {
        private readonly AppDbContext _context;
        public DepartmentPaymentRateService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DepartmentPaymentRateDto>> GetByDepartmentAsync(Guid departmentId)
        {
            return await _context.DepartmentPaymentRates
                .Where(r => r.DepartmentId == departmentId)
                .Select(r => new DepartmentPaymentRateDto(
                    r.Id,
                    r.DepartmentId,
                    r.RateType,
                    r.Amount,
                    r.MatchKey,
                    r.OtherLabel,
                    r.AppliesTo
                ))
                .ToListAsync();
        }

        public async Task<DepartmentPaymentRateDto?> GetByIdAsync(Guid id)
        {
            var r = await _context.DepartmentPaymentRates.FindAsync(id);
            if (r == null) return null;
            return new DepartmentPaymentRateDto(
                r.Id,
                r.DepartmentId,
                r.RateType,
                r.Amount,
                r.MatchKey,
                r.OtherLabel,
                r.AppliesTo
            );
        }

        public async Task<DepartmentPaymentRateDto> CreateAsync(Guid departmentId, DepartmentPaymentRateInput input)
        {
            // Validate required fields
            if (input == null || string.IsNullOrEmpty(input.RateType) || input.Amount <= 0 || string.IsNullOrEmpty(input.AppliesTo))
                throw new ArgumentException("Missing required fields for creating DepartmentPaymentRate");

            var entity = new Models.DepartmentPaymentRate
            {
                Id = Guid.NewGuid(),
                DepartmentId = departmentId,
                RateType = input.RateType,
                Amount = input.Amount,
                MatchKey = input.MatchKey,
                OtherLabel = input.OtherLabel,
                AppliesTo = input.AppliesTo
            };
            _context.DepartmentPaymentRates.Add(entity);
            await _context.SaveChangesAsync();
            return new DepartmentPaymentRateDto(
                entity.Id,
                entity.DepartmentId,
                entity.RateType,
                entity.Amount,
                entity.MatchKey,
                entity.OtherLabel,
                entity.AppliesTo
            );
        }

        public async Task<DepartmentPaymentRateDto?> UpdateAsync(Guid id, DepartmentPaymentRateInput input)
        {
            var entity = await _context.DepartmentPaymentRates.FindAsync(id);
            if (entity == null) return null;
            if (!string.IsNullOrEmpty(input.RateType)) entity.RateType = input.RateType;
            if (input.Amount > 0) entity.Amount = input.Amount;
            if (!string.IsNullOrEmpty(input.AppliesTo)) entity.AppliesTo = input.AppliesTo;

            if (entity.AppliesTo == "other")
            {
                if (string.IsNullOrWhiteSpace(input.MatchKey) || input.MatchKey.Length > 20)
                    throw new ArgumentException("'matchKey' is required and must be at most 20 characters for 'Other' rates.");
                if (string.IsNullOrWhiteSpace(input.OtherLabel) || input.OtherLabel.Length > 40)
                    throw new ArgumentException("'otherLabel' is required and must be at most 40 characters for 'Other' rates.");
                entity.MatchKey = input.MatchKey;
                entity.OtherLabel = input.OtherLabel;
            }
            else
            {
                entity.MatchKey = null;
                entity.OtherLabel = null;
            }

            await _context.SaveChangesAsync();
            return new DepartmentPaymentRateDto(
                entity.Id,
                entity.DepartmentId,
                entity.RateType,
                entity.Amount,
                entity.MatchKey,
                entity.OtherLabel,
                entity.AppliesTo
            );
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.DepartmentPaymentRates.FindAsync(id);
            if (entity == null) return false;
            _context.DepartmentPaymentRates.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

using AttendVisionReportsApi.Data;
using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendVisionReportsApi.Services
{
    public class DepartmentsService(AppDbContext db) : IDepartmentsService
    {

        public Task<List<DepartmentResponse>> GetAllAsync() =>
            (from d in db.Departments
             join c in db.Companies on d.CompanyId equals c.Id into companyJoin
             from c in companyJoin.DefaultIfEmpty()
             orderby d.DepartmentName
             select new DepartmentResponse(
                 d.Id,
                 d.DepartmentName,
                 d.Manager,
                 d.AddressLine1,
                 d.AddressLine2,
                 d.City,
                 d.State,
                 d.PostalCode,
                 d.Country,
                 d.SerialNo,
                 d.CompanyId,
                 d.CompanyCode,
                 c != null ? c.Name : null,
                 d.HikCentralOrgIndexCode
             )).ToListAsync();

        // Overload: filter by current user
        public async Task<List<DepartmentResponse>> GetAllForUserAsync(System.Security.Claims.ClaimsPrincipal user)
        {

            if (!Helpers.ClaimsHelper.TryGetUserId(user, out var userId, logClaims: false))
                return new List<DepartmentResponse>();

            var departmentIds = await db.DepartmentUsers
                .Where(du => du.UserId == userId)
                .Select(du => du.DepartmentId)
                .ToListAsync();

            return await (from d in db.Departments
                          where departmentIds.Contains(d.Id)
                          join c in db.Companies on d.CompanyId equals c.Id into companyJoin
                          from c in companyJoin.DefaultIfEmpty()
                          orderby d.DepartmentName
                          select new DepartmentResponse(
                              d.Id,
                              d.DepartmentName,
                              d.Manager,
                              d.AddressLine1,
                              d.AddressLine2,
                              d.City,
                              d.State,
                              d.PostalCode,
                              d.Country,
                              d.SerialNo,
                              d.CompanyId,
                              d.CompanyCode,
                              c != null ? c.Name : null,
                              d.HikCentralOrgIndexCode
                          )).ToListAsync();
        }


        public async Task<DepartmentResponse> CreateAsync(DepartmentInput input)
        {
            if (await IsDuplicateAsync(input.DepartmentName, input.SerialNo))
                throw new InvalidOperationException(
                    $"A department named '{input.DepartmentName}' with serial number '{input.SerialNo}' already exists.");

            var dept = Apply(new Department(), input);
            db.Departments.Add(dept);
            await db.SaveChangesAsync();
            var companyName = dept.CompanyId != null ? await db.Companies.Where(c => c.Id == dept.CompanyId).Select(c => c.Name).FirstOrDefaultAsync() : null;
            return Map(dept, companyName);
        }


        public async Task<DepartmentResponse?> UpdateAsync(Guid id, DepartmentInput input)
        {
            var dept = await db.Departments.FindAsync(id);
            if (dept is null) return null;

            if (await IsDuplicateAsync(input.DepartmentName, input.SerialNo, id))
                throw new InvalidOperationException(
                    $"A department named '{input.DepartmentName}' with serial number '{input.SerialNo}' already exists.");

            Apply(dept, input);
            await db.SaveChangesAsync();
            var companyName = dept.CompanyId != null ? await db.Companies.Where(c => c.Id == dept.CompanyId).Select(c => c.Name).FirstOrDefaultAsync() : null;
            return Map(dept, companyName);
        }

        private Task<bool> IsDuplicateAsync(string departmentName, string? serialNo, Guid? excludeId = null) =>
            db.Departments.AnyAsync(d =>
                d.DepartmentName == departmentName &&
                d.SerialNo == serialNo &&
                (excludeId == null || d.Id != excludeId));

        public async Task<bool> DeleteAsync(Guid id)
        {
            var dept = await db.Departments.FindAsync(id);
            if (dept is null) return false;
            db.Departments.Remove(dept);
            await db.SaveChangesAsync();
            return true;
        }

        private static Department Apply(Department d, DepartmentInput i)
        {
            d.DepartmentName = i.DepartmentName;
            d.Manager = i.Manager;
            d.AddressLine1 = i.AddressLine1;
            d.AddressLine2 = i.AddressLine2;
            d.City = i.City;
            d.State = i.State;
            d.PostalCode = i.PostalCode;
            d.Country = i.Country;
            d.SerialNo = i.SerialNo;
            d.CompanyId = i.CompanyId;
            d.CompanyCode = i.CompanyCode;
            d.HikCentralOrgIndexCode = i.HikCentralOrgIndexCode;
            return d;
        }

        private static DepartmentResponse Map(Department d, string? companyName = null) =>
            new(
                d.Id,
                d.DepartmentName,
                d.Manager,
                d.AddressLine1,
                d.AddressLine2,
                d.City,
                d.State,
                d.PostalCode,
                d.Country,
                d.SerialNo,
                d.CompanyId,
                d.CompanyCode,
                companyName,
                d.HikCentralOrgIndexCode
            );
    }
}

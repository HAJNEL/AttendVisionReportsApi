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
                 (double?)d.PaymentRate,
                 d.AddressLine1,
                 d.AddressLine2,
                 d.City,
                 d.State,
                 d.PostalCode,
                 d.Country,
                 d.SerialNo,
                 d.CompanyId,
                 c != null ? c.Name : null
             )).ToListAsync();


        public async Task<DepartmentResponse> CreateAsync(DepartmentInput input)
        {
            var dept = Apply(new Department(), input);
            db.Departments.Add(dept);
            await db.SaveChangesAsync();
            var companyName = dept.CompanyId != null ? await db.Companies.Where(c => c.Id == dept.CompanyId).Select(c => c.Name).FirstOrDefaultAsync() : null;
            return Map(dept, companyName);
        }


        public async Task<DepartmentResponse?> UpdateAsync(int id, DepartmentInput input)
        {
            var dept = await db.Departments.FindAsync(id);
            if (dept is null) return null;
            Apply(dept, input);
            await db.SaveChangesAsync();
            var companyName = dept.CompanyId != null ? await db.Companies.Where(c => c.Id == dept.CompanyId).Select(c => c.Name).FirstOrDefaultAsync() : null;
            return Map(dept, companyName);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var dept = await db.Departments.FindAsync(id);
            if (dept is null) return false;
            db.Departments.Remove(dept);
            await db.SaveChangesAsync();
            return true;
        }

        private static Department Apply(Department d, DepartmentInput i)
        {
            d.DepartmentName = i.DepartmentName; d.Manager = i.Manager;
            d.PaymentRate = i.PaymentRate; d.AddressLine1 = i.AddressLine1;
            d.AddressLine2 = i.AddressLine2; d.City = i.City;
            d.State = i.State; d.PostalCode = i.PostalCode;
            d.Country = i.Country;
            d.SerialNo = i.SerialNo;
            d.CompanyId = i.CompanyId;
            return d;
        }

        private static DepartmentResponse Map(Department d, string? companyName = null) =>
            new(
                d.Id, d.DepartmentName, d.Manager, (double?)d.PaymentRate,
                d.AddressLine1, d.AddressLine2, d.City, d.State, d.PostalCode, d.Country,
                d.SerialNo,
                d.CompanyId,
                companyName
            );
    }
}

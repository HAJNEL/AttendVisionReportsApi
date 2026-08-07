using System.Collections.Generic;
using System.Threading.Tasks;
using AttendVisionReportsApi.Data;
using Microsoft.EntityFrameworkCore;

namespace AttendVisionReportsApi.Services
{
    public class FilterService : IFilterService
    {
        private readonly AppDbContext _context;
        public FilterService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<EmployeeResult>> GetDepartmentEmployeesAsync(Guid? departmentId, System.Security.Claims.ClaimsPrincipal? user = null)
        {
            var query = _context.Employees.AsQueryable();
            if (departmentId.HasValue)
            {
                query = query.Where(e => e.DepartmentId == departmentId.Value);
            }
            else if (user != null && Helpers.ClaimsHelper.TryGetUserId(user, out var userId, logClaims: false))
            {
                var departmentIds = await _context.DepartmentUsers
                    .Where(du => du.UserId == userId)
                    .Select(du => du.DepartmentId)
                    .ToListAsync();

                query = query.Where(e => e.DepartmentId != null && departmentIds.Contains(e.DepartmentId.Value));
            }

            var result = await query
                .Where(e => !string.IsNullOrWhiteSpace(e.FullName) && !string.IsNullOrWhiteSpace(e.EmployeeNo))
                .Select(e => new EmployeeResult
                {
                    DepartmentId = e.DepartmentId != null ? e.DepartmentId.ToString() : null,
                    Name = e.FullName,
                    EmployeeId = e.EmployeeNo
                })
                .Distinct()
                .OrderBy(x => x.Name)
                .ToListAsync();
            return result;
        }

        public class EmployeeResult
        {
            public string? DepartmentId { get; set; }
            public string? Name { get; set; }
            public string? EmployeeId { get; set; }
        }

        // Helper: get allowed departments for user
        private async Task<List<string>?> GetAllowedDepartmentsAsync(System.Security.Claims.ClaimsPrincipal? user, string? department)
        {
            if (user == null)
            {
                if (!string.IsNullOrEmpty(department))
                    return new List<string> { department };
                return null;
            }

            if (!Helpers.ClaimsHelper.TryGetUserId(user, out var userId, logClaims: false))
                return new List<string>();

            var departmentIds = await _context.DepartmentUsers
                .Where(du => du.UserId == userId)
                .Select(du => du.DepartmentId)
                .ToListAsync();

            if (!departmentIds.Any())
                return new List<string>();

            var departmentNames = await _context.Departments
                .Where(d => departmentIds.Contains(d.Id))
                .Select(d => d.DepartmentName)
                .ToListAsync();

            if (!string.IsNullOrEmpty(department))
            {
                if (departmentNames.Contains(department, StringComparer.OrdinalIgnoreCase))
                {
                    return new List<string> { department };
                }
                else
                {
                    return new List<string>();
                }
            }

            return departmentNames;
        }
    }
}

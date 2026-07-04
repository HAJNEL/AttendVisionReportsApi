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
            var query = _context.AccessRecords.AsQueryable();
            if (departmentId.HasValue)
            {
                query = query.Where(x => x.Department != null && _context.Departments.Any(d => d.Id == departmentId.Value && d.DepartmentName == x.Department));
            }
            else if (user != null && Helpers.ClaimsHelper.TryGetUserId(user, out var userId, logClaims: false))
            {
                var departmentIds = await _context.DepartmentUsers
                    .Where(du => du.UserId == userId)
                    .Select(du => du.DepartmentId)
                    .ToListAsync();

                var departmentNames = await _context.Departments
                    .Where(d => departmentIds.Contains(d.Id))
                    .Select(d => d.DepartmentName)
                    .ToListAsync();

                query = query.Where(x => !string.IsNullOrEmpty(x.Department) && departmentNames.Contains(x.Department));
            }
            query = query.Where(x => !string.IsNullOrWhiteSpace(x.PersonName) && !string.IsNullOrWhiteSpace(x.EmployeeId) && !string.IsNullOrWhiteSpace(x.Department));

            // Get the latest AccessDatetime per EmployeeId so we use the most recent name
            var latestPerEmployee = query
                .GroupBy(ar => ar.EmployeeId)
                .Select(g => new { EmployeeId = g.Key, MaxDatetime = g.Max(ar => ar.AccessDatetime) });

            var result = await (from ar in query
                                join latest in latestPerEmployee
                                    on new { ar.EmployeeId, ar.AccessDatetime }
                                    equals new { latest.EmployeeId, AccessDatetime = latest.MaxDatetime }
                                join d in _context.Departments on ar.Department equals d.DepartmentName
                                select new EmployeeResult
                                {
                                    DepartmentId = d.Id.ToString(),
                                    Name = ar.PersonName,
                                    EmployeeId = ar.EmployeeId
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

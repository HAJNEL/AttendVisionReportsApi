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

        public async Task<IEnumerable<EmployeeResult>> GetDepartmentEmployeesAsync(Guid? departmentId)
        {
            var query = _context.AccessRecords.AsQueryable();
            if (departmentId.HasValue)
            {
                query = query.Where(x => x.Department != null && _context.Departments.Any(d => d.Id == departmentId.Value && d.DepartmentName == x.Department));
            }
            query = query.Where(x => !string.IsNullOrWhiteSpace(x.PersonName) && !string.IsNullOrWhiteSpace(x.EmployeeId) && !string.IsNullOrWhiteSpace(x.Department));

            var result = await (from ar in query
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
            if (!string.IsNullOrEmpty(department))
                return new List<string> { department };
            if (user == null)
                return null;
            if (!Helpers.ClaimsHelper.TryGetUserId(user, out var userId, logClaims: false))
                return null;
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
            return departmentNames;
        }
    }
}

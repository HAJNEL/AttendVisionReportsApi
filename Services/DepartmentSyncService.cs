using AttendVisionReportsApi.Data;
using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendVisionReportsApi.Services
{
    public class DepartmentSyncService(AppDbContext db, IHikCentralService hikCentral) : IDepartmentSyncService
    {
        private const int OrgPageSize = 100;

        public async Task<DepartmentSyncResult> SyncAllAsync(CancellationToken ct = default)
        {
            var warnings = new List<string>();

            var allDepartments = await db.Departments.ToListAsync(ct);
            var departmentsByOrg = allDepartments
                .Where(d => d.HikCentralOrgIndexCode != null)
                .ToDictionary(d => d.HikCentralOrgIndexCode!, d => d);
            // Departments created by hand before this sync existed never got an org
            // index code, so fall back to matching by name to backfill onto them
            // instead of creating a duplicate row.
            var unlinkedByName = allDepartments
                .Where(d => d.HikCentralOrgIndexCode == null)
                .GroupBy(d => d.DepartmentName.Trim().ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.First());

            var allOrgs = await FetchAllOrgsAsync(ct);

            int created = 0, updated = 0;

            foreach (var org in allOrgs)
            {
                if (string.IsNullOrEmpty(org.OrgIndexCode) || string.IsNullOrWhiteSpace(org.OrgName))
                {
                    warnings.Add($"Skipped HikCentral org with missing index code or name (orgIndexCode: {org.OrgIndexCode ?? "null"}).");
                    continue;
                }

                // The org with no real parent is the root of HikCentral's org tree
                // (the whole organization, not a real department/site) - skip it.
                // HikCentral represents "no parent" as "0", not null/empty.
                if (string.IsNullOrEmpty(org.ParentOrgIndexCode) || org.ParentOrgIndexCode == "0")
                {
                    warnings.Add($"Skipped root organization '{org.OrgName}' (orgIndexCode: {org.OrgIndexCode}) - not a department.");
                    continue;
                }

                if (departmentsByOrg.TryGetValue(org.OrgIndexCode, out var department))
                {
                    // Only count/write this as an update when the name actually
                    // changed - otherwise every sync run re-touches every
                    // already-linked department for no reason.
                    if (department.DepartmentName != org.OrgName)
                    {
                        department.DepartmentName = org.OrgName;
                        updated++;
                    }
                }
                else if (unlinkedByName.TryGetValue(org.OrgName.Trim().ToLowerInvariant(), out department))
                {
                    department.HikCentralOrgIndexCode = org.OrgIndexCode;
                    departmentsByOrg[org.OrgIndexCode] = department;
                    unlinkedByName.Remove(org.OrgName.Trim().ToLowerInvariant());
                    updated++; // genuine change: backfilling a previously-missing org index code
                }
                else
                {
                    department = new Department
                    {
                        DepartmentName = org.OrgName,
                        HikCentralOrgIndexCode = org.OrgIndexCode,
                    };
                    db.Departments.Add(department);
                    departmentsByOrg[org.OrgIndexCode] = department;
                    created++;
                }
            }

            await db.SaveChangesAsync(ct);

            return new DepartmentSyncResult(allOrgs.Count, created, updated, warnings);
        }

        private async Task<List<HikCentralOrganization>> FetchAllOrgsAsync(CancellationToken ct)
        {
            var allOrgs = new List<HikCentralOrganization>();
            var pageNo = 1;
            while (true)
            {
                var page = await hikCentral.GetOrganizationsAsync(pageNo, OrgPageSize, ct);
                var list = page.List ?? [];
                allOrgs.AddRange(list);
                if (list.Count == 0 || allOrgs.Count >= page.Total) break;
                pageNo++;
            }
            return allOrgs;
        }
    }
}

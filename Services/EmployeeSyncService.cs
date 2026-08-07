using AttendVisionReportsApi.Data;
using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendVisionReportsApi.Services
{
    public class EmployeeSyncService(AppDbContext db, IHikCentralService hikCentral) : IEmployeeSyncService
    {
        private const int PersonPageSize = 100;
        private const int BatchSize = 50;

        public async Task<EmployeeSyncResult> SyncAllAsync(CancellationToken ct = default)
        {
            var warnings = new List<string>();

            var departmentsByOrg = await db.Departments
                .Where(d => d.HikCentralOrgIndexCode != null)
                .ToDictionaryAsync(d => d.HikCentralOrgIndexCode!, d => d.Id, ct);
            var existingByPersonId = await db.Employees.ToDictionaryAsync(e => e.HikCentralPersonId, ct);

            var allPersons = await FetchAllPersonsAsync(ct);
            var personIds = allPersons
                .Where(p => !string.IsNullOrEmpty(p.PersonId))
                .Select(p => p.PersonId!)
                .ToList();

            var photosByPersonId = await TryLoadPhotosAsync(personIds, warnings, ct);
            var shiftsByPersonId = await TryLoadShiftsAsync(personIds, warnings, ct);

            var now = DateTime.UtcNow;
            int created = 0, updated = 0;
            var syncedEmployees = new List<Employee>();

            foreach (var person in allPersons)
            {
                if (string.IsNullOrEmpty(person.PersonId)) continue;

                if (existingByPersonId.TryGetValue(person.PersonId, out var employee))
                {
                    updated++;
                }
                else
                {
                    employee = new Employee { HikCentralPersonId = person.PersonId };
                    db.Employees.Add(employee);
                    existingByPersonId[person.PersonId] = employee;
                    created++;
                }

                ApplyPersonToEmployee(employee, person, departmentsByOrg);

                if (photosByPersonId.TryGetValue(person.PersonId, out var photo))
                    employee.PhotoBase64 = photo;

                if (shiftsByPersonId.TryGetValue(person.PersonId, out var shift))
                {
                    employee.CurrentShiftName = shift.ShiftName;
                    employee.CurrentShiftOnDuty = shift.OnDutyTime;
                    employee.CurrentShiftOffDuty = shift.OffDutyTime;
                }

                employee.LastSyncedAt = now;
                syncedEmployees.Add(employee);
            }

            await db.SaveChangesAsync(ct);

            await TrySyncAccessLevelsAsync(syncedEmployees, warnings, ct);

            return new EmployeeSyncResult(allPersons.Count, created, updated, warnings);
        }

        private async Task<List<HikCentralPerson>> FetchAllPersonsAsync(CancellationToken ct)
        {
            var allPersons = new List<HikCentralPerson>();
            var pageNo = 1;
            while (true)
            {
                var page = await hikCentral.SearchPersonsAsync(new HikCentralPersonSearchRequest(pageNo, PersonPageSize, null, null), ct);
                var list = page.List ?? [];
                allPersons.AddRange(list);
                if (list.Count == 0 || allPersons.Count >= page.Total) break;
                pageNo++;
            }
            return allPersons;
        }

        private static void ApplyPersonToEmployee(Employee employee, HikCentralPerson person, Dictionary<string, Guid> departmentsByOrg)
        {
            var fullName = !string.IsNullOrWhiteSpace(person.PersonName)
                ? person.PersonName
                : $"{person.PersonGivenName} {person.PersonFamilyName}".Trim();

            employee.EmployeeNo = person.PersonCode;
            employee.FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName;
            employee.FirstName = person.PersonGivenName;
            employee.LastName = person.PersonFamilyName;
            employee.Gender = person.Gender;
            employee.OrgIndexCode = person.OrgIndexCode;
            employee.DepartmentId = person.OrgIndexCode != null && departmentsByOrg.TryGetValue(person.OrgIndexCode, out var deptId)
                ? deptId
                : null;
            employee.Position = person.CustomFields?
                .FirstOrDefault(c => string.Equals(c.Key, "position", StringComparison.OrdinalIgnoreCase))?
                .Value;
            employee.PhoneNo = person.PhoneNo;
            employee.Email = person.Email;
            employee.JobNo = person.JobNo;
            employee.Remark = person.Remark;
            employee.BeginTime = ParseHikTime(person.BeginTime);
            employee.EndTime = ParseHikTime(person.EndTime);
        }

        private async Task<Dictionary<string, string?>> TryLoadPhotosAsync(List<string> personIds, List<string> warnings, CancellationToken ct)
        {
            var photosByPersonId = new Dictionary<string, string?>();
            try
            {
                foreach (var batch in Batch(personIds, BatchSize))
                {
                    var faces = await hikCentral.SearchFacesAsync(batch, ct);
                    foreach (var f in faces.List ?? [])
                        if (!string.IsNullOrEmpty(f.PersonId)) photosByPersonId[f.PersonId] = f.FaceData;
                }
            }
            catch (Exception ex)
            {
                warnings.Add($"Photo sync unavailable: {ex.Message}");
            }
            return photosByPersonId;
        }

        private async Task<Dictionary<string, HikCentralAtsShift>> TryLoadShiftsAsync(List<string> personIds, List<string> warnings, CancellationToken ct)
        {
            var shiftsByPersonId = new Dictionary<string, HikCentralAtsShift>();
            try
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
                foreach (var batch in Batch(personIds, BatchSize))
                {
                    var shifts = await hikCentral.GetAtsSchedulesAsync(batch, today, today, ct);
                    foreach (var s in shifts)
                        if (!string.IsNullOrEmpty(s.PersonId)) shiftsByPersonId[s.PersonId] = s;
                }
            }
            catch (Exception ex)
            {
                warnings.Add($"Attendance schedule sync unavailable: {ex.Message}");
            }
            return shiftsByPersonId;
        }

        private async Task TrySyncAccessLevelsAsync(List<Employee> employees, List<string> warnings, CancellationToken ct)
        {
            try
            {
                var employeeIds = employees.Select(e => e.Id).ToList();
                var existing = await db.EmployeeAccessLevels.Where(a => employeeIds.Contains(a.EmployeeId)).ToListAsync(ct);
                db.EmployeeAccessLevels.RemoveRange(existing);

                foreach (var employee in employees)
                {
                    HikCentralPrivilegeByPersonData? privileges;
                    try
                    {
                        privileges = await hikCentral.GetPersonPrivilegesAsync(employee.HikCentralPersonId, ct);
                    }
                    catch (Exception ex)
                    {
                        warnings.Add($"Access level lookup failed for person {employee.HikCentralPersonId}: {ex.Message}");
                        continue;
                    }

                    foreach (var p in privileges?.PersonPrivilegeList ?? [])
                    {
                        db.EmployeeAccessLevels.Add(new EmployeeAccessLevel
                        {
                            EmployeeId = employee.Id,
                            TemplateId = p.TemplateId,
                            TemplateName = p.TemplateName,
                        });
                    }
                }

                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                warnings.Add($"Access level sync unavailable: {ex.Message}");
            }
        }

        private static IEnumerable<List<string>> Batch(List<string> source, int size)
        {
            for (var i = 0; i < source.Count; i += size)
                yield return source.Skip(i).Take(size).ToList();
        }

        private static DateTime? ParseHikTime(string? value) =>
            DateTime.TryParse(value, out var dt) ? dt : null;
    }
}

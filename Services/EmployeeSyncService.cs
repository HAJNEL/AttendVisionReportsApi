using AttendVisionReportsApi.Data;
using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendVisionReportsApi.Services
{
    public class EmployeeSyncService(AppDbContext db, IHikCentralService hikCentral, IDepartmentSyncService departmentSync, IConfiguration configuration) : IEmployeeSyncService
    {
        private const int PersonPageSize = 100;
        private const int BatchSize = 50;

        public async Task<EmployeeSyncResult> SyncAllAsync(CancellationToken ct = default)
        {
            var warnings = new List<string>();

            // Departments must exist (and carry a HikCentralOrgIndexCode) before
            // persons can be linked to them, so sync departments first rather than
            // requiring a separate manual "Sync with HikCentral" on the Departments
            // page beforehand.
            try
            {
                var deptResult = await departmentSync.SyncAllAsync(ct);
                warnings.AddRange(deptResult.Warnings.Select(w => $"Department sync: {w}"));
            }
            catch (Exception ex)
            {
                warnings.Add($"Department sync failed, employees may be linked to stale departments: {ex.Message}");
            }

            var departmentsByOrg = await db.Departments
                .Where(d => d.HikCentralOrgIndexCode != null)
                .ToDictionaryAsync(d => d.HikCentralOrgIndexCode!, d => d.Id, ct);
            var existingByPersonId = await db.Employees.ToDictionaryAsync(e => e.HikCentralPersonId, ct);

            var allPersons = await FetchAllPersonsAsync(ct);
            var personIds = allPersons
                .Where(p => !string.IsNullOrEmpty(p.PersonId))
                .Select(p => p.PersonId!)
                .ToList();

            var photosByPersonId = await TryLoadPhotosAsync(allPersons, warnings, ct);
            var shiftsByPersonId = await TryLoadShiftsAsync(personIds, warnings, ct);
            var jobTitlesByPersonId = await TryLoadJobTitlesAsync(warnings, ct);

            var now = DateTime.UtcNow;
            var syncedEmployees = new List<Employee>();

            foreach (var person in allPersons)
            {
                if (string.IsNullOrEmpty(person.PersonId)) continue;

                if (!existingByPersonId.TryGetValue(person.PersonId, out var employee))
                {
                    employee = new Employee { HikCentralPersonId = person.PersonId };
                    db.Employees.Add(employee);
                    existingByPersonId[person.PersonId] = employee;
                }

                ApplyPersonToEmployee(employee, person, departmentsByOrg);

                if (jobTitlesByPersonId.TryGetValue(person.PersonId, out var jobTitle) && !string.IsNullOrWhiteSpace(jobTitle))
                    employee.Position = jobTitle;

                if (photosByPersonId.TryGetValue(person.PersonId, out var photo))
                    employee.PhotoBase64 = photo;

                if (shiftsByPersonId.TryGetValue(person.PersonId, out var shift))
                {
                    employee.CurrentShiftName = shift.ShiftName;
                    employee.CurrentShiftOnDuty = shift.OnDutyTime;
                    employee.CurrentShiftOffDuty = shift.OffDutyTime;
                }

                syncedEmployees.Add(employee);
            }

            // Detect which rows actually differ from what's stored before touching
            // LastSyncedAt - otherwise that field alone would make every employee
            // look "changed" and get rewritten on every sync run, regardless of
            // whether any real data changed.
            db.ChangeTracker.DetectChanges();

            int created = 0, updated = 0;
            foreach (var employee in syncedEmployees)
            {
                var state = db.Entry(employee).State;
                if (state == EntityState.Added)
                {
                    employee.LastSyncedAt = now;
                    created++;
                }
                else if (state == EntityState.Modified)
                {
                    employee.LastSyncedAt = now;
                    updated++;
                }
                // else Unchanged - leave the row (and LastSyncedAt) untouched.
            }

            await db.SaveChangesAsync(ct);

            await TrySyncAccessLevelsAsync(syncedEmployees, warnings, ct);

            return new EmployeeSyncResult(allPersons.Count, created, updated, warnings);
        }

        public async Task<EmployeeListItem> CreateAsync(EmployeeCreateRequest request, CancellationToken ct = default)
        {
            var department = await db.Departments.FindAsync([request.DepartmentId], ct)
                ?? throw new InvalidOperationException("Department not found.");
            if (string.IsNullOrEmpty(department.HikCentralOrgIndexCode))
                throw new InvalidOperationException($"'{department.DepartmentName}' isn't linked to a HikCentral organization yet — set its HikCentral Org Index Code first.");

            var now = DateTime.UtcNow;
            var addRequest = new HikCentralAddPersonRequest(
                request.EmployeeNo,
                request.LastName,
                request.FirstName,
                department.HikCentralOrgIndexCode,
                request.Gender,
                request.PhoneNo,
                request.Email,
                request.Remark,
                ToHikTime(now),
                ToHikTime(now.AddYears(10))
            );
            var personId = await hikCentral.AddPersonAsync(addRequest, ct);

            var employee = new Employee
            {
                HikCentralPersonId = personId,
                EmployeeNo = request.EmployeeNo,
                FirstName = request.FirstName,
                LastName = request.LastName,
                FullName = $"{request.FirstName} {request.LastName}".Trim(),
                Gender = request.Gender,
                DepartmentId = department.Id,
                OrgIndexCode = department.HikCentralOrgIndexCode,
                PhoneNo = request.PhoneNo,
                Email = request.Email,
                Remark = request.Remark,
                // Photo is local-only, same as UpdateAsync below - never pushed to
                // HikCentral (its face/photo APIs aren't licensed on this deployment).
                PhotoBase64 = request.PhotoBase64,
                BeginTime = now,
                EndTime = now.AddYears(10),
                LastSyncedAt = now,
            };
            db.Employees.Add(employee);
            await db.SaveChangesAsync(ct);

            return MapToListItem(employee, department.DepartmentName, []);
        }

        public async Task<EmployeeListItem> UpdateAsync(Guid id, EmployeeUpdateRequest request, CancellationToken ct = default)
        {
            var employee = await db.Employees.FindAsync([id], ct)
                ?? throw new InvalidOperationException("Employee not found.");

            var departmentId = request.DepartmentId ?? employee.DepartmentId;
            Department? department = null;
            if (departmentId.HasValue)
            {
                department = await db.Departments.FindAsync([departmentId.Value], ct)
                    ?? throw new InvalidOperationException("Department not found.");
                if (string.IsNullOrEmpty(department.HikCentralOrgIndexCode))
                    throw new InvalidOperationException($"'{department.DepartmentName}' isn't linked to a HikCentral organization yet — set its HikCentral Org Index Code first.");
            }

            var updateRequest = new HikCentralUpdatePersonRequest(
                employee.HikCentralPersonId,
                request.LastName,
                request.FirstName,
                department?.HikCentralOrgIndexCode,
                request.Gender,
                request.PhoneNo,
                request.Email,
                request.JobNo
            );
            await hikCentral.UpdatePersonAsync(updateRequest, ct);

            // Photo is local-only - not pushed to HikCentral (its face/photo
            // APIs aren't available on this deployment's license, and the
            // requirement here is just to persist it in Postgres).
            if (!string.IsNullOrEmpty(request.PhotoBase64))
                employee.PhotoBase64 = request.PhotoBase64;

            employee.FirstName = request.FirstName;
            employee.LastName = request.LastName;
            employee.FullName = $"{request.FirstName} {request.LastName}".Trim();
            employee.Gender = request.Gender;
            if (department != null)
            {
                employee.DepartmentId = department.Id;
                employee.OrgIndexCode = department.HikCentralOrgIndexCode;
            }
            employee.PhoneNo = request.PhoneNo;
            employee.Email = request.Email;
            employee.JobNo = request.JobNo;
            employee.LastSyncedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            var departmentName = department?.DepartmentName ?? (employee.DepartmentId.HasValue
                ? await db.Departments.Where(d => d.Id == employee.DepartmentId).Select(d => d.DepartmentName).FirstOrDefaultAsync(ct)
                : null);
            var accessLevelNames = await db.EmployeeAccessLevels
                .Where(a => a.EmployeeId == employee.Id)
                .Select(a => a.TemplateName ?? a.TemplateId ?? "")
                .Where(n => n != "")
                .ToListAsync(ct);

            return MapToListItem(employee, departmentName, accessLevelNames);
        }

        // Called only from TempEmployeeService.ApproveAsync for a "Delete"
        // request - the HikCentral delete happens first, so a failure there
        // (e.g. wrong/missing admin password) leaves both the local employee
        // row and the pending temp_employees row untouched for a retry,
        // rather than deleting locally and silently drifting from HikCentral.
        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var employee = await db.Employees.FindAsync([id], ct)
                ?? throw new InvalidOperationException("Employee not found.");

            await hikCentral.DeletePersonAsync(employee.HikCentralPersonId, ct);

            db.Employees.Remove(employee);
            await db.SaveChangesAsync(ct);
        }

        // Debug/test button on the Employees page - fetches as much information
        // as possible about one employee: everything already stored locally,
        // plus every live HikCentral call the sync pipeline makes (or could
        // make) for them, without writing anything, so the raw shapes can be
        // inspected in the browser console.
        public async Task<EmployeeHikCentralTestResult> TestFetchAsync(Guid id, CancellationToken ct = default)
        {
            var employee = await db.Employees.FindAsync([id], ct)
                ?? throw new InvalidOperationException("Employee not found.");

            var departmentName = employee.DepartmentId.HasValue
                ? await db.Departments.Where(d => d.Id == employee.DepartmentId).Select(d => d.DepartmentName).FirstOrDefaultAsync(ct)
                : null;
            var storedAccessLevelNames = await db.EmployeeAccessLevels
                .Where(a => a.EmployeeId == employee.Id)
                .Select(a => a.TemplateName ?? a.TemplateId ?? "")
                .Where(n => n != "")
                .ToListAsync(ct);
            var localRecord = MapToListItem(employee, departmentName, storedAccessLevelNames);

            HikCentralPerson? v1Person = null;
            string? v1Error = null;
            try
            {
                v1Person = await hikCentral.GetPersonAsync(employee.HikCentralPersonId, ct);
            }
            catch (Exception ex)
            {
                v1Error = ex.Message;
            }

            HikCentralPerson? v2Person = null;
            string? v2Error = null;
            try
            {
                var pageNo = 1;
                while (true)
                {
                    var page = await hikCentral.SearchPersonsV2Async(
                        new HikCentralPersonSearchRequest(pageNo, PersonPageSize, employee.OrgIndexCode, null), ct);
                    var list = page.List ?? [];
                    v2Person = list.FirstOrDefault(p => p.PersonId == employee.HikCentralPersonId);
                    if (v2Person != null || list.Count == 0 || pageNo * PersonPageSize >= page.Total) break;
                    pageNo++;
                }
                if (v2Person is null)
                    v2Error = "Person not found via v2 personList (checked all pages for its org).";
            }
            catch (Exception ex)
            {
                v2Error = ex.Message;
            }

            List<HikCentralPersonPrivilege> accessLevels = [];
            string? accessLevelsError = null;
            try
            {
                var privileges = await hikCentral.GetPersonPrivilegesAsync(employee.HikCentralPersonId, ct);
                accessLevels = privileges?.PersonPrivilegeList ?? [];
            }
            catch (Exception ex)
            {
                accessLevelsError = ex.Message;
            }

            // Documented approach (HIK_API/HikCentral-OpenAPI-Endpoints.md): no
            // reverse by-person lookup exists, so list every access level
            // (group), then check membership of this person in each one. Capped
            // so one Test click can't run away against a deployment with a huge
            // number of groups.
            const int maxGroupsToCheck = 100;
            const int maxPersonPagesPerGroup = 10;
            List<string> accessLevelsViaGroups = [];
            string? accessLevelsViaGroupsError = null;
            try
            {
                var groups = await hikCentral.GetAccessLevelsAsync(1, maxGroupsToCheck, ct);
                foreach (var group in groups.List ?? [])
                {
                    if (string.IsNullOrEmpty(group.PrivilegeGroupId)) continue;

                    var pageNo = 1;
                    while (pageNo <= maxPersonPagesPerGroup)
                    {
                        var page = await hikCentral.GetAccessLevelPersonsAsync(group.PrivilegeGroupId, pageNo, 200, ct);
                        var list = page.List ?? [];
                        if (list.Any(p => p.Id == employee.HikCentralPersonId))
                        {
                            accessLevelsViaGroups.Add(group.PrivilegeGroupName ?? group.PrivilegeGroupId);
                            break;
                        }
                        if (list.Count == 0 || pageNo * 200 >= page.Total) break;
                        pageNo++;
                    }
                }
            }
            catch (Exception ex)
            {
                accessLevelsViaGroupsError = ex.Message;
            }

            string? livePhotoBase64 = null;
            string? livePhotoError = null;
            try
            {
                var picUri = v1Person?.PersonPhoto?.PicUri;
                if (!string.IsNullOrEmpty(picUri))
                {
                    var bytes = await hikCentral.GetPersonPictureAsync(employee.HikCentralPersonId, picUri, ct);
                    if (bytes is { Length: > 0 })
                        livePhotoBase64 = Convert.ToBase64String(bytes);
                    else
                        livePhotoError = "HikCentral returned an empty image.";
                }
                else
                {
                    livePhotoError = "No personPhoto.picUri on the v1 person record.";
                }
            }
            catch (Exception ex)
            {
                livePhotoError = ex.Message;
            }

            HikCentralAtsShift? shift = null;
            string? shiftError = null;
            try
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
                var shifts = await hikCentral.GetAtsSchedulesAsync([employee.HikCentralPersonId], today, today, ct);
                shift = shifts.FirstOrDefault();
            }
            catch (Exception ex)
            {
                shiftError = ex.Message;
            }

            System.Text.Json.JsonElement? attendanceGroup = null;
            string? attendanceGroupError = null;
            try
            {
                attendanceGroup = await hikCentral.GetPersonAttendanceGroupAsync(employee.HikCentralPersonId, ct);
            }
            catch (Exception ex)
            {
                attendanceGroupError = ex.Message;
            }

            // Real documented replacement for the ats/v1/schedule/list and
            // ats/v1/attendance/person/group calls above (both confirmed dead on
            // this deployment) - see HikCentralAttendanceReportRequestBody in
            // HikCentralDtos.cs. Scoped to today only, matching the shift/
            // attendanceGroup calls it replaces.
            //
            // Four date formats, then five body-content variants (sortInfo
            // presence, personID as string/number/empty/personCode) all failed
            // with the *identical* "beginTime parameter error" - including a
            // trial matching the guide's own example byte-for-byte. That level
            // of insensitivity to the body means the body isn't what's actually
            // being validated. This round tests the other two candidates:
            // whether the "userId" header value/presence matters, and whether
            // the "attendanceReportRequest" wrapper or beginTime/endTime's
            // position (inside vs. outside queryInfo) is wrong instead.
            var localToday = DateTimeOffset.Now.ToString("yyyy-MM-dd");
            var offsetColon = DateTimeOffset.Now.ToString("zzz");
            var beginTime = $"{localToday}T00:00:00{offsetColon}";
            var endTime = $"{localToday}T23:59:59{offsetColon}";
            var personId = employee.HikCentralPersonId;
            var configuredUserId = configuration["HikCentral:UserId"] ?? "";

            var attendanceFormatCandidates = new (string Label, string RequestJson, string? UserIdOverride, bool IncludeUserIdHeader)[]
            {
                ("no userId header at all",
                    $"{{\"attendanceReportRequest\":{{\"pageNo\":1,\"pageSize\":100,\"queryInfo\":{{\"personID\":[\"{personId}\"],\"beginTime\":\"{beginTime}\",\"endTime\":\"{endTime}\"}}}}}}",
                    null, false),
                ("userId=1 (numeric platform id guess)",
                    $"{{\"attendanceReportRequest\":{{\"pageNo\":1,\"pageSize\":100,\"queryInfo\":{{\"personID\":[\"{personId}\"],\"beginTime\":\"{beginTime}\",\"endTime\":\"{endTime}\"}}}}}}",
                    "1", true),
                ("no attendanceReportRequest wrapper (flat body)",
                    $"{{\"pageNo\":1,\"pageSize\":100,\"queryInfo\":{{\"personID\":[\"{personId}\"],\"beginTime\":\"{beginTime}\",\"endTime\":\"{endTime}\"}}}}",
                    null, true),
                ("beginTime/endTime outside queryInfo (sibling of pageNo)",
                    $"{{\"attendanceReportRequest\":{{\"pageNo\":1,\"pageSize\":100,\"beginTime\":\"{beginTime}\",\"endTime\":\"{endTime}\",\"queryInfo\":{{\"personID\":[\"{personId}\"]}}}}}}",
                    null, true),
            };

            List<HikCentralAttendanceRecord> attendanceReport = [];
            string? attendanceReportError = null;
            string? attendanceReportRequestJson = null;
            var attendanceReportFormatTrials = new Dictionary<string, string>
            {
                ["configured userId"] = configuredUserId
            };

            foreach (var (label, requestJson, userIdOverride, includeUserIdHeader) in attendanceFormatCandidates)
            {
                try
                {
                    var raw = await hikCentral.PostAttendanceReportRawAsync(requestJson, userIdOverride, includeUserIdHeader, ct);
                    var envelope = System.Text.Json.JsonSerializer.Deserialize<ArtemisResponse<HikCentralAttendanceReportData>>(raw);
                    if (envelope?.Code == "0")
                    {
                        var records = envelope.Data?.Record ?? [];
                        attendanceReportFormatTrials[label] = $"OK: {records.Count} record(s) | sent: {requestJson} | raw: {raw}";
                        if (attendanceReportError is not null || attendanceReport.Count == 0)
                        {
                            attendanceReport = records;
                            attendanceReportError = null;
                            attendanceReportRequestJson = requestJson;
                        }
                    }
                    else
                    {
                        attendanceReportFormatTrials[label] = $"ERROR: raw: {raw} | sent: {requestJson}";
                        if (attendanceReportRequestJson is null)
                        {
                            attendanceReportError = $"HikCentral API error {envelope?.Code}: {envelope?.Msg}";
                            attendanceReportRequestJson = requestJson;
                        }
                    }
                }
                catch (Exception ex)
                {
                    attendanceReportFormatTrials[label] = $"ERROR: {ex.Message} | sent: {requestJson}";
                    if (attendanceReportRequestJson is null)
                    {
                        attendanceReportError = ex.Message;
                        attendanceReportRequestJson = requestJson;
                    }
                }
            }

            var employeeName = employee.FullName ?? v1Person?.PersonName;

            return new EmployeeHikCentralTestResult(
                employeeName, localRecord, v1Person, v1Error, v2Person, v2Error,
                accessLevels, accessLevelsError, accessLevelsViaGroups, accessLevelsViaGroupsError,
                livePhotoBase64, livePhotoError, shift, shiftError, attendanceGroup, attendanceGroupError,
                attendanceReport, attendanceReportError, attendanceReportRequestJson, attendanceReportFormatTrials);
        }

        private static EmployeeListItem MapToListItem(Employee e, string? departmentName, List<string> accessLevelNames) =>
            new(
                e.Id,
                e.HikCentralPersonId,
                e.EmployeeNo,
                e.FullName,
                e.FirstName,
                e.LastName,
                e.Gender,
                e.DepartmentId,
                departmentName,
                e.Position,
                e.OrgIndexCode,
                e.PhoneNo,
                e.Email,
                e.JobNo,
                e.PhotoBase64,
                e.CurrentShiftName,
                e.CurrentShiftOnDuty,
                e.CurrentShiftOffDuty,
                accessLevelNames,
                e.BeginTime,
                e.EndTime,
                e.LastSyncedAt
            );

        // Postgres timestamptz columns already store these as UTC (see the
        // ParseHikTime Kind-normalization above) - this just formats that UTC
        // instant as the ISO8601-with-offset string HikCentral's Add/Update
        // person APIs expect.
        private static string ToHikTime(DateTime utc) =>
            new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToString("yyyy-MM-ddTHH:mm:sszzz");

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

        // v1's personList (used by FetchAllPersonsAsync) doesn't return jobTitle
        // on this deployment even though it's documented elsewhere - v2 does, so
        // it's fetched as a separate pass and merged in by personId rather than
        // switching the main sync over to an endpoint that isn't otherwise
        // verified against this HikCentral edition/license.
        private async Task<Dictionary<string, string?>> TryLoadJobTitlesAsync(List<string> warnings, CancellationToken ct)
        {
            var jobTitlesByPersonId = new Dictionary<string, string?>();
            try
            {
                var pageNo = 1;
                while (true)
                {
                    var page = await hikCentral.SearchPersonsV2Async(new HikCentralPersonSearchRequest(pageNo, PersonPageSize, null, null), ct);
                    var list = page.List ?? [];
                    foreach (var person in list)
                        if (!string.IsNullOrEmpty(person.PersonId)) jobTitlesByPersonId[person.PersonId] = person.JobTitle;
                    if (list.Count == 0 || jobTitlesByPersonId.Count >= page.Total) break;
                    pageNo++;
                }
            }
            catch (Exception ex)
            {
                warnings.Add($"Position (jobTitle) sync unavailable: {ex.Message}");
            }
            return jobTitlesByPersonId;
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
            // HikCentral's UI "Position" field maps to the person's jobTitle;
            // fall back to a "position" custom field for deployments/API
            // versions that expose it that way instead.
            employee.Position = !string.IsNullOrWhiteSpace(person.JobTitle)
                ? person.JobTitle
                : person.CustomFields?
                    .FirstOrDefault(c => string.Equals(c.Key, "position", StringComparison.OrdinalIgnoreCase))?
                    .Value;
            employee.PhoneNo = person.PhoneNo;
            employee.Email = person.Email;
            employee.JobNo = person.JobNo;
            employee.Remark = person.Remark;
            employee.BeginTime = ParseHikTime(person.BeginTime);
            employee.EndTime = ParseHikTime(person.EndTime);
        }

        private async Task<Dictionary<string, string?>> TryLoadPhotosAsync(List<HikCentralPerson> persons, List<string> warnings, CancellationToken ct)
        {
            var photosByPersonId = new Dictionary<string, string?>();
            var withPhoto = persons.Where(p => !string.IsNullOrEmpty(p.PersonId) && !string.IsNullOrEmpty(p.PersonPhoto?.PicUri)).ToList();

            var failureCount = 0;
            foreach (var person in withPhoto)
            {
                try
                {
                    var bytes = await hikCentral.GetPersonPictureAsync(person.PersonId!, person.PersonPhoto!.PicUri!, ct);
                    if (bytes is { Length: > 0 })
                        photosByPersonId[person.PersonId!] = Convert.ToBase64String(bytes);
                }
                catch (Exception ex)
                {
                    failureCount++;
                    // Cap individual entries so one unsupported/unauthorized
                    // endpoint doesn't flood the sync result with one near-identical
                    // warning per person.
                    if (failureCount <= 3)
                        warnings.Add($"Photo fetch failed for person {person.PersonId}: {ex.Message}");
                }
            }
            if (failureCount > 3)
                warnings.Add($"...and {failureCount - 3} more photo fetch failures.");

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
                var existingByEmployee = (await db.EmployeeAccessLevels
                        .Where(a => employeeIds.Contains(a.EmployeeId))
                        .ToListAsync(ct))
                    .GroupBy(a => a.EmployeeId)
                    .ToDictionary(g => g.Key, g => g.ToList());

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

                    var incoming = privileges?.PersonPrivilegeList ?? [];
                    existingByEmployee.TryGetValue(employee.Id, out var current);
                    current ??= [];

                    // Only rewrite this employee's rows when the assigned template
                    // set actually differs - avoids deleting/recreating every
                    // employee's access levels on every sync just to end up with
                    // the same rows.
                    var currentTemplateIds = current.Select(c => c.TemplateId).ToHashSet();
                    var incomingTemplateIds = incoming.Select(p => p.TemplateId).ToHashSet();
                    if (currentTemplateIds.SetEquals(incomingTemplateIds))
                        continue;

                    db.EmployeeAccessLevels.RemoveRange(current);
                    foreach (var p in incoming)
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

        private static DateTime? ParseHikTime(string? value)
        {
            if (!DateTime.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                return null;

            // Postgres' timestamptz columns reject anything but Kind=Utc. HikCentral's
            // timestamps carry a UTC offset, which DateTime.TryParse converts to
            // Kind=Local (the server's local zone) by default - normalize instead of
            // letting Npgsql throw on save.
            return dt.Kind switch
            {
                DateTimeKind.Utc => dt,
                DateTimeKind.Local => dt.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            };
        }
    }
}

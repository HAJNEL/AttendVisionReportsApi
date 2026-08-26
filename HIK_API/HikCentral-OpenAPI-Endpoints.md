# HikCentral Professional OpenAPI — Endpoint Reference

Built from `HikCentral Professional OpenAPI Developer Guide` (the official Hikvision PDF the user
supplied — 542 pages, `softVersion` example in the doc is `V2.1.0.0`), for reference while working on
this project's HikCentral integration (`Services/HikCentralService.cs`, `Services/HikCentral/`).

This is **not** a replacement for `HIK_API/openapi-api.yaml` (the raw OpenAPI capability dump used
elsewhere in this repo) — it's a curated, human-readable version of the subset of endpoints this
project actually calls or is likely to need, pulled from the authoritative developer guide rather than
guessed at. See [Findings for this project](#findings-for-this-project-read-this-first) below before
changing any HikCentral integration code — it corrects some wrong assumptions already baked into
`HikCentralService.cs`.

All requests are `POST` (except where noted), to `https://[serverAddress]:[serverPort]/artemis/...`,
signed per the AK/SK scheme implemented in `ArtemisSigningHandler.cs`.

---

## Findings for this project (read this first)

Cross-checking this guide against `Services/HikCentralService.cs` and `Services/Interfaces/IHikCentralService.cs`
surfaced several concrete mismatches, found 2026-08-20:

1. **Error code `8` does *not* mean "unauthorized API".** It's a documented, specific code (see
   [Status or Error Code](#status-or-error-code) below): `8 = "This product version is not supported."`
   — distinct from the actual permission-denied codes (`17`, `0x02401007`, `0x00072201`/`0x00072202`).
   A prior debugging session in this project incorrectly concluded code `8` meant an OpenAPI Gateway
   authorization gap — that should be treated as **unconfirmed**, not settled. It genuinely can mean
   the specific API isn't supported by this HikCentral edition/version, *or* that the endpoint being
   called doesn't exist (see next point) — both produce the same generic code.

2. **`GetPersonPrivilegesAsync` calls a path that isn't in this guide at all.** The code POSTs to
   `/artemis/api/ac/v1/privilege/byPerson` (note `ac/v1`, not `acs/v1`). This guide's Access Control
   API (§5.8) has no `ac/v1` prefix anywhere and no person→privileges reverse lookup. The documented
   way to find a person's access levels is the *other* direction: `POST /artemis/api/acs/v1/privilege/group`
   to list all access levels, then `POST /artemis/api/acs/v1/privilege/group/single/personList` per
   group to see which persons are members — i.e. enumerate groups and check membership, not a single
   per-person call. This alone would explain the "not supported" error regardless of any licensing
   question, since the called endpoint doesn't exist under that path.

3. **`jobTitle` is not a documented field anywhere in this guide.** Searched the full extracted text:
   zero matches for `jobTitle`. The `PersonInfo` object (§A.1.62, the authoritative field list for
   `personList`/`personInfo` responses) has no `jobTitle`, no `jobNo`, and no `position` field at all
   — its full field set is `personId, personCode, personName, personFamilyName, personGivenName,
   gender, orgIndexCode, fingerPrint, phoneNo, personPhoto, email, remark, cards, beginTime, endTime,
   customFieldList`. `SearchPersonsV2Async` (added on the assumption v2 carries `jobTitle`) has no
   basis in this guide either — there's no `v2/person/personList` path documented at all. HikCentral's
   "Position" dropdown (seen in the admin UI, values like "Permanent"/"Casual") is most likely a
   **custom field** — `CustomField.customFieldType == 3` is documented as "single selection", which
   matches a dropdown UI exactly (see [CustomField](#customfield)). The existing fallback logic in
   `EmployeeSyncService.ApplyPersonToEmployee` that looks for a custom field named `"position"` was
   on the right track; it just needs `customFieldList` to actually come back non-null for the
   relevant employee (it was `null` in the one live test we ran — worth checking whether that
   specific employee's custom field is actually populated via the API vs. only visible in the UI, or
   whether the field is `isShow: false` / a different name).

4. **No Attendance/Time & Attendance (ATS) module exists in this guide at all.** Searched the full
   extracted text for `attendance`/`ats/v1`: two incidental "Attendance type" field mentions inside
   unrelated event structures, nothing else. `GetAtsSchedulesAsync` (`/artemis/api/ats/v1/schedule/list`)
   and the attendance-group endpoints from the user's other source (`/artemis/api/ats/v1/attendance/...`)
   are **not part of this OpenAPI Developer Guide's scope**. HikCentral Time & Attendance is very
   likely licensed/documented as a separate module with its own separate developer guide this PDF
   doesn't include — this explains why every `ats/v1` call returns the same "not supported" error
   while every `resource/v1/person/*` call succeeds. If attendance-group/shift data is needed, the
   next step is finding *that* guide, not further guessing at `ats/v1/*` paths against this one.

5. **`jobNo` isn't documented on Add/Update Person either.** `POST /artemis/api/resource/v1/person/single/update`'s
   documented body is `personId, personCode, personGivenName, personFamilyName, gender, orgIndexCode,
   phoneNo, email, remark, cards, beginTime, endTime, residentRoomNo, residentFloorNo` — no `jobNo`.
   Same for `single/add`. The `JobNo` field wired into `HikCentralAddPersonRequest`/`HikCentralUpdatePersonRequest`
   in `DTOs/HikCentralDtos.cs` isn't backed by anything in this guide.

6. **`POST /artemis/api/attendance/v1/report` is real — found 2026-08-21, but confirmed dead on this
   deployment.** Not in this guide (point 4 above still holds for *this* PDF), but documented in a
   later **HikCentral Professional OpenAPI V3.0.1 Developer Guide** (§5.14.1) and shown in the
   official tpp.hikvision.com Time & Attendance integration page. Full request/response spec
   implemented in `HikCentralService.GetAttendanceReportAsync`. Requires a `userId` HTTP header (a
   HikCentral platform user's ID, not the AK/SK key — see `HikCentral:UserId` in appsettings).
   **Tried 13 request-shape variations (4 date formats, 5 body-content variants, 4 header/structure
   variants) — every one fails with the byte-for-byte identical `error 2: "beginTime parameter
   error"`.** That total insensitivity to the actual request rules out a request-shape bug; it's the
   same "not really supported" signature as point 4's `ats/v1/*` calls, just surfaced through a buggy
   validator instead of a clean "not supported" code. See `[[hikcentral_attendance_report_endpoint]]`
   memory for the full trial log. Don't re-guess formats for this endpoint without new evidence
   (e.g. confirmation the T&A module is actually licensed on this deployment).

None of the above is a reason to rip out existing code — HikCentral's live behavior has already
diverged from this guide in places we've *confirmed* work (e.g. `orgIndexCode` shows up in the real
`personList` response even though some example payloads in this doc omit it). Treat this section as
a prioritized list of things to re-verify against the live API before spending more time guessing.

---

## Authentication

AK/SK (AppKey/AppSecret) request signing — HMAC-SHA256, Alibaba Cloud API Gateway style. Already
implemented in `Services/HikCentral/ArtemisSigningHandler.cs`. Headers: `X-Ca-Key`, `X-Ca-Timestamp`,
`X-Ca-Signature-Headers`, `X-Ca-Signature`, plus `Content-MD5` when a body is present.

Every response wraps in the same envelope:

```json
{
  "code": "0",
  "msg": "Success",
  "data": { }
}
```

`code != "0"` means failure — `msg` is a human string, but the *code* itself is authoritative (see
[Status or Error Code](#status-or-error-code)).

---

## Full endpoint index

Grouped exactly as the guide's Chapter 5 (API Reference) organizes them. Method is `POST` for all
unless noted.

### 5.1 Common API
| Path | Purpose |
|---|---|
| `/artemis/api/common/v1/version` | Get platform product name + version (e.g. `HikCentral Professional V2.1.0.0`) — useful for confirming what edition/version is actually installed when an endpoint claims "not supported". |

### 5.2 Physical Resources API
| Path | Purpose |
|---|---|
| `/artemis/api/resource/v1/acsDevice/acsDeviceList` | List access control devices. |
| `/artemis/api/resource/v1/acsDevice/indexCode/acsDeviceInfo` | Get one access control device by ID. |
| `/artemis/api/resource/v1/acsDevice/advance/acsDeviceList` | Search access control devices. |
| `/artemis/api/resource/v1/encodeDevice/encodeDeviceList` | List encoding devices. |
| `/artemis/api/resource/v1/encodeDevice/indexCode/encodeDeviceInfo` | Get one encoding device by ID. |
| `/artemis/api/resource/v1/encodeDevice/advance/encodeDeviceList` | Search encoding devices. |
| `/artemis/api/resource/v1/device/indexCode/wakeUp` | Wake up a (solar) device. |
| `/artemis/api/resource/v1/intelligentServer/intelligentServerList` | List DeepinMind/intelligent servers. |
| `/artemis/api/resource/v1/mobileDevice/mobileDeviceList` | List mobile devices. |
| `/artemis/api/resource/v1/mobileDevice/indexCode/mobileDeviceInfo` | Get one mobile device. |
| `/artemis/api/resource/v1/mobileDevice/advance/mobileDeviceList` | Search mobile devices. |
| `/artemis/api/resource/v1/recordServer/recordServerList` | List recording servers. |
| `/artemis/api/resource/v1/recordServer/indexCode/recordServerInfo` | Get one recording server. |
| `/artemis/api/resource/v1/recordServer/recordStatus` | Get recording status. |
| `/artemis/api/resource/v1/streamServer/streamServerList` | List streaming servers. |
| `/artemis/api/resource/v1/videoManagementServer` | System management server info. |

### 5.3 Logical Resources API
Sub-sections, each with several endpoints (not enumerated individually here — see detailed sections
below for the two used by this project):
- 5.3.1 Site Information
- 5.3.2 Area Information
- 5.3.3 Camera Information
- **5.3.4 Organization Information** — see [detail](#organization-information-534)
- 5.3.5 Access Point Information
- 5.3.6 Vehicle Information
- 5.3.7 Vehicle Linked to On-Board Device
- **5.3.8 Person Information** — see [detail](#person-information-538)
- 5.3.9 Face Comparison Group
- 5.3.10 Face Information
- 5.3.11 Alarm Input/Output Information

### 5.4 Video API
| Path | Purpose |
|---|---|
| `/artemis/api/aiapplication/v1/people/statisticsTotalNumByTime` | People-counting stats by time. |
| `/artemis/api/aiapplication/v1/people/resourceGroupRealTimeCount` | Real-time people count by resource group. |
| `/artemis/api/aiapplication/v1/people/advance/resourceGroupList` | Search resource groups. |
| `/artemis/api/aiapplication/v1/people/statisticsHeatMapByTime` | Heat map stats by time. |
| `/artemis/api/video/v1/cameras/playbackURLs` | Get playback URLs. |
| `/artemis/api/video/v1/cameras/previewURLs` / `v2/cameras/previewURLs` | Get live preview URLs (v1 and v2). |
| `/artemis/api/video/v1/cameras/talkURLs` | Get two-way audio URLs (camera). |
| `/artemis/api/video/v1/device/talkURLs` | Get two-way audio URLs (device). |
| `/artemis/api/video/v1/patrols/addition` / `deletion` / `patrolIndex/patrolInfo` / `searches` | Patrol management. |
| `/artemis/api/video/v1/presets/addition` / `deletion` / `searches` | PTZ preset management. |
| `/artemis/api/video/v1/ptzs/controlling` | PTZ control. |
| `/artemis/api/video/v1/camera/capture` | Capture a picture. |
| `/artemis/api/video/v1/download` / `downloadURL` | Recording download. |
| `/artemis/api/video/v1/event/searchLabels` | Search event labels. |

### 5.5 Alarm and Event API
| Path | Purpose |
|---|---|
| `/artemis/api/eventService/v1/eventRecords/controlling` | Acknowledge/handle event records. |
| `/artemis/api/eventService/v1/eventRecords/page` | Search event records by page. |
| `/artemis/api/eventService/v1/eventSubscriptionByEventTypes` | Subscribe to event types. |
| `/artemis/api/eventService/v1/eventSubscriptionView` | View current subscriptions. |
| `/artemis/api/eventService/v1/eventUnSubscriptionByEventTypes` | Unsubscribe. |
| `/artemis/api/eventService/v1/generalEventRule/*` | General event rule CRUD + trigger. |
| `/artemis/api/eventService/v1/image_data` | Get event image data. |
| `/artemis/api/eventService/v1/deviceApplicationEvent` | Push a device application event. |

### 5.6 Visitor API
Reservation, registration, check-in/out, visitor groups, custom fields, approval flow — 18 endpoints
under `/artemis/api/visitor/v1/*` and `v2/*`. Not currently used by this project.

### 5.7 Vehicle and Parking API
Parking lot/floor/space info, crossing records, blocklist management, fee calculation — 14 endpoints
under `/artemis/api/pms/v1/*` and `/artemis/api/vehicle/v1/*`. Not currently used by this project.

### 5.8 Access Control API
See [detail](#access-control-api-58) — this is the module `AccessLevel`/`Door` features in this
project depend on.

### 5.9 On-Board Monitoring API
GPS/record overview for mobile/on-board devices — 4 endpoints under `/artemis/api/mobilesurveillance/v1/*`.
Not used by this project.

### 5.10 Person Search API
Body/face picture recognition and capture search — 5 endpoints under `/artemis/api/body/v1/*` and
`/artemis/api/frs/v1/*`. Not used by this project.

### 5.11 Digital Signage API
Material data source search/update — 3 endpoints under `/artemis/api/focsign/v1/*`. Not used by this
project.

---

## Detailed reference

### Organization Information (5.3.4)

#### `POST /artemis/api/resource/v1/org/advance/orgList`
Search organizations (traversing search — parent nodes always included in results).
- **Request**: `orgName` (opt, fuzzy match), `pageNo` (req), `pageSize` (req, max 500).
- **Response**: `data.total`, `data.pageNo`, `data.pageSize`, `data.list[]` of [`OrgInfo`](#orginfo).

```json
// Request
{ "orgName": "test", "pageNo": 1, "pageSize": 10 }

// Response
{
  "code": "0", "msg": "Success",
  "data": {
    "total": 2, "pageNo": 1, "pageSize": 10,
    "list": [
      { "orgIndexCode": "1", "orgName": "root", "parentOrgIndexCode": "0" },
      { "orgIndexCode": "2", "orgName": "test", "parentOrgIndexCode": "1" }
    ]
  }
}
```
> This project's `DepartmentSyncService`/`HikCentralService.GetOrganizationsAsync` uses this endpoint.
> `parentOrgIndexCode == "0"` marks the root org — matches the existing "skip root org, it's not a
> real department" logic in `DepartmentSyncService.SyncAllAsync`.

#### `POST /artemis/api/resource/v1/org/orgIndexCode/orgInfo`
Get one organization by ID. Request: `orgIndexCode` (req). Response: single [`OrgInfo`](#orginfo).

#### `POST /artemis/api/resource/v1/org/orgList`
List *all* organizations by page (no filter). Request: `pageNo`, `pageSize`. Response: same shape as
`advance/orgList`.

#### `POST /artemis/api/resource/v1/org/parentOrgIndexCode/subOrgList`
List immediate children of a given org. Request: `parentOrgIndexCode`, `pageNo`, `pageSize`.

#### `POST /artemis/api/resource/v1/org/rootOrg`
Get the root organization. No body params besides `userId` header.

#### `POST /artemis/api/resource/v1/org/single/add`
Add an organization. Request: `orgName` (req), `parentIndexCode` (req). Response: the new [`OrgInfo`](#orginfo).

#### `POST /artemis/api/resource/v1/org/single/delete`
Delete an organization by `orgIndexCode`.

#### `OrgInfo`
| Field | Type | Description |
|---|---|---|
| `orgIndexCode` | String | Organization ID. |
| `orgName` | String | Organization name, up to 64 chars. |
| `parentOrgIndexCode` | String | Parent org ID; `"0"` = root. |

---

### Person Information (5.3.8)

#### `POST /artemis/api/resource/v1/person/personList`
List *all* persons by page (no filter). Request: `pageNo` (req), `pageSize` (req), `appendInfo` (opt
array — `6` = room No., `19` = floor No.; unrelated to custom fields, which come back regardless).
Response: `data.list[]` of [`PersonInfo`](#personinfo).

> This is the endpoint `HikCentralService.SearchPersonsAsync` currently calls.

#### `POST /artemis/api/resource/v1/person/personId/personInfo`
Get one person by `personId`. Same [`PersonInfo`](#personinfo) shape.

> Used by `HikCentralService.GetPersonAsync`.

#### `POST /artemis/api/resource/v1/person/personCode/personInfo`
Get one person by `personCode` (the customer-assigned employee number) instead of the internal
`personId`. Not currently used by this project but potentially simpler for lookups keyed by
employee number.

#### `POST /artemis/api/resource/v1/person/advance/personList`
Search persons by `personName` (fuzzy). Request: `pageNo`, `pageSize`, `personName` (opt), `appendInfo`
(opt). Same response shape as `personList`. *(Note: `orgIndexCode` is not listed as a request filter
in this guide's version of this endpoint, unlike `HikCentralPersonSearchRequest.OrgIndexCode` in this
project's code — worth confirming against the live server whether that filter is actually honored.)*

#### `POST /artemis/api/resource/v1/person/picture_data`
Download a person's photo binary given the `picUri` from `personPhoto.picUri` + `personId`. Response
is a raw base64 data URI string, not the JSON envelope.

> Used by `HikCentralService.GetPersonPictureAsync`.

#### `POST /artemis/api/resource/v1/person/single/add`
Add a person. Request body (all under `Body`, no `appendInfo`):

| Field | Req/Opt | Description |
|---|---|---|
| `personCode` | Opt | Customer-assigned employee ID, immutable once set, max 16 chars. |
| `personGivenName` | Req | Given name, max 256 chars. |
| `personFamilyName` | Req | Family name, max 256 chars. |
| `gender` | Opt | 1-male, 2-female, 0-unknown. |
| `orgIndexCode` | Req | Organization ID. |
| `phoneNo` | Opt | |
| `email` | Opt | |
| `faces` | Opt | Array of face info. |
| `fingerPrint` | Opt | Array of fingerprint info. |
| `remark` | Opt | Max 128 chars. |
| `cards` | Opt | Array of `{cardNo}`. |
| `beginTime` / `endTime` | Opt | ISO 8601 validity window. |
| `residentRoomNo` / `residentFloorNo` | Opt | For intercom/video door phone integration. |

Response: `data` is the new person's `personId` (a plain string, not an object).

> **No `jobNo`/`jobTitle`/`position` field exists here.** `HikCentralAddPersonRequest` in this
> project's `DTOs/HikCentralDtos.cs` matches this shape closely (minus faces/fingerPrint/cards, which
> aren't used).

#### `POST /artemis/api/resource/v1/person/single/update`
Edit a person. Same field set as `single/add` plus `personId` (req) instead of requiring
`personGivenName`/`personFamilyName`/`orgIndexCode` (all become optional on update — "This field is
not required when editing the person's name" for `orgIndexCode`). Response: `data` is empty string
on success.

> **Also no `jobNo` field.** `HikCentralUpdatePersonRequest.JobNo` isn't backed by this guide.

#### `POST /artemis/api/resource/v1/person/single/delete`
Delete a person by `personId`.

#### `POST /artemis/api/resource/v1/person/face/update`
Update the face linked to a person: `personId` + `faceData` (base64).

#### `PersonInfo`
The authoritative field list (§A.1.62) — this is the complete set, nothing else exists on this object:

| Field | Type | Description |
|---|---|---|
| `personId` | String | Internal GUID, max 64 chars. |
| `personCode` | String | Customer-assigned employee ID, immutable, max 16 chars. |
| `personName` | String | Full display name, max 512 chars. |
| `personFamilyName` | String | Max 256 chars. |
| `personGivenName` | String | Max 256 chars. |
| `gender` | Number | 0-unknown, 1-male, 2-female. |
| `orgIndexCode` | String | Organization ID. |
| `fingerPrint` | Object | See `FingerPrint`. |
| `phoneNo` | String | Max 64 chars. |
| `personPhoto` | Object | `{picUri}` — see [`PersonPhoto`](#personphoto). |
| `email` | String | Max 64 chars. |
| `remark` | String | Max 256 chars. |
| `cards` | Array | List of `{cardNo}`. |
| `beginTime` / `endTime` | String | ISO 8601 validity window. |
| `customFieldList` | Array | List of [`CustomField`](#customfield). |

#### `PersonPhoto`
| Field | Type | Description |
|---|---|---|
| `picUri` | String | Relative URI; resolve via `person/picture_data`. |

#### `CustomField`
| Field | Type | Description |
|---|---|---|
| `id` | String | Custom field ID. |
| `customFiledName` | String | **Note the typo — "Filed" not "Field"** — this is Hikvision's actual JSON key, max 32 chars. |
| `customFieldType` | Number | `0` = normal text, `1` = numeric, `2` = date, `3` = single selection. |
| `customFieldValue` | String | The value, max 128 chars. |
| `presetValueList` | Array | Only when `customFieldType == 3` — the selectable options. |
| `isPublic` | Bool | `false` (default) = private. |
| `isShow` | Bool | `true` (default) = shown in the person list UI. |

> This project's `HikCentralDtos.cs` already maps `HikCentralCustomField` to `customFiledName`/
> `customFieldValue` correctly (fixed 2026-08-19). If a "Position" custom field's `customFieldValue`
> is coming back empty via the API despite showing a value in the HikCentral UI, check whether
> `isShow` is `false` on that field, or whether it's actually a *different* mechanism than a custom
> field (see [finding #3](#findings-for-this-project-read-this-first) above).

---

### Access Control API (5.8)

#### `POST /artemis/api/acs/v1/auth/applicationResult`
Get status of applying person/access-level settings to devices (async operation status check).
Request: `applicationResultType` (1 = stats, 2 = error details), `pageNo`/`pageSize` (when type 2),
`type` (1 = access control, 2 = visitor).

#### `POST /artemis/api/acs/v1/door/doControl`
Control one or more doors. Request: `doorIndexCodes` (array, max 10), `controlType` (0-remain open,
1-close, 2-open, 3-remain closed), `controlDirection` (0-entry, 1-exit; barrier gates only). Response:
`data[]` of [`DoorControlResult`](#doorcontrolresult-not-expanded---see-appendix-a122-in-source-pdf).

> Used by `HikCentralService.ControlDoorAsync`.

#### `POST /artemis/api/acs/v1/door/events`
Search access/card-swipe events. Request: `eventType` (req, decimal code) OR `personName` — **at
least one of these two is required**; `doorIndexCodes` (req, array max 10), `startTime`/`endTime`
(req, ISO 8601, **max 31 days apart**), `pageNo`/`pageSize` (req), `temperatureStatus`/`wearMaskStatus`
(opt). Response: `data.list[]` of [`PersonInOutEvents`](#personinoutevents).

> Used by `HikCentralService.SearchAccessEventsAsync`.

#### `POST /artemis/api/acs/v1/event/pictures`
Download event picture binary given `picUri`. Same pattern as `person/picture_data`.

#### `POST /artemis/api/acs/v1/privilege/group`
List access levels ("privilege groups"). Request: `pageNo`, `pageSize`, `type` (1-access control,
2-visitor). Response: `data.list[]` of [`PrivilegeGroupInfo`](#privilegegroupinfo).

```json
{
  "code": "0", "msg": "Success",
  "data": { "total": 2, "pageNo": 1, "pageSize": 10, "list": [
    { "privilegeGroupId": "2", "privilegeGroupName": "234", "description": "", "timeSchedule": { "indexCode": "1", "name": "" } },
    { "privilegeGroupId": "1", "privilegeGroupName": "123", "description": "", "timeSchedule": { "indexCode": "1", "name": "" } }
  ]}
}
```

#### `POST /artemis/api/acs/v1/privilege/group/single/addPersons`
Assign an access level to persons. Request: `privilegeGroupId`, `type`, `list[]` of `{id}` (person or
visitor ID).

> Used by `HikCentralService.AssignAccessLevelAsync`.

#### `POST /artemis/api/acs/v1/privilege/group/single/deletePersons`
Unassign — same request shape as `addPersons`.

#### `POST /artemis/api/acs/v1/privilege/group/single/personList`
Get the persons assigned to one access level (group). Request: `pageNo`, `pageSize`, `type`,
`privilegeGroupId`. Response: `data.list[]` — response example shows only `{id}` per entry, not a
full `PersonInfo`, despite the docs saying "See details in PersonInfo".

> **This — not a per-person reverse lookup — is the documented way to determine a person's access
> levels: list all groups via `privilege/group`, then check membership per group via this endpoint.**
> See [finding #2](#findings-for-this-project-read-this-first).

#### `PrivilegeGroupInfo`
| Field | Type | Description |
|---|---|---|
| `privilegeGroupId` | String | Access level (group) ID. |
| `privilegeGroupName` | String | |
| `description` | String | |
| `timeSchedule` | Object | `{indexCode, name}` — the time schedule this access level is active during. |

#### `PersonInOutEvents`
| Field | Type | Description |
|---|---|---|
| `eventId` | String | |
| `eventType` | Number | Decimal event type code (see Event Types appendix in source PDF). |
| `eventTime` / `deviceTime` | String | ISO 8601. |
| `personId` / `personName` / `personFamilyName` / `personGivenName` | String | |
| `doorName` / `doorIndexCode` | String | |
| `cardNo` | String | |
| `checkInAndOutType` | Number | 0-unknown, 1-check-in, 2-check-out, 3-break-out, 4-break-in, 5-overtime-in, 6-overtime-out. |
| `picUri` | String | Resolve via `acs/v1/event/pictures`. |
| `temperatureData` / `temperatureStatus` / `wearMaskStatus` | | Thermal/mask-detection extras. |
| `readerIndexCode` / `readerName` | String | |

---

## Status or Error Code

Full appendix (§A.5), verbatim. **Correcting the earlier misdiagnosis in this project's memory**: code
`8` is a generic-but-specific "product/version doesn't support this" code — not the same thing as the
actual permission-denied codes below it.

### API Gateway
| Code | Meaning |
|---|---|
| `0x02401000` | No AppKey configured. |
| `0x02401001` | AppKey's partner doesn't exist. |
| `0x02401002` | No signature configured. |
| `0x02401003` | Invalid signature. |
| `0x02401004` | Token authentication failed. |
| `0x02401005` | No token configured. |
| `0x02401006` | Token exception. |
| `0x02401007` | **No permission — contact admin to apply for permissions.** |
| `0x02401008` | Authentication exception (check gateway service). |
| `0x02401009` | Max API calling attempts reached. |
| `0x0240100a` | Parameter conversion exception. |
| `0x0240100b` | Calling statistics exception. |

### Parameter
| Code | Meaning |
|---|---|
| `0x00072001` | Required parameter missing. |
| `0x00072002` | Invalid parameter value range. |
| `0x00072003` | Invalid parameter value format. |
| `0x00072004` | Response too long — reduce page size. |

### Internal Service
| Code | Meaning |
|---|---|
| `0x00052101` | Highest service performance reached — retry later. |
| `0x00052102` | Service error — retry later. |
| `0x00052103` | Service response timed out. |
| `0x00052104` | Service unavailable. |

### Resource Access
| Code | Meaning |
|---|---|
| `0x00072201` | **No permission for resource access.** |
| `0x00072202` | **No permission — contact admin.** |
| `0x00072203` | Resource doesn't exist. |
| `0x00072204` | Max licenses reached. |

### Other
| Code | Meaning |
|---|---|
| `0x00052301` | Unknown error. |

### OpenAPI Translation Service (the small integer codes seen in `ArtemisResponse.Code`)
| Code | Meaning |
|---|---|
| `1` | Unknown error. |
| `2` | Incorrect request parameter. |
| `3` | Insufficient system resources. |
| `4` | Network timed out. |
| `5` | Service exception. |
| `6` | Server busy. |
| `7` | Invalid network command. |
| **`8`** | **This product version is not supported.** *(← the one we keep hitting)* |
| `9` | Invalid token. |
| `10` | Incorrect XML response message. |
| `11` | HTTP not supported. |
| `12` | URL not supported. |
| `13` | Auth info mismatch between service and HikCentral OpenAPI. |
| `14` | WAN info not configured. |
| `15` | NIC info not configured. |
| `16` | Connecting to service exception. |
| `17` | **No permission for OpenAPI access.** *(← the actual "not authorized" code — distinct from 8)* |
| `64` | Third-party partner platform user exception. |
| `65` | User is locked. |
| `66` | User doesn't exist. |
| `67` | Must initialize before first login. |
| `128` | Requested resource doesn't exist. |
| `129` | Resource is offline. |
| `130` | No permission for resource access. |
| `131` | Requested resource already exists. |
| `132` | Max number of resources reached. |
| `133` | Resource is occupied. |
| `140` | Recording schedule doesn't exist. |
| `192` | Operation/control failed. |
| `193` | Connecting to cloud storage failed. |
| `194` | Connecting to device failed. |
| `195` | Device doesn't support this function. |

---

## Source

- Original PDF: `HIKC OPEN API ENDPOINTS.pdf` (user-supplied, 542 pages) — "HikCentral Professional
  OpenAPI Developer Guide", copyright Hangzhou Hikvision Digital Technology Co., Ltd.
- Extracted to text via `pdftotext -layout` and cross-referenced against this project's
  `Services/HikCentralService.cs`, `Services/Interfaces/IHikCentralService.cs`, and `DTOs/HikCentralDtos.cs`
  on 2026-08-20.
- Not exhaustive — Visitor, Vehicle/Parking, Video, On-Board Monitoring, Person Search, and Digital
  Signage modules are indexed (method + path) but not detailed, since this project doesn't currently
  integrate with them. Expand those sections here if/when this project starts calling them.

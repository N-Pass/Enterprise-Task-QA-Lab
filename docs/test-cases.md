# Test Cases and Execution Report

**Project:** Enterprise Task Management & QA Automation Lab
**Date of execution:** 2026-10-24

This document lists the test cases, their expected and actual results, the defects found and the traceability between requirements and tests. The approach is described in the [test plan](test-plan.md), and requirement IDs refer to [requirements.md](requirements.md).

Every result marked **Passed** comes from an actual execution, listed in section 1. No result is assumed.

## 1. Execution summary

| Run | Environment | Unit | Integration | UI | Result |
|---|---|---|---|---|---|
| Local, 2026-10-24 | Windows 11, .NET SDK 10.0.402, Chrome 155, Debug and Release | 37/37 | 35/35 | 6/6 | Passed |
| CI, [run 40096749939](https://github.com/N-Pass/Enterprise-Task-QA-Lab/actions/runs/40096749939), commit `dc18c55` | GitHub Actions `ubuntu-latest`, Release | 37/37 | 35/35 | 6/6 | Passed |
| CI failure demonstration, [run 40097061030](https://github.com/N-Pass/Enterprise-Task-QA-Lab/actions/runs/40097061030) | Temporary branch with a deliberately injected bug (new tasks started as "In Progress") | 32/37 (5 failed) | Skipped | Skipped | Failed, as expected |

The failure demonstration shows that the suite detects a broken business rule (BR-04) and that the pipeline stops: integration tests are skipped after the failing step, and the UI job does not start. The temporary branch was deleted afterwards and never merged.

The UI suite was also run 4 times in a row locally (6/6 each time), and two UI tests were run on their own (both passed). This shows the tests do not depend on execution order.

## 2. UI test cases (Selenium)

Implemented in [`TaskManagementUiTests.cs`](../tests/TaskManagement.UITests/Tests/TaskManagementUiTests.cs). Preconditions for all cases: the API is running with an empty temporary database, and a fresh Chrome window is open on the task list.

### TC-UI-001: Create a task successfully

| | |
|---|---|
| **Requirements** | FR-01, FR-07, FR-08, FR-11, BR-04 |
| **Steps** | 1. Click "New task". 2. Enter a unique title and a description, select priority "High", enter assignee "Alice". 3. Click "Create task". 4. Reload the page. |
| **Expected** | The dialog closes; the message `Task "<title>" was created.` appears; the card shows status "To Do", "High priority" and "Assigned to Alice"; the task is still listed after the reload. |
| **Actual** | As expected. **Passed.** |

### TC-UI-002: Reject an invalid task form

| | |
|---|---|
| **Requirements** | BR-01, BR-02, BR-08, NFR-05 |
| **Steps** | 1. Click "New task". 2. Enter a title of only spaces and a description. 3. Click "Create task". 4. Type 130 characters into the title. 5. Cancel and reload the page. |
| **Expected** | The message "Title is required." appears under the title; the field is marked `aria-invalid`; the dialog stays open; the title field accepts at most 120 characters; no task is created (the task count is unchanged). |
| **Actual** | As expected. **Passed.** |

### TC-UI-003: Update an existing task

| | |
|---|---|
| **Requirements** | FR-04, BR-05, BR-06 |
| **Steps** | 1. Create a task with priority "Low". 2. Click "Edit" on its card. 3. Change the title, set priority "High" and assignee "Bruno". 4. Click "Save changes". 5. Reload the page. |
| **Expected** | The form opens titled "Edit task" and pre-filled; after saving, the message `Task "<new title>" was updated.` appears; the old title disappears; the card shows "High priority" and "Assigned to Bruno"; the change is still there after the reload. |
| **Actual** | As expected. **Passed.** |

### TC-UI-004: Change task status

| | |
|---|---|
| **Requirements** | FR-06, BR-06, FR-11 |
| **Steps** | 1. Create a task. 2. Select "In Progress" in its status list. 3. Select "Done". 4. Reload the page. |
| **Expected** | The card starts as "To Do"; after each change, the status label updates and the message `Status of "<title>" changed to <status>.` appears; after the reload, the status is "Done". |
| **Actual** | As expected. **Passed.** |

### TC-UI-005: Delete a task

| | |
|---|---|
| **Requirements** | FR-05, FR-11 |
| **Steps** | 1. Create a task. 2. Click "Delete" and then "Cancel". 3. Click "Delete" and then confirm. 4. Reload the page. |
| **Expected** | The confirmation names the task; "Cancel" keeps it; confirming removes the card and shows `Task "<title>" was deleted.`; the task is still gone after the reload. |
| **Actual** | As expected. **Passed.** |

### TC-UI-006: Filter tasks by status and priority

| | |
|---|---|
| **Requirements** | FR-09 |
| **Steps** | 1. Create task A (High) and task B (Low); set A to "In Progress". 2. Filter status "In Progress". 3. Also filter priority "Low". 4. Click "Clear filters". |
| **Expected** | Step 2: A is shown and B is hidden. Step 3: A is hidden as well. Step 4: both are shown. |
| **Actual** | As expected. **Passed.** |

## 3. API integration test cases

Implemented in [`TasksApiTests.cs`](../tests/TaskManagement.IntegrationTests/TasksApiTests.cs), [`ErrorHandlingTests.cs`](../tests/TaskManagement.IntegrationTests/ErrorHandlingTests.cs) and [`FrontendHostingTests.cs`](../tests/TaskManagement.IntegrationTests/FrontendHostingTests.cs). Each test runs against a new in-memory host and an empty SQLite database. All results are **Passed** (35/35, see section 1).

| ID | Scenario | Expected result | Automated test(s) |
|---|---|---|---|
| TC-API-001 | Create a task with valid data | 201, `Location` = `/api/tasks/{id}`, body with every field, status `ToDo`, `updatedAtUtc` = `createdAtUtc` | `Create_WithValidData_Returns201WithLocationAndTask` |
| TC-API-002 | Create without priority | 201, priority `Medium` | `Create_WithoutPriority_DefaultsToMedium` |
| TC-API-003 | Timestamps on the wire | ISO 8601 with `Z` (UTC) | `Create_ReturnsTimestampsInUtc` |
| TC-API-004 | Missing title (null, empty, whitespace) | 400 ProblemDetails, `errors.Title` | `Create_WithNullTitle_Returns400`, `Create_WithEmptyTitle_Returns400`, `Create_WithWhitespaceTitle_Returns400` |
| TC-API-005 | Title boundary 120 / 121 characters | 120 → 201; 121 → 400 `errors.Title` | `Create_WithTitleOfExactly120Characters_Returns201`, `Create_WithTitleOver120Characters_Returns400` |
| TC-API-006 | Description of 1,001 characters | 400 `errors.Description` | `Create_WithDescriptionOver1000Characters_Returns400` |
| TC-API-007 | Invalid priority (unknown name, number) | 400 `errors.$.priority` | `Create_WithUnknownPriorityName_Returns400`, `Create_WithNumericPriority_Returns400` |
| TC-API-008 | Malformed JSON | 400 without internal type names | `Create_WithMalformedJson_Returns400WithoutInternalDetails` |
| TC-API-009 | Get an existing task | 200 with the same task | `GetById_ExistingTask_Returns200WithTask` |
| TC-API-010 | Get an unknown or malformed id | 404 `application/problem+json` | `GetById_UnknownId_Returns404ProblemDetails`, `GetById_MalformedId_Returns404` |
| TC-API-011 | List order | Newest first | `List_ReturnsAllTasksNewestFirst` |
| TC-API-012 | Combined filters | Only tasks matching status and priority | `List_WithStatusAndPriorityFilters_ReturnsOnlyMatchingTasks` |
| TC-API-013 | Invalid status filter (`Blocked`, `7`) | 400 `errors.status` | `List_WithInvalidStatusFilter_Returns400("Blocked")`, `List_WithInvalidStatusFilter_Returns400("7")` |
| TC-API-014 | Update with valid data | 200; fields changed; `createdAtUtc` unchanged; `updatedAtUtc` advanced by exactly the clock offset | `Update_WithValidData_Returns200AndRefreshesModificationTimestamp` |
| TC-API-015 | Update is persisted | A later GET returns the new values | `Update_IsPersisted` |
| TC-API-016 | Different id in the update body | Id unchanged | `Update_WithDifferentIdInBody_DoesNotChangeIdentifier` |
| TC-API-017 | Update with invalid data | 400; the stored task is unchanged | `Update_WithInvalidData_Returns400AndLeavesTaskUnchanged`, `Update_WithoutPriority_Returns400` |
| TC-API-018 | Update an unknown id | 404 | `Update_UnknownId_Returns404` |
| TC-API-019 | Change status | 200; persisted; `updatedAtUtc` advanced | `ChangeStatus_WithValidStatus_Returns200AndPersists` |
| TC-API-020 | Change status without a status / unknown id | 400 `errors.Status` / 404 | `ChangeStatus_WithoutStatus_Returns400`, `ChangeStatus_UnknownId_Returns404` |
| TC-API-021 | Delete | 204, then GET returns 404 | `Delete_ExistingTask_Returns204AndTaskIsGone` |
| TC-API-022 | Delete an unknown id | 404 | `Delete_UnknownId_Returns404` |
| TC-API-023 | Unexpected exception | 500 `application/problem+json`, generic message, no exception message, type or stack trace | `UnexpectedException_Returns500WithoutSensitiveDetails` |
| TC-API-024 | Frontend hosting | `/` returns the HTML page; CSS and JS are served with the correct content types | `Root_ServesTheTaskManagerPage`, `StaticAssets_AreServedWithCorrectContentType` (3 cases) |

## 4. Unit test cases

Implemented in [`TaskItemTests.cs`](../tests/TaskManagement.UnitTests/Domain/TaskItemTests.cs) (25 tests) and [`TaskServiceTests.cs`](../tests/TaskManagement.UnitTests/Application/TaskServiceTests.cs) (12 tests). All results are **Passed** (37/37, see section 1).

| ID | Scenario | Expected result | Automated test(s) |
|---|---|---|---|
| TC-UNIT-001 | New task with valid data | Status `ToDo`; all fields set; unique id; both timestamps equal to the creation time | `Constructor_WithValidData_StartsWithToDoStatus`, `Constructor_WithValidData_SetsAllProperties`, `Constructor_CalledTwice_GeneratesUniqueIds` |
| TC-UNIT-002 | Required title (null, empty, whitespace) | `DomainValidationException` for `Title` | `Constructor_WithMissingTitle_Throws` (3 cases), `CreateAsync_WithInvalidTitle_ThrowsAndPersistsNothing` |
| TC-UNIT-003 | Title boundary 120 / 121 | 120 accepted; 121 rejected | `Constructor_WithTitleAtMaxLength_Succeeds`, `Constructor_WithTitleOverMaxLength_Throws` |
| TC-UNIT-004 | Description boundary 1000 / 1001 | 1000 accepted; 1001 rejected | `Constructor_WithDescriptionAtMaxLength_Succeeds`, `Constructor_WithDescriptionOverMaxLength_Throws` |
| TC-UNIT-005 | Assignee over 100 characters | Rejected | `Constructor_WithAssignedToOverMaxLength_Throws` |
| TC-UNIT-006 | Trimming and blank optional fields | Text trimmed; blank optional fields become `null` | `Constructor_TrimsTextAndConvertsBlankOptionalFieldsToNull` |
| TC-UNIT-007 | Undefined enum values | Rejected | `Constructor_WithUndefinedPriority_Throws`, `ChangeStatus_ToUndefinedStatus_Throws` |
| TC-UNIT-008 | Non-UTC timestamp | Rejected | `Constructor_WithNonUtcTimestamp_Throws` |
| TC-UNIT-009 | Valid status changes (all 6 transitions) | Status changed; `UpdatedAtUtc` updated | `ChangeStatus_ToDifferentStatus_UpdatesStatusAndTimestamp` (6 cases) |
| TC-UNIT-010 | Same-status change | No-op; timestamp unchanged | `ChangeStatus_ToSameStatus_IsNoOp` |
| TC-UNIT-011 | Update refreshes the modification timestamp | Fields and `UpdatedAtUtc` changed; `Id` and `CreatedAtUtc` unchanged; status unchanged | `Update_WithValidData_ChangesFieldsAndModificationTimestamp`, `Update_DoesNotChangeStatus`, `UpdateAsync_ExistingTask_UpdatesFieldsAndModificationTimestamp` |
| TC-UNIT-012 | Failed update | Exception; task left unchanged | `Update_WithInvalidData_ThrowsAndLeavesTaskUnchanged` |
| TC-UNIT-013 | Service create / get / default priority | Task saved once and returned; priority defaults to `Medium` | `CreateAsync_WithValidRequest_PersistsTaskAndReturnsIt`, `CreateAsync_WithoutPriority_DefaultsToMedium`, `GetByIdAsync_ExistingTask_ReturnsIt` |
| TC-UNIT-014 | Service status change, delete and list | Status and timestamp updated; task removed; filters passed to the repository | `ChangeStatusAsync_ExistingTask_ChangesStatusAndTimestamp`, `DeleteAsync_ExistingTask_RemovesIt`, `ListAsync_WithFilters_ReturnsOnlyMatchingTasks` |
| TC-UNIT-015 | Service operations with an unknown id | `NotFoundException` | `GetByIdAsync_UnknownId_ThrowsNotFound`, `UpdateAsync_UnknownId_ThrowsNotFound`, `ChangeStatusAsync_UnknownId_ThrowsNotFound`, `DeleteAsync_UnknownId_ThrowsNotFound` |

## 5. Manual checks

These checks were executed during development on 2026-10-24. The browser checks were driven by a scripted headless Chrome session (real clicks and typing through the Chrome DevTools Protocol). That script is a temporary tool and is not part of the repository.

| ID | Check | Requirements | Expected result | Actual result |
|---|---|---|---|---|
| MAN-01 | Start the API twice against the same database file | NFR-01, AC-12 | 1st start applies `InitialCreate` and inserts 3 sample tasks; 2nd start reports "No migrations were applied" and inserts nothing | As expected. **Passed** |
| MAN-02 | Smoke test of all endpoints with `curl` (20 scenarios) | FR-01 to FR-09, BR-07, BR-08 | Correct status code for every scenario; no server errors in the log | As expected after the fix for DEF-002. **Passed** |
| MAN-03 | Layout at 1280 px, 400 px and 320 px | NFR-04 | No horizontal overflow; filters and card buttons wrap | As expected. **Passed** |
| MAN-04 | Keyboard and focus behavior | NFR-05 | Focus starts in the title field; moves to the first invalid field; `Escape` and "Cancel" close dialogs; the delete dialog focuses "Cancel" | As expected. **Passed** (not tested with a screen reader) |
| MAN-05 | XSS attempt: title `<img src=x onerror="window.__xss=1">` | NFR-06 | Shown as plain text; no image element; handler not executed | As expected. **Passed** |
| MAN-06 | Act on a task deleted by another client (status change and delete) | AC-11, FR-11 | Friendly message without the raw id; the list refreshes; the stale card disappears | As expected after the fix for DEF-003. **Passed** |
| MAN-07 | OpenAPI document at `/openapi/v1.json` (Development) | NFR-03 | All 6 operations listed, with descriptions | As expected. **Passed** |

## 6. Defects found

| ID | Description | Found by | Severity | Status / fix |
|---|---|---|---|---|
| DEF-001 | The default `.gitignore` did not exclude SQLite database files (`*.db`, `*.db-shm`, `*.db-wal`), so local data could have been committed. | Review before the first database was created | Low | Fixed in `5f5c5d4` |
| DEF-002 | `GET /api/tasks/not-a-guid` returned 404 with an empty body, unlike the other errors, which return ProblemDetails. | MAN-02 | Low | Fixed before commit (`UseStatusCodePages`, included in `d8a3b76`) |
| DEF-003 | In the UI, a 404 showed the raw task id to the user ("Task with id '4cb0…' was not found."), and after a failed status change the deleted task stayed on screen. | MAN-06 | Medium (usability) | Fixed before commit (included in `00f263e`) |
| DEF-004 | Validation 400 responses were returned as `application/json` instead of `application/problem+json` (RFC 9457), while 404 and 500 responses were correct. Cause: a class-level `[Produces("application/json")]` on the controller. | Integration tests (12 failures) | Medium | Fixed in `d6c2ff0`; verified by rerunning the 35 integration tests |

Test infrastructure and process issues (not product defects):

- The NUnit `[SetUpFixture]` that starts the API for the UI tests was placed in a sibling namespace, so it never ran (6 UI failures with "The app server is not running"). It was moved to the root test namespace before commit `414c03f`. The failure screenshots were produced correctly, which also verified the diagnostics feature.
- A CI run could pass with zero tests discovered. A guard was added in `3a97c95` that fails the job when a suite executes no tests.

## 7. Traceability matrix

| Requirement | Unit | Integration | UI | Manual | Status |
|---|---|---|---|---|---|
| FR-01 Create a task | TC-UNIT-001, TC-UNIT-013 | TC-API-001 | TC-UI-001 | MAN-02 | Covered, passed |
| FR-02 View task list | | TC-API-011 | TC-UI-001 | MAN-02 | Covered, passed |
| FR-03 View task details | | TC-API-009 | | MAN-02, MAN-06 | Covered, passed (UI details dialog checked manually) |
| FR-04 Edit a task | TC-UNIT-011 | TC-API-014, TC-API-015 | TC-UI-003 | | Covered, passed |
| FR-05 Delete a task | TC-UNIT-014 | TC-API-021 | TC-UI-005 | | Covered, passed |
| FR-06 Change status | TC-UNIT-009, TC-UNIT-014 | TC-API-019 | TC-UI-004 | | Covered, passed |
| FR-07 Define priority | TC-UNIT-013 | TC-API-001, TC-API-002, TC-API-014 | TC-UI-001, TC-UI-003 | | Covered, passed |
| FR-08 Assign a person | TC-UNIT-001, TC-UNIT-005 | TC-API-001, TC-API-014 | TC-UI-001, TC-UI-003 | | Covered, passed |
| FR-09 Filter by status and priority | TC-UNIT-014 | TC-API-012, TC-API-013 | TC-UI-006 | | Covered, passed |
| FR-10 Task fields | TC-UNIT-001 | TC-API-001 | | MAN-07 | Covered, passed |
| FR-11 Success and error messages | | | TC-UI-001 to TC-UI-005 | MAN-06 | Covered, passed |
| BR-01 Title required | TC-UNIT-002 | TC-API-004 | TC-UI-002 | | Covered, passed |
| BR-02 Title ≤ 120 | TC-UNIT-003 | TC-API-005 | TC-UI-002 | | Covered, passed |
| BR-03 Description ≤ 1,000 | TC-UNIT-004 | TC-API-006 | | | Covered, passed |
| BR-04 New task is "To Do" | TC-UNIT-001 | TC-API-001 | TC-UI-001, TC-UI-004 | | Covered, passed |
| BR-05 Id cannot change | TC-UNIT-011 | TC-API-016 | | | Covered, passed |
| BR-06 Update refreshes timestamp | TC-UNIT-009 to TC-UNIT-011 | TC-API-014, TC-API-019 | | | Covered, passed |
| BR-07 Unknown or malformed id → 404 | TC-UNIT-015 | TC-API-010, TC-API-018, TC-API-020, TC-API-022 | | MAN-02 | Covered, passed |
| BR-08 Invalid data → 400 | TC-UNIT-002 to TC-UNIT-008 | TC-API-004 to TC-API-008, TC-API-013, TC-API-017, TC-API-020 | TC-UI-002 | | Covered, passed |
| BR-09 No sensitive details on failure | | TC-API-008, TC-API-023 | | | Covered, passed |
| BR-10 UTC timestamps | TC-UNIT-008 | TC-API-003 | | | Covered, passed |
| BR-11 Assignee ≤ 100 | TC-UNIT-005 | | | | Covered, passed |
| NFR-01 Persistence | | TC-API-015 (real SQLite, migrations applied) | TC-UI-001, TC-UI-003 to TC-UI-005 (reload) | MAN-01 | Covered, passed |
| NFR-02 Layered architecture | TC-UNIT-* (domain and service tested without HTTP or database) | | | | Covered by design and tests |
| NFR-03 API documentation | | | | MAN-07 | Checked manually, passed |
| NFR-04 Responsive UI | | | | MAN-03 | Checked manually, passed |
| NFR-05 Accessibility | | | TC-UI-002 (`aria-invalid`) | MAN-04 | Partially covered (no screen reader test) |
| NFR-06 Security basics | | TC-API-008, TC-API-023 | | MAN-05 | Covered, passed |
| NFR-07 Reproducibility | | | | CI runs (section 1) | Covered, passed |
| NFR-08 Quality automation | | | | CI runs and failure demonstration (section 1) | Covered, passed |

## 8. Known limitations of the testing

- UI tests run on Google Chrome only.
- Accessibility was checked for labels, focus and ARIA attributes, but not with a real screen reader or an automated accessibility scanner.
- The UI task details dialog (FR-03) is verified manually, not by a Selenium test.
- No performance or load testing was done.
- The CI runner uses the `ubuntu-latest` image, which changes over time. A future image update could change the environment without any code change.

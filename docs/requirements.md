# Software Requirements Specification

**Project:** Enterprise Task Management & QA Automation Lab
**Version:** 1.0
**Date:** 2026-10-24

## 1. Introduction

### 1.1 Purpose

This document specifies the functional and non-functional requirements of a small task management application. It is the reference for the implementation, the [test plan](test-plan.md) and the [test cases](test-cases.md), which trace every requirement to the tests that verify it.

### 1.2 Scope

The system lets a team record tasks, assign them to a person, prioritize them and track their progress through three statuses. It consists of a REST API with persistent storage and a browser-based user interface.

The project is a personal portfolio exercise. It is not deployed to production and has a single, unauthenticated user role.

### 1.3 Definitions

| Term | Meaning |
|---|---|
| Task | A unit of work with a title, optional description, status, priority, optional assignee and timestamps. |
| Status | Progress state of a task: `ToDo` ("To Do"), `InProgress` ("In Progress"), `Done`. |
| Priority | Importance of a task: `Low`, `Medium`, `High`. |
| Assignee | Free-text name of the person responsible for a task. |
| ProblemDetails | Standard JSON error format defined by RFC 9457, returned by the API for every error. |
| UTC | Coordinated Universal Time; all timestamps are stored and returned in UTC. |

## 2. Overall description

### 2.1 Users

A single user role: a team member who manages tasks through the web interface or directly through the REST API.

### 2.2 Constraints

- Backend: C#, ASP.NET Core Web API, Entity Framework Core, SQLite, on a supported LTS version of .NET (.NET 10).
- Frontend: HTML5, CSS3 and vanilla JavaScript, without a frontend framework.
- Tests: NUnit, ASP.NET Core integration testing tools and Selenium WebDriver.
- The database schema is managed through EF Core migrations.

### 2.3 Assumptions

These decisions were taken where the original brief left room for interpretation:

| ID | Assumption |
|---|---|
| A1 | Any status transition is allowed (including moving back from `Done`). Setting the status a task already has is a no-op and does not change its timestamp. |
| A2 | `PUT` replaces title, description, priority and assignee. Status changes only through the dedicated status endpoint, so all status logic has one path. |
| A3 | The assignee is optional free text, up to 100 characters. |
| A4 | Status and priority are exchanged as names (`"InProgress"`, `"High"`). Unknown names and numeric values are rejected. |
| A5 | When priority is omitted on creation it defaults to `Medium`. On update (full replacement) it is required. |
| A6 | Leading and trailing spaces are removed from text fields; an optional field that is blank is stored as empty (`null`). |
| A7 | Task identifiers are GUIDs generated when the task is created. |

## 3. Functional requirements

| ID | Requirement |
|---|---|
| FR-01 | The user can create a task. |
| FR-02 | The user can view the list of tasks, newest first. |
| FR-03 | The user can view the details of a task. |
| FR-04 | The user can edit an existing task (title, description, priority, assignee). |
| FR-05 | The user can delete a task, after confirming in the user interface. |
| FR-06 | The user can change the status of a task. |
| FR-07 | The user can define the priority of a task. |
| FR-08 | The user can assign a responsible person to a task by name. |
| FR-09 | The user can filter tasks by status, by priority, or by both. |
| FR-10 | Each task has: unique identifier, title, description, status, priority, assignee, creation date and last updated date. |
| FR-11 | The user receives clear success and error messages for every action. |

## 4. Business rules

| ID | Rule |
|---|---|
| BR-01 | The task title is required (blank or whitespace-only titles are rejected). |
| BR-02 | The task title must not exceed 120 characters. |
| BR-03 | The task description must not exceed 1,000 characters. |
| BR-04 | A new task starts with the status "To Do". |
| BR-05 | A task identifier cannot be changed. |
| BR-06 | Updating a task, or changing its status, updates its modification timestamp. The creation timestamp never changes. |
| BR-07 | Requests for a nonexistent or malformed task identifier return HTTP 404. |
| BR-08 | Invalid request data returns HTTP 400 with a validation error that identifies the invalid fields. |
| BR-09 | Unexpected failures return HTTP 500 without exposing sensitive implementation details (exception messages, types or stack traces). |
| BR-10 | Timestamps are stored and returned in UTC. |
| BR-11 | The assignee must not exceed 100 characters (assumption A3). |

## 5. Non-functional requirements

| ID | Category | Requirement |
|---|---|---|
| NFR-01 | Persistence | Data is stored in SQLite and persists between application restarts. The schema is created and updated through EF Core migrations. |
| NFR-02 | Maintainability | The code follows a layered architecture (Domain, Application, Infrastructure, Api); business rules do not depend on HTTP or database code. |
| NFR-03 | Documentation | The REST API is described by an OpenAPI document, and the repository includes setup and test instructions. |
| NFR-04 | Usability | The interface is responsive and usable from 320 px wide screens to desktop. |
| NFR-05 | Accessibility | Form controls have labels; the interface is keyboard operable; validation errors are associated with their fields; status messages are announced to assistive technologies. |
| NFR-06 | Security | All input is validated on the server; user-provided text is never rendered as HTML (XSS prevention); error responses do not leak internals. |
| NFR-07 | Reproducibility | The SDK version is pinned, and dependencies are restored from NuGet with exact versions. |
| NFR-08 | Quality automation | Unit, integration and UI tests run automatically in CI on every push to `main`, and the pipeline fails when a build or test step fails. |

## 6. Acceptance criteria

| ID | Covers | Given / When / Then |
|---|---|---|
| AC-01 | FR-01, BR-04, BR-10 | **Given** a valid title, **when** the user creates a task, **then** it is saved with status "To Do", UTC timestamps and a unique id; the API returns 201 with a `Location` header. |
| AC-02 | BR-01, BR-02, BR-03, BR-08, BR-11 | **Given** a missing or whitespace-only title, a title over 120 characters, a description over 1,000 characters or an assignee over 100 characters, **when** the user saves, **then** the task is not saved and a message identifies the invalid field (API: 400 ProblemDetails with `errors`). |
| AC-03 | FR-02 | **Given** several tasks, **when** the user opens the list, **then** all tasks are shown, newest first. |
| AC-04 | FR-03, FR-10 | **Given** an existing task, **when** the user opens its details, **then** every field is shown, with dates in the user's local time. |
| AC-05 | FR-04, BR-05, BR-06 | **Given** an existing task, **when** the user edits it, **then** the changes are persisted, the id and creation date are unchanged and the last updated date moves forward. |
| AC-06 | FR-06, BR-06 | **Given** an existing task, **when** the user changes its status, **then** the new status is persisted and the last updated date moves forward. |
| AC-07 | FR-05 | **Given** an existing task, **when** the user deletes it and confirms, **then** it disappears and later requests for it return 404; cancelling keeps the task. |
| AC-08 | FR-09 | **Given** tasks with different statuses and priorities, **when** the user selects filters, **then** only matching tasks are shown; clearing the filters shows all tasks again. |
| AC-09 | BR-07 | **Given** an unknown or malformed id, **when** any task endpoint is called, **then** the API returns 404 ProblemDetails. |
| AC-10 | BR-09, NFR-06 | **Given** an unexpected server error, **when** a request fails, **then** the response is a generic 500 ProblemDetails without the exception message, type or stack trace. |
| AC-11 | FR-11 | **Given** any user action, **when** it succeeds or fails, **then** a success or error message is shown; if the task no longer exists, the message says so and the list refreshes. |
| AC-12 | NFR-01 | **Given** saved tasks, **when** the application restarts, **then** the tasks are still available and no migration is re-applied. |

## 7. Out of scope

- Authentication, authorization and multiple users or roles.
- Pagination, sorting options and free-text search.
- Concurrency control for simultaneous edits (last write wins).
- Production deployment, monitoring and backups.

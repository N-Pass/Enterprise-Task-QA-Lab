# Test Plan

**Project:** Enterprise Task Management & QA Automation Lab
**Version:** 1.0
**Date:** 2026-10-24

## 1. Objectives

- Verify that every functional requirement, business rule and acceptance criterion in [requirements.md](requirements.md) is met.
- Detect regressions automatically on every change through CI.
- Provide evidence: test results, a traceability matrix and a record of defects found.

## 2. Scope

**In scope**

- Domain business rules and application use cases.
- REST API behavior: status codes, payloads, validation, error format, persistence.
- Web interface workflows: create, validate, edit, change status, delete, filter.
- Non-functional checks: persistence across restarts, responsive layout, basic accessibility and XSS prevention.

**Out of scope**

- Performance, load and stress testing.
- Security penetration testing.
- Browsers other than Google Chrome.
- Testing with a real screen reader.

## 3. Test levels and approach

The levels follow the test pyramid: many fast unit tests, fewer integration tests, and a small set of end-to-end UI tests for the critical user journeys.

| Level | Tool | Purpose | Test basis |
|---|---|---|---|
| Unit | NUnit | Business rules in `TaskItem` and orchestration in `TaskService`, isolated from HTTP and database. Boundary values (120/121, 1000/1001 characters) and every status transition. | BR-01 to BR-06, BR-11 |
| Integration | NUnit + `WebApplicationFactory` | The real API over HTTP with a real (in-memory) SQLite database and migrations: status codes, headers, ProblemDetails, persistence, filters, error handling. | FR-01 to FR-10, BR-01 to BR-10 |
| UI (end-to-end) | NUnit + Selenium WebDriver (Chrome) | Critical user journeys through the real browser, against the API running as a separate process. | FR-01 to FR-09, FR-11 |
| Manual / exploratory | Browser, `curl`, scripted headless Chrome session | Checks that are hard or slow to automate: restart persistence, responsive layout at several widths, keyboard behavior, XSS attempt. | NFR-01, NFR-04 to NFR-06 |

Test design techniques used:

- **Boundary value analysis:** title of 120 and 121 characters, description of 1000 and 1001 characters.
- **Equivalence partitioning:** null, empty and whitespace-only titles; valid, unknown and numeric enum values.
- **State transition testing:** all six status transitions, plus the same-status no-op.
- **Error guessing:** malformed JSON, malformed id, id in the request body, a task deleted by someone else.

## 4. Test environment

| Item | Local | CI |
|---|---|---|
| OS | Windows 11 Pro | GitHub Actions `ubuntu-latest` |
| .NET SDK | 10.0.402 (pinned in `global.json`) | Same, installed by `actions/setup-dotnet` |
| Browser | Google Chrome 155 | Chrome preinstalled on the runner image |
| Driver | ChromeDriver resolved by Selenium Manager | Same |
| Database | SQLite in-memory (integration), temporary file (UI) | Same |

## 5. Test data

- **Unit tests** build objects directly, with fixed UTC timestamps.
- **Integration tests** start from an empty database for every test and create the data they need through the API. A controllable clock makes timestamps exact.
- **UI tests** use a temporary database per run and create their own tasks with unique titles (a random suffix), so they never depend on existing data or on other tests.
- **Development seed data** (three sample tasks) is used only for manual exploration and screenshots. It is never inserted during automated tests.

## 6. Entry and exit criteria

**Entry criteria**

- The solution builds with zero warnings (`TreatWarningsAsErrors` is enabled).
- The feature under test is implemented and committed.

**Exit criteria**

- 100% of automated tests pass locally and in CI.
- Every requirement in the traceability matrix is covered by at least one passing test or a documented manual check.
- No open defect of high severity.
- Known limitations are documented.

## 7. Risks and mitigations

| Risk | Mitigation |
|---|---|
| Flaky UI tests caused by timing | Explicit waits only (no sleeps, no implicit wait); cards are re-located by id after each re-render; the suite was run repeatedly to check stability. |
| Tests depending on each other or on execution order | Isolated databases, a fresh browser per test, unique test data. Individual UI tests were also run alone. |
| Differences between Windows (local) and Linux (CI) | Line endings normalized by `.gitattributes`; the pipeline was simulated locally in Release before pushing. |
| A green pipeline that ran no tests | `summarize-tests.sh` fails the CI job when a suite executes zero tests. |
| In-memory test doubles hiding real database behavior | Integration tests use real SQLite and the real migrations instead of the EF Core in-memory provider. |

## 8. Defect management

Defects are recorded in [test-cases.md](test-cases.md#6-defects-found) with an ID, a description, how they were found, their severity and the commit that fixed them. Each fix is verified by rerunning the related tests, and then the full suite.

## 9. Deliverables

- Automated test suites in `tests/`.
- TRX test results as GitHub Actions artifacts, and screenshots of failed UI tests.
- [Test cases, execution results, traceability matrix and defect log](test-cases.md).

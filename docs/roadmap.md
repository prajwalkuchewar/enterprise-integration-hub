# Enterprise Integration Hub Roadmap

## Sprint 1 ✅

External System Management

- [x] Create External System
- [x] Browse External Systems
- [x] View External System Details
- [x] Update External System
- [x] Activate External System
- [x] Deactivate External System

---

## Sprint 2 ✅

Connector Management

- [x] Create Connector
- [x] Browse Connectors
- [x] View Connector Details
- [x] Update Connector
- [x] Activate Connector
- [x] Deactivate Connector

---

## Sprint 3 ✅

Workflow Management

- [x] Create Workflow
- [x] Browse Workflows
- [x] View Workflow Details
- [x] Update Workflow
- [x] Activate Workflow

## Backend handoff ✅

- [x] REST lifecycle APIs for external systems and connectors
- [x] Persistence-backed repositories and EF Core migrations
- [x] CORS configured for frontend development
- [x] Swagger/OpenAPI available in Development
- [x] Domain, application, and infrastructure test suites passing

The next workstream is the frontend consuming the existing `/api/external-systems`,
`/api/connectors`, and `/api/workflows` resources. Execution, transformation,
monitoring, authentication, and audit history remain post-MVP backend slices.

---

## Sprint 4

Workflow Steps

---

## Sprint 5

Transformation Engine

---

## Sprint 6

Integration Execution

---

## Sprint 7

Operations & Monitoring

---

## Sprint Completion Checklist

Complete this checklist before closing and releasing each sprint:

- [ ] Feature Complete
- [ ] Unit Tests
- [ ] Swagger Tested
- [ ] PR Review
- [ ] Documentation
- [ ] CI Green
- [ ] Merge
- [ ] Tag Release

Create a release tag only after the merged change has passed CI. Use the repository's selected versioning convention once one has been adopted.

---

## CI Hardening

- [x] NuGet package caching in `.github/workflows/ci.yml` using `actions/setup-dotnet` with `cache: true`.
- [x] Formatting verification with `dotnet format --verify-no-changes`.
- [x] Warning-free release builds with `dotnet build /warnaserror`.
- [x] Coverage collection with Coverlet, report generation with ReportGenerator, and CI artifact publication.
- [ ] Configure a coverage badge after selecting its hosting and threshold policy.
- [ ] Configure GitHub branch protection to require the CI check before merge after the workflow changes have been validated on pull requests.

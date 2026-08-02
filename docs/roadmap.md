# Enterprise Integration Hub Roadmap

## Sprint 1 ✅

External System Management

- [x] Create External System
- [x] Browse External Systems
- [x] View External System Details
- [ ] Update External System
- [ ] Activate External System
- [ ] Deactivate External System

---

## Sprint 2

Connector Management

- [ ] Create Connector
- [ ] Browse Connectors
- [ ] View Connector Details
- [ ] Activate Connector
- [ ] Deactivate Connector

---

## Sprint 3

Workflow Management

- [ ] Create Workflow
- [ ] Browse Workflows
- [ ] View Workflow Details

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

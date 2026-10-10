# Implementation Plan: [FEATURE]

**Branch**: `[###-feature-name]` | **Date**: [DATE] | **Spec**: [link]

**Input**: Feature specification from `/specs/[###-feature-name]/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command; its definition describes the execution workflow.

## Summary

[Extract from feature spec: primary requirement + technical approach from research]

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: [e.g., Python 3.11, Swift 5.9, Rust 1.75 or NEEDS CLARIFICATION]

**Primary Dependencies**: [e.g., FastAPI, UIKit, LLVM or NEEDS CLARIFICATION]

**Storage**: [if applicable, e.g., PostgreSQL, CoreData, files or N/A]

**Testing**: [e.g., pytest, XCTest, cargo test or NEEDS CLARIFICATION]

**Target Platform**: [e.g., Linux server, iOS 15+, WASM or NEEDS CLARIFICATION]

**Project Type**: [e.g., library/cli/web-service/mobile-app/compiler/desktop-app or NEEDS CLARIFICATION]

**Performance Goals**: [domain-specific, e.g., 1000 req/s, 10k lines/sec, 60 fps or NEEDS CLARIFICATION]

**Constraints**: [domain-specific, e.g., <200ms p95, <100MB memory, offline-capable or NEEDS CLARIFICATION]

**Scale/Scope**: [domain-specific, e.g., 10k users, 1M LOC, 50 screens or NEEDS CLARIFICATION]

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design and before `/speckit.tasks`.*

### Mandatory Gates

- **GC-01 Source Priority & Scope (Articles 1-2)**
  - Plan cites source artifacts in priority order and confirms active feature scope boundaries.
  - Out-of-scope items are flagged and blocked until explicit authorization exists.

- **GC-02 Architecture & Security (Articles 6-7)**
  - Design preserves required backend/frontend separation of responsibilities.
  - Security model includes authn/authz and tenant isolation rules for sensitive operations.

- **GC-03 Contract Readiness (Article 8)**
  - Planned endpoints include contract paths and ownership for request/response/error documentation.
  - No endpoint is considered complete in planning without contract update tasks.

- **GC-04 Test Strategy Alignment (Article 9)**
  - Plan identifies required evidence by test type (unit or black-box) according to phase policy.
  - Validation approach is defined for each user story.

- **GC-05 Field-Level Traceability (Article 10, C10)**
  - Plan references the Implementation Coverage Matrix in spec.md and confirms coverage rows for all required fields/flows.
  - Any missing coverage row is a planning blocker.

- **GC-06 Spec-to-Deliverable Parity (Article 12, C11)**
  - For persistent entities, runtime source of truth is planned as persistence-backed (not hardcoded in-memory).
  - Temporary exceptions require explicit authorization and removal date.

- **GC-07 Schema Change Governance (Articles 3-5 + schema governance section)**
  - If structural DB changes exist, plan includes migration strategy, impact analysis, rollback plan, and explicit authorization evidence.
  - Plan includes SQL verification evidence for final schema parity (tables, columns nullability, FK and delete actions).

### Gate Evidence Table *(mandatory)*

| Gate | Status (Pass/Fail) | Evidence (file paths, approvals, notes) |
|------|---------------------|------------------------------------------|
| GC-01 | [ ] | [ ] |
| GC-02 | [ ] | [ ] |
| GC-03 | [ ] | [ ] |
| GC-04 | [ ] | [ ] |
| GC-05 | [ ] | [ ] |
| GC-06 | [ ] | [ ] |
| GC-07 | [ ] | [ ] |

**Blocking rule**: Any gate in Fail status blocks progression to Phase 0/1/2 until corrected or explicitly authorized as exception.

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->

```text
# [REMOVE IF UNUSED] Option 1: Single project (DEFAULT)
src/
├── models/
├── services/
├── cli/
└── lib/

tests/
├── contract/
├── integration/
└── unit/

# [REMOVE IF UNUSED] Option 2: Web application (when "frontend" + "backend" detected)
backend/
├── src/
│   ├── models/
│   ├── services/
│   └── api/
└── tests/

frontend/
├── src/
│   ├── components/
│   ├── pages/
│   └── services/
└── tests/

# [REMOVE IF UNUSED] Option 3: Mobile + API (when "iOS/Android" detected)
api/
└── [same as backend above]

ios/ or android/
└── [platform-specific structure: feature modules, UI flows, platform tests]
```

**Structure Decision**: [Document the selected structure and reference the real
directories captured above]

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |

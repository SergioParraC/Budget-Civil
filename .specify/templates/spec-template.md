# Feature Specification: [FEATURE NAME]

**Feature Branch**: `[###-feature-name]`

**Created**: [DATE]

**Status**: Draft

**Input**: User description: "$ARGUMENTS"

## User Scenarios & Testing *(mandatory)*

<!--
  IMPORTANT: User stories should be PRIORITIZED as user journeys ordered by importance.
  Each user story/journey must be INDEPENDENTLY TESTABLE - meaning if you implement just ONE of them,
  you should still have a viable MVP (Minimum Viable Product) that delivers value.

  Assign priorities (P1, P2, P3, etc.) to each story, where P1 is the most critical.
  Think of each story as a standalone slice of functionality that can be:
  - Developed independently
  - Tested independently
  - Deployed independently
  - Demonstrated to users independently
-->

### User Story 1 - [Brief Title] (Priority: P1)

[Describe this user journey in plain language]

**Why this priority**: [Explain the value and why it has this priority level]

**Independent Test**: [Describe how this can be tested independently - e.g., "Can be fully tested by [specific action] and delivers [specific value]"]

**Acceptance Scenarios**:

1. **Given** [initial state], **When** [action], **Then** [expected outcome]
2. **Given** [initial state], **When** [action], **Then** [expected outcome]

---

### User Story 2 - [Brief Title] (Priority: P2)

[Describe this user journey in plain language]

**Why this priority**: [Explain the value and why it has this priority level]

**Independent Test**: [Describe how this can be tested independently]

**Acceptance Scenarios**:

1. **Given** [initial state], **When** [action], **Then** [expected outcome]

---

### User Story 3 - [Brief Title] (Priority: P3)

[Describe this user journey in plain language]

**Why this priority**: [Explain the value and why it has this priority level]

**Independent Test**: [Describe how this can be tested independently]

**Acceptance Scenarios**:

1. **Given** [initial state], **When** [action], **Then** [expected outcome]

---

[Add more user stories as needed, each with an assigned priority]

### Edge Cases

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right edge cases.
-->

- What happens when [boundary condition]?
- How does system handle [error scenario]?

## Requirements *(mandatory)*

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right functional requirements.
-->

### Functional Requirements

- **FR-001**: System MUST [specific capability, e.g., "allow users to create accounts"]
- **FR-002**: System MUST [specific capability, e.g., "validate email addresses"]
- **FR-003**: Users MUST be able to [key interaction, e.g., "reset their password"]
- **FR-004**: System MUST [data requirement, e.g., "persist user preferences"]
- **FR-005**: System MUST [behavior, e.g., "log all security events"]

*Example of marking unclear requirements:*

- **FR-006**: System MUST authenticate users via [NEEDS CLARIFICATION: auth method not specified - email/password, SSO, OAuth?]
- **FR-007**: System MUST retain user data for [NEEDS CLARIFICATION: retention period not specified]

### Key Entities *(include if feature involves data)*

- **[Entity 1]**: [What it represents]
  - **Persistence**: [Persistent | Derived | Transient]
  - **Required fields**: [field_a, field_b, ...]
  - **Optional fields**: [field_x, field_y, ...]
  - **Relationships**: [1:N, N:M, ownership rules]
  - **Runtime source of truth**: [table/view/repository]
- **[Entity 2]**: [What it represents]
  - **Persistence**: [Persistent | Derived | Transient]
  - **Required fields**: [...]
  - **Optional fields**: [...]
  - **Relationships**: [...]
  - **Runtime source of truth**: [...]

**Rule**: If an entity is marked as **Persistent**, its runtime source of truth MUST NOT be a hardcoded in-memory list.

## Implementation Coverage Matrix *(mandatory for data features)*

<!--
  ACTION REQUIRED: Complete one row per required field / required behavior.
  This matrix is the anti-drift control between approved spec and deliverable.
  Use NEEDS CLARIFICATION when any path or ownership is unresolved.
-->

| Coverage ID | Requirement (FR/BR/UC) | Entity.Field or Flow | Domain/Application Path | Persistence Path (schema/table/column/FK) | API Contract Path | Validation/Test Evidence | Status |
|-------------|--------------------------|----------------------|-------------------------|--------------------------------------------|-------------------|--------------------------|--------|
| COV-001 | FR-XXX | EntityA.required_field | src/... | schema.table.column | contracts/... | tests/... or AC-... | Planned |
| COV-002 | FR-YYY | Flow: state transition X->Y | src/... | audit.table.column | contracts/... | tests/... or AC-... | Planned |

**Completion rule**:
- No feature can move from Draft to Approved if any row is missing or unresolved.
- All rows for required fields/flows MUST reach status Implemented + Verified before feature closure.

## Delivery Alignment Gates *(mandatory)*

- **G1 - Spec vs Model completeness**: Every required field in this spec exists in domain/application model design.
- **G2 - Spec vs Persistence completeness**: Every persistent required field has expected table/column/FK defined.
- **G3 - Spec vs Contract completeness**: Every externally visible field/behavior has request/response/error contract coverage.
- **G4 - Verification evidence**: Tasks/tests reference each coverage row and provide objective evidence.
- **G5 - Schema evidence when applicable**: If schema changed, include SQL verification evidence in quickstart or evidence artifact.

## Success Criteria *(mandatory)*

<!--
  ACTION REQUIRED: Define measurable success criteria.
  These must be technology-agnostic and measurable.
-->

### Measurable Outcomes

- **SC-001**: [Measurable metric, e.g., "Users can complete account creation in under 2 minutes"]
- **SC-002**: [Measurable metric, e.g., "System handles 1000 concurrent users without degradation"]
- **SC-003**: [User satisfaction metric, e.g., "90% of users successfully complete primary task on first attempt"]
- **SC-004**: [Business metric, e.g., "Reduce support tickets related to [X] by 50%"]

## Assumptions

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right assumptions based on reasonable defaults
  chosen when the feature description did not specify certain details.
-->

- [Assumption about target users, e.g., "Users have stable internet connectivity"]
- [Assumption about scope boundaries, e.g., "Mobile support is out of scope for v1"]
- [Assumption about data/environment, e.g., "Existing authentication system will be reused"]
- [Dependency on existing system/service, e.g., "Requires access to the existing user profile API"]

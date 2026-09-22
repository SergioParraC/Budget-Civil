# Feature Specification: [FEATURE NAME]

**Feature Branch**: `[###-feature-name]`

**Created**: [DATE]

**Status**: Draft

**Input**: User description: "$ARGUMENTS"

## Source Of Truth & Scope *(mandatory)*

<!--
  ACTION REQUIRED: Declare the governing artifacts used for this feature and the
  explicit scope boundary.
-->

### Governing Sources

List all sources used, in priority order defined by the project constitution.

1. [Highest-priority source used]
2. [Next source used]
3. [Additional source used]

### Scope Boundary

- **In Scope**: [What is included in this feature]
- **Out Of Scope**: [What is explicitly excluded]
- **Out-of-Scope Authorization**: [None | Link to explicit approval if needed]

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

## Endpoint Contracts *(mandatory when API is added or changed)*

<!--
  ACTION REQUIRED: For each endpoint added or changed, document a frontend-ready
  contract with all required fields.
-->

### Endpoint: [METHOD] [ROUTE]

- **Auth Required**: [Yes/No]
- **Allowed Roles**: [Role list]
- **Company Ownership Rule**: [How CompanyId is derived/validated]
- **Request Schema**: [Fields + types + required/optional]
- **Response Schema**: [Fields + types]
- **Validations**: [Business and data validation rules]
- **Errors**: [HTTP code + business code + trigger condition]
- **Request Example**: [JSON example]
- **Response Example**: [JSON example]

[Repeat per endpoint as needed]

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

### Non-Functional Requirements

- **NFR-001**: [Security, performance, privacy, observability, maintainability requirement]
- **NFR-002**: [Additional measurable non-functional requirement]

### Business Rules

- **BR-001**: [Rule statement]
- **BR-002**: [Rule statement]

### Key Entities *(include if feature involves data)*

- **[Entity 1]**: [What it represents, key attributes without implementation]
- **[Entity 2]**: [What it represents, relationships to other entities]

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

## Testing Strategy *(mandatory)*

<!--
  ACTION REQUIRED: Define test evidence aligned with constitution.
-->

- Backend unit tests MUST cover APU value calculations when impacted.
- Remaining backend and frontend behavior MUST be validated with black-box tests.
- Every user story MUST specify at least one verifiable test evidence artifact.

### Planned Test Evidence

- **US1**: [Test type + file/path or execution evidence]
- **US2**: [Test type + file/path or execution evidence]
- **US3**: [Test type + file/path or execution evidence]

## Data Schema Change Control *(mandatory when structural data change exists)*

<!--
  ACTION REQUIRED: Fill only if this feature changes schema structure.
-->

- **Driver Requirement (FR/UC/BR)**: [Reference]
- **Change Type**: [Alter / Split / Merge / Replace / Other]
- **Migration Plan**: [Forward steps]
- **Impact Analysis**: [Backend/API/frontend/test impact]
- **Rollback Plan**: [Rollback strategy]
- **Explicit User Authorization Evidence**: [Link or record]

## Traceability Matrix *(mandatory)*

| Requirement | Use Case | Endpoint/Contract | Test Evidence | Planned Task IDs |
|-------------|----------|-------------------|---------------|------------------|
| FR-XXX | UC-XXX | [METHOD route] | [test file/evidence] | [TXXX, TYYY] |

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

---
description: "Use when creating or editing a sprint card in Implementation/sprints/. Enforces the cadence contract: single objective, explicit DoD, KPIs, risks, and traceability update on delivery."
applyTo: "Implementation/sprints/**/*.md"
---

# Sprint Docs — poc-bouldering-game

Source for cadence rules: [sprint_cadence.md](../../Implementation/sprints/sprint_cadence.md).
Existing cards are the visual reference — match their structure (see [sprint_1_0.md](../../Implementation/sprints/sprint_1_0.md), [sprint_3_0.md](../../Implementation/sprints/sprint_3_0.md)).

## Required sections in a sprint card

A sprint card (`sprint_<major>_<minor>.md`) MUST contain, in this order:

1. **Objective** — exactly one main goal. If you need "and", split the sprint.
2. **Scope** — bullet list of in-scope items. Each item maps to a `plan/` deliverable or an ADR.
3. **Out of scope** — explicit, to prevent creep.
4. **Definition of Done** — every item is independently testable. No "make it nicer" entries.
5. **Top 3 risks** — each with a mitigation. Max 3.
6. **KPIs / validation criteria** — numeric or pass/fail. Link to [plan/06_tests/validation_criteria.md](../../plan/06_tests/validation_criteria.md) when applicable.
7. **External dependencies** — packages, assets, ADRs to land first.
8. **Deliverable** — what is demoable at sprint end.

Addenda (`sprint_X_Y_addendum.md`) repeat sections 1, 4, 8 only, plus a **Rationale** paragraph explaining why scope is extended after the fact.

## Definition of Done — rules

- Each DoD item starts with a verb (`Implement`, `Validate`, `Cover by test`, `Document`).
- A DoD item is binary: done or not done. No percentages.
- A DoD item that cannot be validated in ≤1 remaining day is **deferred**, not partially done — see [sprint_cadence.md § Definition of Deferral](../../Implementation/sprints/sprint_cadence.md).
- Tests are part of DoD, never a footnote.

## Tuning value rule

Any sprint that introduces or changes a numeric value MUST:
1. Update [plan/04_technical/variables_gameplay.md](../../plan/04_technical/variables_gameplay.md) (default + tested range).
2. Bump `tuning_version` if the change is shipped.
3. Reference both in the sprint card's Deliverable section.

## Traceability rule (on delivery)

When a sprint item is delivered (end-of-sprint review):
1. Update [Implementation/planning/plan_sprint_traceability.md](../../Implementation/planning/plan_sprint_traceability.md) with: plan ID → sprint → status.
2. Capture max 3 lessons learned in the sprint card under a `## Lessons Learned` section.
3. List prerequisites for the next sprint in a `## Next Sprint Prerequisites` section.

## Playtest reports

A playtest report lives in [Implementation/sprints/playtest/](../../Implementation/sprints/playtest/) and follows
[playtest_report_template.md](../../plan/06_tests/playtest_report_template.md). Do not invent a new structure.

## Forbidden in sprint cards

- Multi-objective sprints ("FSM **and** scoring **and** UI rebinding").
- DoD items without a validation method.
- Numeric tuning values inline — always link to `variables_gameplay.md`.
- Pasting full code blocks (link the file instead).
- French text — sprint cards are English only, like the rest of the codebase.

## Language

English only (titles, body, identifiers, log excerpts).

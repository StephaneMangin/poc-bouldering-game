---
description: "Draft a new Architecture Decision Record entry conforming to the project's ADR template, with auto-incremented number, status=proposed, and all required sections."
argument-hint: "Title of the decision (e.g. 'Hold detection via SphereOverlap')"
agent: "agent"
---

# Create a new ADR

Reference template and existing entries: [Implementation/architecture/adr.md](../../Implementation/architecture/adr.md).

## Steps

1. **Read the current ADR file** to find the highest `ADR-XXX` number in use. The new entry is `ADR-<max + 1>`, three-digit padded.
2. **Append a new section** at the end of `Implementation/architecture/adr.md` (do not create a separate file — all ADRs live in one document for this project).
3. **Use exactly the template fields** below — no extra sections, no reordering.
4. **Status defaults to `proposed`**. Only the user flips it to `accepted` or `deprecated` after review.
5. **Date is today's date** in ISO format (`YYYY-MM-DD`).
6. **Alternatives section MUST list at least 2** real alternatives with a one-line trade-off each. If you cannot find 2, the decision is not worth an ADR — stop and tell the user.
7. **Consequences MUST be balanced**: positive and negative each have ≥1 concrete item.
8. **Mitigation plan** addresses every negative consequence.

## Template (copy-paste into the file)

```markdown
## ADR-XXX - <Title from $ARGUMENTS>
1. Date: YYYY-MM-DD
2. Status: proposed
3. Context: <why this decision is needed now — link to the sprint card or plan doc>
4. Decision: <one paragraph, active voice, what we WILL do>
5. Alternatives considered:
   1. <alternative 1>: <one-line trade-off>
   2. <alternative 2>: <one-line trade-off>
6. Positive consequences:
   1. <concrete benefit>
7. Negative consequences:
   1. <concrete cost>
8. Mitigation plan:
   1. <addresses negative consequence 1>
```

## Cross-links to add

- If the ADR introduces a pattern affecting Unity C# code, mention it in [AGENTS.md](../../AGENTS.md) "Patterns to follow" section in the same change.
- If the ADR changes numeric tuning, update [variables_gameplay.md](../../plan/04_technical/variables_gameplay.md).
- If the ADR supersedes a previous one, set the old ADR's `Status: deprecated` and add a back-reference.

## Forbidden

- Do **not** create a separate file per ADR — append to the existing `adr.md`.
- Do **not** invent a new template structure.
- Do **not** mark a new ADR as `accepted` without explicit user confirmation.
- Do **not** write the ADR in French.

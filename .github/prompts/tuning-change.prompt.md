---
description: "Apply a tuning value change end-to-end: update variables_gameplay.md, bump tuning_version, update the TuningConfig ScriptableObject, log the change."
argument-hint: "Variable name and new value (e.g. 'endurance_regen_rest_per_s = 7')"
agent: "agent"
---

# Apply a tuning change

Single source of truth for numeric gameplay values:
[plan/04_technical/variables_gameplay.md](../../plan/04_technical/variables_gameplay.md).
Architectural rule: ADR-003 — values are exposed via the `TuningConfig` `ScriptableObject`,
read-only at runtime.

## Required steps (in order)

1. **Validate the variable exists** in [variables_gameplay.md](../../plan/04_technical/variables_gameplay.md). If the name is new, ask the user which section it belongs to (Endurance, Grip, Local Fatigue, Score, etc.) and add it there with the documented convention (`name = default` then tested range in parentheses).

2. **Check the new value is inside the tested range** documented next to the variable. If outside, stop and ask the user to either:
   - widen the tested range (and justify), or
   - pick a value inside the range.

3. **Update `variables_gameplay.md`**: replace the default. Keep the range untouched unless explicitly broadened.

4. **Bump `tuning_version`** in the `TuningConfig` `ScriptableObject` asset under `unity/Assets/BoulderingGym/Data/` (look for `TuningConfig*.asset`). Increment the integer field — do not invent a semver. If the field is missing, surface this as a blocker referencing ADR-003.

5. **Update the C# default** if the variable is exposed as a `[SerializeField]` on `TuningConfig`. The `.asset` value is authoritative — never duplicate the number in another C# file.

6. **Search for hard-coded occurrences** of the old value across `unity/Assets/BoulderingGym/Scripts/**/*.cs`. Any hit is a bug — flag it and replace by a `TuningConfig` read.

7. **Log the change** in the active sprint card under a `## Tuning Changes` section:
   - Variable name
   - Old → new
   - `tuning_version` bump (X → Y)
   - Rationale (one sentence)

8. **Run EditMode tests** for the affected systems (Endurance, Grip, Fatigue, etc.). If a test pins the old number, update it and explain why in the test commit message.

## Forbidden

- Do **not** change a number in C# without updating `variables_gameplay.md`.
- Do **not** bump `tuning_version` without an accompanying value change.
- Do **not** modify the tested range without an explicit user decision.
- Do **not** create a new `TuningConfig` asset — there is exactly one.
- Do **not** commit French in the sprint card change log.

## Output

End with a summary table:

| Variable | Old | New | tuning_version | Sprint card |
| --- | --- | --- | --- | --- |
| `<name>` | `<old>` | `<new>` | `<X → Y>` | [link](../../Implementation/sprints/sprint_X_Y.md) |

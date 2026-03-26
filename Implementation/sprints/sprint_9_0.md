# Sprint 9.0 Checklist (5 days)

## Sprint Objective
Build routes R4-R5, finalize difficulty curve, and complete keyboard rebinding UX.

## Definition of Done
1. R4 and R5 playable on proper wall panels (overhang, roof).
2. Route color coding (blue for R4, red for R5).
3. Easy/Medium/Hard difficulty pass on all 5 routes.
4. Complete `Options > Controls` keyboard rebinding.
5. Score and debrief stable on all routes.

## Day 1
1. Build strong overhang + roof panels for R4/R5.
2. R4 blockout (overhang, requires heel hook/drop knee, blue color).
3. Add gaston and undercling holds to R4.

## Day 2
1. R5 blockout (roof section, all techniques, red color).
2. Add knee bar and bicycle opportunities to R5.
3. Verify technique unlock requirements matched by route difficulty.

## Day 3
1. Easy/Medium/Hard difficulty pass on all 5 routes.
2. Adjust penalties and tolerance per difficulty.
3. Full QA cross-combinations (mode × difficulty × route).

## Day 4
1. Build controls UI (`Options > Controls`): action list + key capture.
2. Detect conflicts + block invalid assignments.
3. Persist bindings (`input_bindings.json`), apply without restart.

## Day 5
1. Long internal test session (60 min across all 5 routes).
2. Fix top 5 anomalies.
3. Pre-external playtest stabilization.

## Validation Criteria
1. Completion gap: Easy > Medium > Hard.
2. Runs stay within the 5-15 min target.
3. R5 perceived as hard but fair.
4. Player rebinds 3 actions in under 2 minutes.
5. No restart needed to apply keys.

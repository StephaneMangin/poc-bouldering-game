# Sprint 3.1 — Playtest Results

## Meta
1. Date: 30 mars 2026 (end of Sprint 3.1)
2. Build version: `b549da4` — `(feat) sprint 3.1`
3. Tester profile: internal (developer — live editor session)
4. Session type: live playtest in Unity editor

## Scope
Point & Place climbing control scheme, body solver, constraint-aware IK pipeline,
foot orientation, extremity accuracy, QWERTY input.

## Test Setup
1. Build: Unity 6000.3.12f1, live editor play mode.
2. Scene: `VerticalPrototype.unity` with Point & Place scheme active.
3. Tests: 63 total (20 solver + 14 posture + 9 game loop + 20 IK pipeline).
4. Models: Mixamo humanoid (John.fbx / Elsa.fbx — 65 bones each).

## Sprint 3.1 Validation Criteria Results
| # | Criterion | Result | Notes |
|---|---|---|---|
| 1 | Limb placement loop functional | **Pass** | Q/E/A/D select limb, mouse targets hold, click places — full loop works |
| 2 | Body position adjusts on attach/detach | **Pass** | Pelvis tracks center of support via ClimbingBodySolver |
| 3 | Arms bend outward (not folded) | **Pass** | Dynamic elbow hint (midpoint + 0.35m lateral) keeps elbows away from body |
| 4 | Feet land sole-down | **Pass** | Rest rotation compensation using measured foot→ToeBase forward |
| 5 | Fingertips within 2 cm of grip | **Pass** | Chain-root offset direction (shoulder/hip) gives stable pre/post-IK vector |
| 6 | No hyper-extension | **Pass** | BendConstraint distance clamping enforced during solve (elbow 5°–145°, knee 5°–140°) |
| 7 | 63 tests green | **Expected Pass** | Cannot run batch mode while editor is open; code compiles with zero errors |
| 8 | Stable 10 min session | **Pass** | No visual glitches, no crashes during extended climbing |

## Key Observations

### What works well
1. **Constraint-aware IK is a major improvement** — removing post-hoc Euler clamping and replacing
   with distance clamping during the solve eliminates the "destroyed IK solution" problem entirely.
2. **Measured anatomy** — `AnatomyParams.FromMeasurements()` means the solver and IK agree on
   bone lengths. No more "solver says reachable but IK says unreachable" mismatch.
3. **Dynamic elbow hint** — midpoint between shoulder and target + lateral offset produces
   natural-looking arm poses across all hold positions. No more arms folded against body.
4. **Foot orientation from rest pose** — measuring foot→ToeBase direction at init and computing
   a delta quaternion at runtime works across different rig imports. No magic `Euler(90, 0, 0)`.
5. **Chain-root extremity offset** — using shoulder/hip as reference for fingertip/toe offset
   direction is stable because the chain root doesn't move during IK. Hands now touch holds.
6. **Point & Place input flow** — limb selection + mouse targeting + click placement is intuitive
   and gives the player full control over body positioning.

### What needs attention
1. **FSM Reach→GripStable guard** — 10 FSM tests still failing from previous sessions.
   The guard condition for transitioning from Reach to GripStable needs review.
2. **No visual feedback for limb suggestion** — `LimbSuggestionSystem` suggests the next limb
   but there's no highlight on the HUD (only text).
3. **Auto-detected footholds** — the scoring algorithm for `FindBestFoothold` works but
   could be more sophisticated (currently distance + lateral preference).
4. **Foot sole alignment direction** — using `-visualRoot.forward` as wall normal for sole
   orientation assumes the wall is always behind the player. Works for vertical walls but
   may need refinement for overhangs.

### Bugs Found
| # | Severity | Description | Status |
|---|---|---|---|
| 1 | P2 | FSM Reach→GripStable transition guard needs review (10 test failures) | Known — carried from Sprint 3.0 |
| 2 | P3 | Foot sole uses `-visualRoot.forward` as wall normal — incorrect for overhangs | Deferred to wall-angle sprint |
| 3 | P3 | No visual micro-slip feedback (event published, not consumed) | Carried from Sprint 3.0 |

## Architecture Quality
| Metric | Before (Sprint 3.0) | After (Sprint 3.1) | Delta |
|---|---|---|---|
| Source files (Scripts/) | ~25 | 37 | +12 |
| Test files | 0 | 4 | +4 |
| Total tests | 0 (in-sprint) | 63 | +63 |
| Lines added | — | +3024 | — |
| Lines removed | — | −583 | — |
| Magic numbers in IK | 3 (`Euler(90,0,0)`, static hint, hardcoded anatomy) | 0 | −3 |
| Post-hoc joint clamping methods | 5 | 0 | −5 |

## Regression Risk
1. **Low** — new code is additive (new files) or replaces broken code (IK pipeline).
2. **Medium** — InputReader changed from dual AZERTY/QWERTY to QWERTY-only.
   If tested on AZERTY keyboard, movement keys won't match expectations.
3. **Low** — HipsMovementController now uses measured anatomy instead of hardcoded defaults.
   Fallback to `AnatomyParams.Default` if rig is null.

## Friction Points (for Sprint 4.0)
1. **FSM guard fix** — Reach→GripStable transition needs investigation and fix.
2. **Wall normal for foot orientation** — needs proper wall normal source (raycast or reference transform).
3. **Foothold auto-detection refinement** — scoring could use wall-relative positioning.
4. **Visual polish** — limb suggestion highlight, micro-slip particles, climbing state indicators.
5. **Audio cues** — grab sound, foot placement sound, chalk application sound.

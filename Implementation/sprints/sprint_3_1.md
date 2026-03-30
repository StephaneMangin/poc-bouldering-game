# Sprint 3.1 Checklist (5 days)

## Sprint Objective
Deliver a complete Point & Place climbing control scheme with anatomically correct body posing:
body solver, IK pipeline, limb selection, hold targeting, and visual feedback.

## Definition of Done
1. Player can select a limb (Q/E/A/D), target a hold (mouse), and place (click) — full loop.
2. Body position (pelvis/root) adjusts dynamically based on held limbs.
3. IK solver produces anatomically correct arm and leg poses (no hyper-extension, no folded arms).
4. Feet land sole-down on footholds (not instep/dorsum).
5. Fingertips/toe tips land on grip points (not offset into the air).
6. Joint constraints (elbow 5°–145°, knee 5°–140°) enforced during IK solve.
7. Input is QWERTY-only (WASD movement, Q/E/A/D limbs).
8. 59+ tests covering body solver, IK pipeline, posture physics, and game loop.

## Key Deliverables

### Day 1 — Body Solver Foundation
1. Create `ClimbingBodySolver` (pure static solver: pelvis position from limb contacts).
2. Create `ClimbingBodySolverTests` (20 tests: pelvis placement, reach validation, root position).
3. Create `LimbId` enum and `LimbSuggestionSystem` (next-limb suggestion logic).
4. Create `HoldTargetingSystem` (mouse raycast → hold targeting with visual feedback).

### Day 2 — ClimberIKController + Point & Place Loop
1. Refactor `ClimberIKController` with full 4-limb IK (arms + legs).
2. Refactor `PlayerHandsController` for all 4 limbs (hands + feet).
3. Wire `PlayerStateMachineDriver` for Point & Place input flow.
4. Integrate `HipsMovementController` with body solver for climbing body follow.
5. Create `LimbSelectionHUD` (IMGUI overlay for active limb + prompts).

### Day 3 — IK Pipeline Hardening
1. Fix TwoBoneIKSolver stale end-position bug (use `end.position` post-rotation).
2. Implement dynamic elbow hint (midpoint + lateral offset, not static toward-wall).
3. Create `ClimbingPosturePhysicsTests` (14 tests: posture, COG, wall angles).
4. Create `ClimbingGameLoopTests` (9 tests: grab/release/fall/placement flow).

### Day 4 — Constraint-Aware IK Architecture
1. Create `ClimbingJointConstraints` (`BendConstraint` struct with law-of-cosines distance clamping).
2. Rewrite `TwoBoneIKSolver.Solve` with `constraintMinDist`/`constraintMaxDist` parameters.
3. Add `AnatomyParams.FromMeasurements()` — measure real bone lengths from rig at init.
4. Remove post-hoc Euler joint clamping (`ApplyJointLimits`, `ClampLimbSwing`, `ClampHingeJoint`).
5. Integrate constraints into `ClimberIKController` (precompute distance ranges at init).
6. Update `HipsMovementController` to use measured anatomy (not hardcoded defaults).
7. Create `IKPipelineTests` (16 tests + 4 TestCases: end-effector accuracy, elbow direction, constraints).

### Day 5 — Foot Orientation + Extremity Accuracy + Polish
1. Fix foot orientation: measure rest rotation + ToeBase forward at init, compute delta quaternion.
2. Fix extremity offset: use chain root (shoulder/hip) for direction instead of pre-IK bone position.
3. Set input to QWERTY-only (WASD movement).
4. Add 3 extremity offset tests to IKPipelineTests.
5. Internal playtest and tuning.

## Architecture Decisions
1. **Constraints during IK, not after** — bend angle limits are converted to distance ranges
   (law of cosines) and clamped during the solver pass. No post-hoc Euler decomposition.
2. **Measured anatomy** — bone lengths and rest rotations are measured from the actual rig at
   initialization, not hardcoded. Works across different Mixamo models.
3. **Chain-root extremity offset** — the IK target for fingertips/toes is offset using the
   chain root (shoulder/hip) direction, which is stable pre/post-IK. The end-effector's
   pre-IK position gives a wrong direction.

## Validation Criteria
1. Limb placement loop is functional: select limb → target hold → click → limb attaches.
2. Body position adjusts when limbs attach/detach (pelvis follows center of support).
3. Arms bend outward (elbows away from body), not folded against torso.
4. Feet land sole-down on footholds (rest rotation compensation, no magic Euler numbers).
5. Fingertips within 2 cm of grip point after IK solve.
6. No hyper-extension: arm/leg distance respects constraint bounds.
7. 63 tests green (20 solver + 14 posture + 9 game loop + 16+4 IK pipeline).
8. Stable 10-minute climbing session without visual glitches.

## Test Summary
| Suite | File | Tests | Coverage |
|---|---|---|---|
| Body Solver | `ClimbingBodySolverTests.cs` | 20 | Pelvis placement, reach clamping, root position, anatomy measurement |
| Posture Physics | `ClimbingPosturePhysicsTests.cs` | 14 | Extension ratios, extremity IK targets, COG offset, wall angle effects |
| Game Loop | `ClimbingGameLoopTests.cs` | 9 | Grab/release flow, fall transitions, placement validation |
| IK Pipeline | `IKPipelineTests.cs` | 20 | End-effector accuracy, elbow direction, bend constraints, extremity offset |
| **Total** | | **63** | |

## Files Changed (vs Sprint 3.0)
- **New** (12 files): ClimbingBodySolver, ClimbingJointConstraints, LimbId, LimbSuggestionSystem,
  HoldTargetingSystem, LimbSelectionHUD, + 4 test suites + metas
- **Modified** (12 files): TwoBoneIKSolver, InputReader, ClimberIKController, PlayerHandsController,
  HipsMovementController, PlayerStateMachineDriver, ClimberPostureController,
  ClimberFeedbackController, ClimberVisualOrchestrator, HoldComponent,
  PrototypeBootstrapper, ClimbingStateOverlay
- **Net delta**: +3024 / −583 lines

# Sprint 2.0 Addendum — Post-Sprint Review

## Outcome
Objective achieved with unplanned architectural pivot. The sprint required a major architecture refactoring (unanticipated prerequisite) and a Rigidbody → CharacterController migration before the physics model could be integrated.

## Definition of Done — Results
| # | Criterion | Status | Evidence |
|---|---|---|---|
| 1 | COG computed from contact points in real-time | ✅ | `ClimbingPhysicsModel.ComputeCOG()` (330 LOC) |
| 2 | Weight distribution updates per-limb based on COG | ✅ | `ComputeWeight()` (hand/foot ratios) |
| 3 | Wall angle detected and scaling multipliers applied | ✅ | `WallAngleCategory` enum (Slab→Roof), `GetMultipliers()` |
| 4 | PhysicMaterials assigned to wall, holds, crash pad | ✅ | `PhysicsMaterialAssigner.cs` (134 LOC, Editor tool) |
| 5 | Body-wall contact detection on slab | ✅ | `bodyWallContactThreshold` + `slabBodyFriction` in `TuningConfig` |
| 6 | Debug overlay: COG + stability polygon | ✅ | `PhysicsDebugOverlay.cs` (149 LOC) |

## Actual Execution Timeline

### Day 1–2: Architecture Pivot (unplanned)
The monolithic Climbing feature was the primary blocker. States, camera, locomotion, physics, visuals, and player FSM were all coupled inside a single feature folder. This made the physics model impossible to integrate cleanly.

**Refactoring — 1 feature → 6 features:**

| Before (S1.1) | After (S2.0) |
|---|---|
| `Features/Climbing/Domain/ClimbingState.cs` | `Features/Player/Domain/PlayerState.cs` |
| `Features/Climbing/Domain/ClimbingContext.cs` | `Features/Player/Domain/PlayerContext.cs` |
| `Features/Climbing/Domain/ClimbingTrigger.cs` | `Features/Player/Domain/PlayerTrigger.cs` |
| `Features/Climbing/Domain/States/*` | `Features/Player/Domain/States/*` |
| `Features/Climbing/Application/ClimbingStateMachine.cs` | `Features/Player/Application/PlayerStateMachine.cs` |
| `Features/Climbing/Presentation/Camera/*` | `Features/Camera/Presentation/*` |
| `Features/Climbing/Presentation/Locomotion/*` | `Features/Locomotion/Presentation/*` |
| `Features/Climbing/Presentation/Visual/*` | `Features/Visual/Presentation/*` |
| `Features/Climbing/Presentation/Player/*` | `Features/Player/Presentation/*` |
| _(none)_ | `Features/Physics/Domain/*` |
| _(none)_ | `Features/Physics/Presentation/*` |

**Core extractions:**
1. Generic `StateMachine<TContext, TTrigger>` extracted to `Core/StateMachine/` with `IState<TContext>` interface.
2. `PrototypeBootstrapper` consolidated (merged `Sprint1_0PrototypeBootstrapper` + `Sprint1_1PrototypeBootstrapper`).

**Rigidbody → CharacterController migration:**
1. `GroundRootMotionDriver` (Rigidbody + Root Motion) replaced by `GroundLocomotionController` (CC-based, manual gravity).
2. SphereCast backup grounding added for edge cases where `cc.isGrounded` is unreliable.
3. `_verticalVelocity` properly initialized; `groundedPull` applied every grounded frame.

### Day 3: Physics Model Implementation
| File | LOC | Purpose |
|---|---|---|
| `ClimbingPhysicsModel.cs` | 330 | Pure domain: COG computation, weight distribution, wall angle multipliers, body-wall contact |
| `PhysicsData.cs` | 71 | Data structs: `ContactPoint`, `COGResult`, `WeightDistribution`, `WallAngleMultipliers`, `PhysicsSnapshot` |
| `PhysicsModelDriver.cs` | 227 | MonoBehaviour driver: connects domain model to Unity runtime |
| `PhysicsMaterialAssigner.cs` | 134 | Editor tool: assigns PhysicMaterials to wall/holds/crash pad |
| `PhysicsDebugOverlay.cs` | 149 | Debug overlay: COG position + weight + wall category |
| `WallPanelComponent.cs` | — | Wall panel component for wall angle detection |
| **Total** | **911** | |

`TuningConfig` extended with 18 new wall angle constants (endurance/fatigue/grip drain multipliers for Slab through Roof).

### Day 4: Test Infrastructure
| File | LOC | Mode | Coverage |
|---|---|---|---|
| `ClimbingPhysicsModelTests.cs` | 409 | EditMode | COG, weight, multipliers, body contact |
| `StateMachineTests.cs` | 365 | EditMode | Transitions, guards, callbacks |
| `CameraTests.cs` | 197 | PlayMode | Follow, orbit, recenter |
| `HoldSelectionTests.cs` | 251 | PlayMode | Grab range, hand selection |
| `GroundLocomotionTests.cs` | 287 | PlayMode | CC grounding, gravity, walk |
| `FSMTransitionTests.cs` | 339 | PlayMode | Full FSM flow |
| `PostureLandingTests.cs` | 148 | PlayMode | Posture + landing |
| `ClimbingScenarioTests.cs` | 268 | PlayMode | End-to-end functional scenarios |
| `TestSceneFixture.cs` | 124 | PlayMode | Reusable fixture |
| **Total** | **2 388** | **119 tests** | **59 EditMode + 60 PlayMode, 0 failures** |

### Day 5: Ground Alignment Debug + Playtest
Critical bug: **character walking under the ground** after CC migration.

**Root causes identified (3):**
1. `heightFromFeet: 0` on all 6 Male Walk `.fbx.meta` clips (Female Walk had `1`) — animation origin was at hips instead of feet.
2. `Mannequin.localPosition.y = 0.14486` — residual offset from initial model setup, invisible in hierarchy.
3. CC dimensions `height=1.8, center.y=0.9` did not match the scaled model (BodyVisual scale `0.7, 0.9, 0.7` → effective height 1.62m).

**Fixes applied:**
1. `heightFromFeet: 0` → `1` on 6 Male Walk clips.
2. `Mannequin.localPosition.y` → `0`.
3. CC: `height=1.62, center.y=0.81`.
4. SphereCast backup grounding in `GroundLocomotionController.CheckGrounded()`.
5. Missing script `ClimberVisualController` removed from `VerticalPrototype.unity`.
6. `EditorClassIdentifier` corrected: `ClimbingStateMachineDriver` → `PlayerStateMachineDriver`.

Debug methodology documented in [`Implementation/methods/debug_character_ground_alignment.md`](../methods/debug_character_ground_alignment.md).

### Assets & Documentation
1. 8 new animation clips (landing, start/stop walking, walking backward — Male + Female).
2. 13 animation tutorial summaries in `Implementation/knowledge/animation/`.
3. `sprint_2_0_analysis.md`: detailed pre-sprint analysis (39 scripts, identified gaps).
4. `debug_character_ground_alignment.md`: debug methodology document.

## Validation Criteria — Results
| # | Criterion | Result | Notes |
|---|---|---|---|
| 1 | COG moves visibly when repositioning hands/feet | ✅ Pass | `ComputeCOG()` tested (409 physics tests) |
| 2 | Climbing on overhang feels harder than vertical | ⚠️ Partial | Multipliers in place (`GetMultipliers()`), not yet connected to endurance runtime |
| 3 | PhysicMaterials produce distinguishable friction | ✅ Pass | `PhysicsMaterialAssigner` operational |
| 4 | Stable build on 10 min of continuous climbing | ✅ Pass | Full playtest session completed without crash |

## Plan vs. Actual
| Planned (5 days) | Actual (5 days) |
|---|---|
| Day 1: COG computation | Day 1–2: Architecture refactoring (6 features split, CC migration) |
| Day 2: Weight distribution + wall angle | Day 3: Physics model (COG + weight + wall angle + PhysicMaterials) |
| Day 3: PhysicMaterials + body-wall contact | Day 4: Test infrastructure (119 tests) |
| Day 4: Debug overlay + COG-endurance hookup | Day 5: Ground alignment bug fix + playtest |
| Day 5: Tuning + playtest | _(absorbed into Day 5)_ |

**Key deviation:** The architecture refactoring consumed 2 days but was a prerequisite — the monolithic Climbing feature would have made physics integration unmaintainable. This is technical debt payoff from S1.0/S1.1.

## Metrics
- **Code**: 9,699 insertions, 3,236 deletions (104 significant files).
- **Tests**: 119 (59 EditMode + 60 PlayMode), 0 failures.
- **Architecture**: 1 monolithic feature → 6 independent features + Core/StateMachine.
- **Migration**: Rigidbody → CharacterController (manual gravity, SphereCast backup).
- **Physics model**: 911 LOC (domain + driver + editor tool + overlay).
- **Test code**: 2,388 LOC across 9 test files.

## Risks Materialized
1. **Architecture debt (realized)**: Monolithic Climbing feature blocked clean physics integration. Cost: 2 days unplanned refactoring.
2. **CC migration side effects (realized)**: `heightFromFeet` and model offset bugs hidden by Rigidbody's different grounding model. Cost: 1 day debugging.
3. **Endurance integration deferred**: Wall angle multipliers not connected to gameplay — carries to S3.0.

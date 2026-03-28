# Sprint 2.0 - Playtest Results

## Meta
1. Date: end of Sprint 2.0
2. Build version: post-commit WIP (rewritten into atomic commits)
3. Tester profile: internal (developer)
4. Session duration: ~45 min (internal mini-playtest + debug sessions)

## Scope
Internal mini-playtest on `VerticalPrototype.unity` validating Sprint 2.0 deliverables:
physics model (COG, weight distribution, wall angle multipliers), CharacterController migration,
architecture refactoring (6 features), ground alignment fix, 119 tests.

## Baseline Values (validated)
1. `grab_range = 1.60`
2. `hips_move_speed = 2.00`
3. `camera_follow_smooth = 0.60`
4. `ground_locomotion`: CharacterController-based (migrated from Rigidbody)
5. `cc_height = 1.62`, `cc_center_y = 0.81`, `cc_radius = 0.3`, `cc_skin_width = 0.08`
6. `body_wall_contact_threshold`: configurable in `TuningConfig`
7. `slab_body_friction`: configurable in `TuningConfig`
8. `wall_angle_multipliers`: Slab (0.7/0.6/0.5) → Roof (2.0/2.5/3.0)

## Test Setup
1. Build: local editor play mode.
2. Scene: `VerticalPrototype.unity` (flat wall 4m×6m + 15° overhang 4m×4m, 10 holds in 3 lanes).
3. Input: keyboard (AZERTY/QWERTY tolerant).
4. Characters: Mixamo humanoids (John.fbx male, Elsa.fbx female).
5. Tests: 119 (59 EditMode + 60 PlayMode), all green.

## Executed Scenarios
1. Ground approach: walk toward wall with CharacterController, verify grounding.
2. Walk cycle: idle → walk → stop, observe feet/ground stability.
3. Grab loop: grab holds across all 3 lanes, verify arm IK coherence.
4. Fall + reset: release all holds, observe CC fall, verify crash pad reset.
5. Camera orbit: orbit with middle mouse + recenter with `C`.
6. Physics overlay: enable debug overlay, observe COG/weight/wall category values.
7. Full loop: approach → grab → climb → fall → reset → re-approach.

## Sprint 2.0 Validation Criteria Results
| # | Criterion | Result | Notes |
|---|---|---|---|
| 1 | COG moves visibly when repositioning hands/feet | **Pass** | `ClimbingPhysicsModel.ComputeCOG()` tested (409 tests) |
| 2 | Climbing on overhang feels harder than vertical | **Partial** | Multipliers in place, not yet connected to endurance runtime |
| 3 | PhysicMaterials produce distinguishable friction | **Pass** | `PhysicsMaterialAssigner` operational, materials assigned |
| 4 | Stable build on 10 min of continuous climbing | **Pass** | Full session completed without crash |
| 5 | CC grounding stable (no float/clip) | **Pass** | SphereCast backup + heightFromFeet fix |
| 6 | Walk cycle natural after CC migration | **Pass** | Blend tree functional, feet on ground |
| 7 | 6-feature architecture coherent | **Pass** | No circular dependencies, clean imports |
| 8 | 119 tests green | **Pass** | 59 EditMode + 60 PlayMode, 0 failures |

## Key Observations
1. **What works well**:
	1. CharacterController provides more reliable grounding than Rigidbody — no more "character falls when walking".
	2. 6-feature architecture makes the code navigable and testable.
	3. Pure domain PhysicsModel (no MonoBehaviour) — easy to unit test.
	4. Physics debug overlay useful for validating COG computations in real-time.
	5. 119 tests provide a comfortable safety net for upcoming sprints.
2. **What creates confusion**:
	1. CC migration introduced a ground alignment bug (heightFromFeet=0 on Male Walk clips).
	2. Mannequin had a residual Y offset (0.14486) invisible to the naked eye in the hierarchy.
	3. CC dimensions (1.8/0.9) did not match the scaled model (effective 1.62m).
3. **Frustration moments**:
	1. Ground alignment debug consumed an entire day — 3 distinct root causes.
	2. Architecture pivot (unplanned) pushed physics work back by 2 days.
4. **Satisfaction moments**:
	1. Ground alignment bug resolved after systematic investigation (methodology documented).
	2. First green bar at 119 tests — confidence in build stability.
	3. COG debug overlay shows consistent values in real-time.

## Friction Points (carried from S1.1 + new)
1. **[S1.1 carry] Input discoverability still low** — needs contextual prompts or tutorial.
2. **[S1.1 carry] No hold color coding** — lanes spatially clear but not visually distinct.
3. **[S1.1 carry] Post-fall orientation** — player faces wrong direction after reset.
4. **[S1.1 carry] Ground-to-climb transition** — no smooth blend between locomotion and first grab.
5. **[New] Wall angle multipliers not connected to endurance** — physics model computes but does not yet penalize gameplay.
6. **[New] No player-facing COG feedback** — only visible via debug overlay, not in-game.
7. **[New] CC skinWidth artifacts** — occasional jitter on wall-distance clamp edge cases.

## Bug Fixes Applied (Sprint 2.0)
1. `heightFromFeet: 0` → `1` on 6 Male Walk `.fbx.meta` clips (character under the ground).
2. `Mannequin.localPosition.y: 0.14486` → `0` (residual offset eliminated).
3. CC dimensions: `height 1.8 / center.y 0.9` → `1.62 / 0.81` (match scaled model 0.7/0.9/0.7).
4. SphereCast backup grounding added to `GroundLocomotionController.CheckGrounded()`.
5. Missing script `ClimberVisualController` removed from `VerticalPrototype.unity`.
6. `EditorClassIdentifier` corrected: `ClimbingStateMachineDriver` → `PlayerStateMachineDriver`.
7. `_verticalVelocity` properly initialized in `GroundLocomotionController`.

## Recommended Actions
1. **P0** (fix before next sprint):
	1. Connect `WallAngleMultipliers` to endurance runtime system.
2. **P1** (important):
	1. Add player-facing COG visual feedback (not just debug overlay).
	2. Fix post-fall orientation (face wall on reset).
	3. Smooth ground-to-climb transition blend.
3. **P2** (improvement):
	1. Hold color coding by route (planned S8.0).
	2. Audio system (planned S6.0).
	3. Contextual input prompts.

## Decision
1. **Go** — Sprint 2.0 objectives met. Physics model foundation in place (COG, weight, wall angle, PhysicMaterials).
   Architecture significantly strengthened by the 6-feature pivot + CC migration.
   Sprint scope was broader than planned (refactoring + migration + bugs), but the outcome is solid.

## Free Notes
Sprint 2.0 was a major pivot: the initial plan (5 days purely physics) transformed into architectural refactoring (2 days) + CC migration (1 day) + physics model (1 day) + ground alignment debug (1 day).

This pivot was necessary: the monolithic Climbing architecture and Rigidbody were structural blockers for future work. Technical debt accumulated in S1.0/S1.1 was paid off.

Debug methodology documented in `Implementation/methods/debug_character_ground_alignment.md` — reusable for future alignment issues.

Test coverage (119) is a major asset: each feature has its own dedicated test suite.
The physics model (`ClimbingPhysicsModel`) is pure domain, decoupled from Unity — easily extensible in S3.0.

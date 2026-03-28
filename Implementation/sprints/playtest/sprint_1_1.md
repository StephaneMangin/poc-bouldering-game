# Sprint 1.1 - Playtest Results

## Meta
1. Date: fin Sprint 1.1
2. Build version: post-commit `aa64157`
3. Tester profile: internal (développeur)
4. Session duration: ~30 min (mini-playtest interne)

## Scope
Internal mini-playtest on `VerticalPrototype.unity` validating Sprint 1.1 deliverables:
humanoid mannequin, lane hold organization, physics fall, camera orbit, ground locomotion.

## Baseline Values (validated)
1. `grab_range = 1.60`
2. `hips_move_speed = 2.00`
3. `camera_follow_smooth = 0.60`
4. `camera_orbit`: middle mouse click (`Mouse3`), yaw/pitch around mannequin
5. `ground_locomotion`: 2D Freeform Cartesian blend tree (`movex`, `movez`)
6. `foot_ik`: raycast-based ground sampling via `OnAnimatorIK()`
7. `fall_reset_y_threshold = -2` (position snap to ResetAnchor)

## Test Setup
1. Build: local editor play mode.
2. Scene: `VerticalPrototype.unity` (flat wall 4m×6m + 15° overhang 4m×4m, 10 holds in 3 lanes).
3. Input: keyboard (AZERTY/QWERTY tolerant).
4. Characters: Mixamo humanoids (John.fbx male, Elsa.fbx female).

## Executed Scenarios
1. Ground approach: walk toward wall, verify wall-distance clamp, observe walk cycle.
2. Grab loop: grab holds across left/center/right lanes, verify IK arm coherence.
3. Fall + reset: release all holds mid-climb, observe physics fall, verify crash pad reset.
4. Camera orbit: hold middle mouse click, orbit around mannequin, recenter with `C`.
5. Full loop: approach → grab → climb lanes → fall → reset → re-approach.

## Sprint 1.1 Validation Criteria Results
| # | Criterion | Result | Notes |
|---|---|---|---|
| 1 | Lane progression identifiable without guidance | **Pass** | Left/center/right lanes readable |
| 2 | Falls readable and physically coherent | **Pass** | Rigidbody fall, no teleport/clip loop, reset at y < -2 |
| 3 | Arm behavior human-readable during grabs | **Pass** | Two-Bone IK, elbow 5-150°, shoulder -85/95°, no inversion |
| 4 | No regression on grab → stabilize → release loop | **Pass** | S1.0 loop intact |
| 5 | Camera orbit with middle click + return to follow | **Pass** | Yaw/pitch orbit, `C` recenters |
| 6 | Ground approach without teleport/wall crossing | **Pass** | Wall-distance clamp in GroundRootMotionDriver |
| 7 | Grounded movement perceived as natural walking | **Pass** | Alternating legs, slight arm swing, torso bob |
| 8 | Feet stay grounded (no float/clip) | **Pass** | OnAnimatorIK() foot IK active |
| 9 | Human-like bend ranges (no hyperextension) | **Pass** | Joint limits enforced (elbow 5-150°, knee 8-160°) |

## Key Observations
1. **What works well**:
	1. Mannequin significantly improves readability vs. primitive capsule.
	2. Lane organization makes route intent clear.
	3. Walk cycle feels natural — alternating legs + arm swing.
	4. Camera orbit is smooth and useful for wall reading.
	5. Fall physics feel weighted and predictable.
2. **What creates confusion**:
	1. No visual distinction between holds of different lanes (all same color/material).
	2. No audio feedback — climbing is silent, reduces immersion.
	3. Grab range sphere is invisible to the player — success/failure not clearly telegraphed.
3. **Frustration moments**:
	1. After fall reset, player faces away from wall (orientation not restored).
	2. Transitions between ground locomotion and climbing feel abrupt (no blend).
4. **Satisfaction moments**:
	1. First successful lane traversal feels rewarding (clear spatial progression).
	2. Camera orbit gives a good "scouting" feel before climbing.

## Friction Points (carried from S1.0 + new)
1. **[S1.0 carry] Input discoverability still low** — needs contextual prompts or tutorial.
2. **[S1.0 carry] Grab failure cause not explicit** — distance vs. angle vs. occupied hand.
3. **[New] No hold color coding** — lanes are spatially clear but not visually distinct.
4. **[New] Silent climbing** — zero audio damages immersion; planned for S6.0.
5. **[New] Post-fall orientation** — player faces wrong direction after reset.
6. **[New] Ground-to-climb transition** — no smooth blend between locomotion and first grab.

## Bug Fixes Applied (Day 5)
1. FSM dead state: `Falling + Landed` → `IdleGround` (bypass `RunEnd` trap).
2. `Rigidbody` constraints: `FreezePositionZ` only during fall.
3. Material memory leaks: `MaterialPropertyBlock` replaces `.material.color`.
4. Camera orbit on `Mouse3` with yaw/pitch; `C` resets orbit.
5. Foot IK re-enabled via `OnAnimatorIK()`.
6. Wall-distance clamp centralized in `GroundRootMotionDriver`.
7. `HoldRegistry` injected via bootstrapper (removed `FindObjectsByType`).
8. `ClimbingStateMachineDriver.Update()` simplified (single null guard).

## Recommended Actions
1. **P0** (fix before next sprint):
	1. Fix post-fall orientation (face wall on reset).
2. **P1** (important):
	1. Add hold color coding by route (planned S8.0).
	2. Add grab range visual hint (highlight reachable holds).
	3. Smooth ground-to-climb transition blend.
3. **P2** (improvement):
	1. Contextual input prompts (planned in later sprint).
	2. Audio system (planned S6.0).

## Decision
1. **Go** — Sprint 1.1 objectives fully met. Foundation solid for S2.0 physics model.

## Free Notes
Architecture significantly improved by Day 5 code review. Clean separation:
Domain (states, events) → Application (controllers, orchestrators) → Presentation (camera, visuals, locomotion).
Event bus functional. TuningConfig centralized. No magic numbers in runtime code.
Ready for physics model integration in S2.0.

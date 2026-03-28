# Sprint 1.1 Checklist (5 days)

## Sprint Objective
Complete the Sprint 1.0 extension requested during Sprint 1.0 execution:
1. Replace primitive player proxy with a humanoid mannequin (head, torso, arms, forearms, hands, pelvis, legs, feet).
2. Reorganize hold placement into readable route lanes.
3. Add physics-driven falling behavior with predictable reset.
4. Add camera orbit control with middle mouse click (`Mouse3`) around the mannequin center of mass.
5. Add ground locomotion so the climber can approach/leave the wall before grabbing while staying within a controlled wall-distance band.
6. Make grounded locomotion read as human walking (alternating leg cycle, slight natural arm swing).
7. Drive locomotion with a `2D Freeform Cartesian` Animator blend tree (`movex`, `movez`) and ground-aware foot IK to preserve natural contact.

## Definition of Done
1. The player uses a humanoid mannequin with clearly identifiable human body segments (not a capsule/proxy shape).
2. Arm movement during reach/grab/fall is physically coherent:
	1. no impossible elbow inversion,
	2. no major clipping through wall/torso on standard grabs,
	3. shoulder-elbow-hand chain remains continuous and readable.
3. Mannequin supports at least `idle`, `reach/grab`, `falling` state poses.
4. Holds are organized into clear lanes (`left`, `center`, `right`) with stable spacing.
5. Falling is driven by `Rigidbody` and reset flow is deterministic.
6. Existing Sprint 1.0 climbing loop remains stable after integration.
7. Holding middle mouse click rotates the camera around the mannequin center of mass without breaking follow readability.
8. In `IdleGround`, move input allows the character to approach the wall and retreat from it without crossing the wall plane.
9. Arms and legs react to supports and targets instead of staying static in front of the wall.
10. Ground locomotion stays within tuned minimum and maximum distance bounds relative to the wall.
11. During `IdleGround` locomotion, walk gait is human-readable with alternating steps and slight opposite arm swing.
12. Foot placement uses ground sampling/IK so feet do not float or clip through the floor on uneven surfaces.
13. Joint motion stays within human-like limb limits (no obvious hyperextension or impossible twists).

## Day 1
1. Integrate humanoid mannequin prefab (full body) and base animator controller.
2. Verify rig mapping for shoulders, elbows, wrists, hips, knees, and feet.

## Day 2
1. Rework hold placement into route lanes (`left`, `center`, `right`).
2. Apply simple spacing/alignment rules for readability.

## Day 3
1. Implement `Rigidbody`-based fall controller.
2. Add reset anchor and reset conditions after fall.

## Day 4
1. Connect mannequin pose changes to climbing and fall transitions.
2. Add arm solving layer (Animation Rigging IK constraints) for left/right reach targets.
3. Tune constraints/weights to keep arm motion coherent with body and wall contact context.
4. Add camera orbit input on middle mouse click (`Mouse3`) centered on mannequin center of mass.
5. Validate lane readability during lateral and vertical movement.
6. Add and tune ground locomotion depth (toward/away from wall) while in `IdleGround`.
7. Add and tune wall-distance clamps to prevent wall penetration and over-retreat while grounded.
8. Add and tune procedural walk cycle (legs + slight arm swing + torso bob) for grounded movement.
9. Hook Animator locomotion parameters (`movex`, `movez`, `speed`, `is_grounded`) for `2D Freeform Cartesian` blend-tree transitions.
10. Add raycast-based foot placement and tune limb limits for stable human-like motion.

## Day 5
1. Run internal mini-playtest focused on humanoid readability, arm coherence, and fall coherence.
2. Document outcomes and top friction points in `Implementation/sprints/playtest/sprint_1_0.md`.
3. Code review pass — fix bugs and improve architecture:
	1. Fix FSM dead state: `Falling + Landed` now transitions directly to `IdleGround` (bypass `RunEnd` trap).
	2. Fix `Rigidbody` constraints: `FreezePositionZ` only active during fall (not when kinematic), allowing wall-approach movement.
	3. Fix material memory leaks: replace `.material.color` with `MaterialPropertyBlock` in `HoldComponent`, `ClimberVisualController` (body + hand markers).
	4. Implement camera orbit on middle mouse click (`Mouse3`) with yaw/pitch around look target; recenter (`C`) resets orbit.
	5. Re-enable foot IK on ground via `OnAnimatorIK()` using `Animator.SetIKPosition/Rotation` even when Animator-driven ground pose is active.
	6. Centralize wall-distance clamp in `GroundRootMotionDriver` (removed redundant clamp from `HipsMovementController` when root motion is active).
	7. Inject `HoldRegistry` into `ClimberVisualController` via bootstrapper (removed standalone `FindObjectsByType` scan).
	8. Simplify `ClimbingStateMachineDriver.Update()`: single `inputReader != null` guard instead of 7 repeated checks.

## Validation Criteria
1. Testers can identify intended lane progression without verbal guidance.
2. Falls are readable and physically coherent (no teleport/clip loop).
3. Arm behavior remains human-readable during grab transitions (no obvious anatomical break in normal conditions).
4. No blocking regression on the Sprint 1.0 `grab -> stabilize -> release` loop.
5. Testers can orbit the camera with middle click and return to a readable follow view.
6. Testers can start on ground, move toward the wall, and initiate the first grab without teleporting or crossing through the wall.
7. Testers perceive grounded movement as natural walking (no static limbs while moving).
8. On ramps/height variation, feet stay grounded and visually stable (no persistent foot floating/clipping).
9. During walk cycles, elbows and knees keep human-like bend ranges without obvious inversion.

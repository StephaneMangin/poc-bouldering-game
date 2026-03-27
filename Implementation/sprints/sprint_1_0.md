# Sprint 1.0 Checklist (5 days)

## Sprint Objective
Obtain a playable vertical prototype with:
1. Climbing test scene.
2. Basic controls (hands + hips movement).
3. Minimal FSM observable in debug.

## Definition of Done (Sprint 1.0)
1. The character can grab holds with left/right hand.
2. A simple `grab -> stabilize -> release` loop works without crash.
3. The camera stays readable on a vertical wall.
4. A debug overlay displays the current FSM state.
5. The project launches cleanly on the dev machine.

## Day 1 - Project Setup

### Tasks
1. Create Unity LTS project.
2. Install packages:
   1. Input System
   2. Cinemachine
   3. Animation Rigging
3. Structure folders (ADR-006 — layered architecture):
   1. `Assets/_Project/Scripts/Core/Events/`
   2. `Assets/_Project/Scripts/Core/Domain/`
   3. `Assets/_Project/Scripts/Features/Climbing/Domain/`
   4. `Assets/_Project/Scripts/Features/Climbing/Application/`
   5. `Assets/_Project/Scripts/Features/Climbing/Presentation/`
   6. `Assets/_Project/Scripts/Infrastructure/`
   7. `Assets/_Project/Scripts/UI/`
   8. `Assets/_Project/Data/Gameplay/Tuning/`
   9. `Assets/_Project/Prefabs/Gameplay/`
   10. `Assets/_Project/Scenes/`
4. Create `TuningConfig` ScriptableObject asset (ADR-003).
5. Create `VerticalPrototype.unity` scene with two wall segments: flat (~4 m × 6 m) + overhang (~4 m × 4 m, ~15°), 10–12 holds placed manually (ADR-012).

### Verification
1. Project compiles without error.
2. Base scene loads with active camera and both wall segments visible.
3. `TuningConfig` SO is assigned and readable in Inspector.

## Day 2 - Controls and Camera

### Tasks
1. Configure Input actions (keyboard + gamepad).
2. Implement simple hips movement.
3. Integrate Cinemachine for player follow.
4. Add camera recentering.

### Verification
1. Fluid movement without major jitter.
2. Camera does not obscure nearby holds.

## Day 3 - Holds, Hand Interaction and IK

### Tasks
1. Create `Hold` prefab with `HoldComponent` MonoBehaviour containing a `[Serializable] HoldData` struct (ADR-010):
   1. `hold_quality`
   2. `grip_texture_hold`
   3. `grip_point` Transform (IK target anchor)
2. Implement per-hand hold detection: `SphereOverlap` from each hand's `LeftHandAnchor` / `RightHandAnchor` transform, filtered by `grab_range` and `grab_angle_max` from `TuningConfig` (ADR-011).
3. Implement left / right hand grab with `TwoBoneIKConstraint` target set to `hold.grip_point` on grab, released on hand free (ADR-009).
4. Gate IK constraint activation on FSM state: enable on `GripStable` entry, disable on exit.
5. Display grab success/failure feedback.
6. Draw `Gizmos.DrawWireSphere` from each hand anchor in debug mode.

### Verification
1. Player can chain 3 consecutive grabs on both wall segments.
2. Out-of-reach holds are cleanly rejected.
3. Hand visually lands on `grip_point` with no arm stretch artifact.

## Day 4 - Minimal FSM (State Pattern GoF)

### Tasks
1. Create abstract `ClimbingState` base class with `OnEnter(ClimbingContext)`, `OnExit()`, `HandleTrigger(Trigger, ClimbingContext)` (ADR-004).
2. Define `ClimbingContext` record: holds references to `TuningConfig`, resource systems, event bus.
3. Implement 4 concrete state classes:
   1. `IdleGroundState`
   2. `ReachState`
   3. `GripStableState`
   4. `FallingState`
4. Implement `ClimbingStateMachine`: owns current state, delegates trigger handling, logs invalid transitions with state name + trigger.
5. Add current state debug overlay (state class name + last trigger).

### Verification
1. Expected transitions reproducible (`IdleGround → Reach → GripStable → Falling → RunEnd`).
2. No inconsistent state loop over 10 attempts.
3. Invalid transition attempts appear in console with reason.

## Day 5 - Integration and Mini-Playtest

### Tasks
1. Stabilize the prototype scene.
2. Adjust initial values (grab range, movement speed, camera).
3. Run internal mini-playtest (2-3 people).
4. Note max 5 friction points.
5. Prioritize fixes for Sprint 2.0.
6. Record the playtest baseline and results in `Implementation/sprints/playtest/sprint_1.md`.

### Verification
1. 80% of testers understand basic controls in under 2 minutes.
2. No blocking bug on a 10-minute session.

## Overflow Backlog (if time remains)
1. Start `EnduranceSystem` (simple gauge).
2. Contextual prompt "unstable hold".
3. Local save of keyboard bindings.

## Sprint 1.0 Risks
1. Camera hard to tune across two wall segments with different angles (ADR-012).
2. Ambiguous input mapping between keyboard and gamepad.
3. IK rig setup blocks Day 3 hand interaction (ADR-009).
4. Per-hand detection requires rig hand anchors available from Day 3 (ADR-011).

## Quick Mitigation
1. Prioritize camera readability on the flat segment first; add Cinemachine blend trigger for overhang on Day 2.
2. Keep FSM at 4 states this week; remaining 5 states added in Sprints 2–3.
3. If rig setup blocks Day 3: fall back to position snap for Sprint 1.0 only — document in sprint retrospective and add IK on Day 4.
4. Expose `LeftHandAnchor` / `RightHandAnchor` as serialized transforms with a hips-offset fallback if rig is absent.

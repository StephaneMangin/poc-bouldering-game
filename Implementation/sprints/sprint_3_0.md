# Sprint 3.0 Checklist (5 days)

## Sprint Objective
Integrate resource systems (endurance, grip, local fatigue) driven by the physics model.

## Definition of Done
1. Endurance decreases/regens according to actions + COG offset + wall angle.
2. Grip decreases according to hold texture + weight on hand + wall angle.
3. Local fatigue per arm calculated with physics-driven weight inputs.
4. Chalk action functional with 20 s cooldown.
5. Resource HUD displays endurance, grip, and arm fatigue gauges.

## Day 1
1. Create `EnduranceSystem` (values from `variables_gameplay.md`).
2. Connect costs to `grab`, `reposition`, `dyno`.
3. Apply `cog_offset_penalty` and `wall_angle_endurance_mult` from PhysicsModelSystem.

## Day 2
1. Create `GripSystem`.
2. Add `grip_texture_hold` and `grip_texture_wall` to `HoldComponent`.
3. Apply `weight_on_hand` and `wall_angle_grip_drain_mult` to grip drain.
4. Implement micro-slip on low grip.

## Day 3
1. Implement `LocalFatigueSystem` (left/right).
2. Connect gains by hold type + `weight_on_arm` + `wall_angle_fatigue_mult`.
3. Add `chalk` action + cooldown.

## Day 4
1. Activate fatigue thresholds (60, 80, 95).
2. Resource HUD (endurance gauge, grip gauge, arm fatigue indicators).
3. Basic contextual prompts (critical grip, chalk recommended, alternate arms).

## Day 5
1. Initial tuning with physics model active.
2. Internal mini-playtest: verify resources respond to body position.
3. List max 5 frictions for Sprint 4.0.

## Validation Criteria
1. Players clearly perceive the effect of chalk.
2. Climbing with poor posture (high COG offset) drains faster.
3. Arm fatigue difference is perceptible when arms alternate vs. not.
4. No infinite regen/loss inconsistency loop.
5. Stable build on 10 min of continuous climbing.

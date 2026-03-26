# Sprint 2.0 Checklist (5 days)

## Sprint Objective
Build the physics model foundation: COG, weight distribution, wall angle physics, PhysicMaterials.

## Definition of Done
1. COG computed from contact points in real-time.
2. Weight distribution updates per-limb based on COG position.
3. Wall angle detected and scaling multipliers applied.
4. PhysicMaterials assigned to wall, holds, and crash pad.
5. Body-wall contact detection functional on slab.
6. Debug overlay visualizes COG position + stability polygon.

## Day 1
1. Implement `PhysicsModelSystem` (COG computation).
2. Define support polygon from active contact points.
3. Compute `cog_offset` (distance from COG to wall plane).

## Day 2
1. Implement weight distribution (hand/foot ratios based on COG).
2. Add wall angle detection per contact point.
3. Apply `wall_angle_endurance_mult` / `wall_angle_fatigue_mult` / `wall_angle_grip_drain_mult`.

## Day 3
1. Create PhysicMaterials: `PM_Wall_Rough`, `PM_Wall_Smooth`, `PM_Hold_Rubber`, `PM_Hold_Wet`, `PM_CrashPad`, `PM_Shoe_Rubber`.
2. Assign PhysicMaterials to existing hold prefabs and wall panels.
3. Implement basic body-wall contact (slab friction assist).

## Day 4
1. Debug visualization: COG marker (sphere gizmo), stability polygon wireframe.
2. Connect `cog_offset_penalty` to endurance cost (placeholder multiplier).
3. Test on existing VerticalPrototype wall: verify COG shifts with hand/foot placement.

## Day 5
1. Tune initial values from `variables_gameplay.md`.
2. Internal mini-playtest: verify COG feedback is perceptible.
3. List max 5 frictions for Sprint 3.0.

## Validation Criteria
1. COG moves visibly when repositioning hands/feet.
2. Climbing on overhang section feels harder than vertical.
3. PhysicMaterials produce distinguishable friction behavior.
4. Stable build on 10 min of continuous climbing.

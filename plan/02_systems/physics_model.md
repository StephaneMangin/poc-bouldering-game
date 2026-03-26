# Physics Model

## Objective
Simulate realistic climbing physics where the player's body interacts with the wall through gravity, friction, and weight distribution — without requiring a fully rigid-body skeleton.

## Design Approach
Hybrid kinematic/physics: the player remains kinematic during controlled climbing but physics forces (COG, friction, tension) are calculated continuously to drive gameplay outcomes (slips, swings, falls).

## Centre of Gravity (COG)

### Principle
The COG is a virtual point computed from the positions and weights of all contact points (hands, feet) and the player's body mass. It determines stability, endurance drain, and fall direction.

### Computation
1. Each limb anchor contributes a weighted position.
2. Hands contribute 40% of support when feet are placed; 100% when feet are free.
3. Feet contribute 60% of support when placed on valid holds.
4. COG position = weighted average of all contact point positions.
5. `cog_offset` = horizontal distance from COG to wall plane.

### Stability Rules
1. If COG falls within the support polygon (convex hull of contact points), posture is stable.
2. If COG is outside the polygon, the player enters `Unstable` zone — endurance drain increases.
3. If COG offset exceeds `cog_max_offset`, the player starts a barn-door swing or falls.

## Weight Distribution

### Principle
Weight is distributed dynamically between contact points based on COG position and limb angles.

### Rules
1. Feet absorb more weight when COG is low and close to the wall.
2. Hands absorb more weight when COG is high or far from the wall (overhangs).
3. Weight on a limb modifies its fatigue rate and grip drain.
4. `weight_ratio_hands` = 0.0 (ideal) to 1.0 (full hang). Target: 0.3-0.4 on vertical.

### Impact on Systems
1. Grip drain = `base_drain × weight_on_hand × grip_texture_coeff`.
2. Local fatigue gain = `base_gain × weight_on_arm × posture_coeff`.
3. Endurance cost = `base_cost × (1 + cog_offset_penalty)`.

## Wall Angle Physics

### Principle
Wall angle directly affects the force required to stay on the wall. Slab (< 90°) is easiest; roof (180°) is hardest.

### Angle Categories
1. `Slab` (70°-85°): gravity assists — low endurance cost, feet carry 70-80%.
2. `Vertical` (85°-95°): balanced — reference costs.
3. `Slight overhang` (95°-115°): arms work harder — hands carry 50-60%.
4. `Strong overhang` (115°-145°): demanding — hands carry 60-80%, high fatigue.
5. `Roof` (145°-180°): extreme — hands carry 80-100%, rapid fatigue.

### Scaling
1. `wall_angle_endurance_mult`: 0.6 (slab) → 1.0 (vertical) → 2.5 (roof).
2. `wall_angle_fatigue_mult`: 0.5 (slab) → 1.0 (vertical) → 3.0 (roof).
3. `wall_angle_grip_drain_mult`: 0.7 (slab) → 1.0 (vertical) → 2.0 (roof).

## Pendular Physics (Barn Door / Swing)

### Principle
When the player releases a hold on one side or misses a dynamic move, the body swings like a pendulum around the remaining contact point(s).

### Rules
1. Swing magnitude depends on the distance between COG and the pivot hold.
2. During a swing, the player has a recovery window (`swing_recovery_window_s = 0.8`) to grab a hold.
3. Swing velocity decays with wall friction (if body contacts wall).
4. Failed recovery → `Falling` state.

## Dyno Ballistics

### Principle
Dynamic moves (dynos) launch the player along a ballistic arc. Success depends on timing, direction, and residual grip.

### Rules
1. Launch vector = normalized direction from push foot to target hold.
2. Force = `dyno_base_force × (1 - arm_fatigue_pct) × (1 - cog_offset_penalty)`.
3. Gravity applies during flight phase.
4. Catch window = `dyno_catch_window_s = 0.3` seconds at apex.
5. Failed catch → pendular swing or fall.

## Landing / Crash Pad

### Principle
Falls end on crash pads. Landing quality affects score and provides audiovisual feedback.

### Rules
1. Landing zone = crash pad area below the climbing wall.
2. Fall height determines impact intensity (audio volume, camera shake, score penalty).
3. Fall from > `fall_height_heavy_threshold = 3.0` m → heavy landing animation + larger penalty.
4. Fall from < 1.5 m → light landing, minimal penalty.

## Body-Wall Contact

### Principle
The climber's body (hips, knees, chest) can contact the wall surface. This provides friction-based support on slabs and limits movement on overhangs.

### Rules
1. On slab: body contact assists stability — `slab_body_friction = 0.4`.
2. On vertical: minimal body contact — neutral.
3. On overhang: body must stay clear — contact with wall adds drag but no support.

## PhysicMaterials

### Required Materials
1. `PM_Wall_Rough`: static friction 0.8, dynamic friction 0.6 — bare wall surface.
2. `PM_Wall_Smooth`: static friction 0.5, dynamic friction 0.3 — polished wall.
3. `PM_Hold_Rubber`: static friction 0.9, dynamic friction 0.7 — rubber hold surface.
4. `PM_Hold_Wet`: static friction 0.3, dynamic friction 0.2 — wet/greasy hold.
5. `PM_CrashPad`: static friction 0.9, dynamic friction 0.8, bounciness 0.15 — landing pad.
6. `PM_Shoe_Rubber`: static friction 0.95, dynamic friction 0.8 — climbing shoe sole.

## Tuning Reference
Source of truth for numerical values: [Gameplay Variables](../04_technical/variables_gameplay.md).
In case of discrepancy, the value in `variables_gameplay.md` prevails.

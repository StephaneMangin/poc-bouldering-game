# Endurance System

## Objective
Model the climber's overall effort to encourage technique, rhythm, and rest management.

## Main Variables
1. `endurance_max`: 100
2. `endurance_current`: [0..100]
3. `state_effort`: `resting`, `controlled`, `intense`
4. `regen_delay`: 0.8 s after a costly action

## Base Costs (medium difficulty)
1. Stable grab: `-2`
2. Lateral reposition: `-4`
3. Difficult hold change: `-6`
4. Controlled leg push: `-7`
5. Dynamic move (dyno): `-12`
6. Emergency correction after slip: `-9`

## Recovery
1. Active rest (good foothold + stable hold): `+6 / s`
2. Partial rest (average hold): `+3 / s`
3. Hanging by arms only: `0 / s`
4. Intense movement: `-1 / s` additional while state lasts

## Update Formula (tick)
`endurance_current = clamp(0, endurance_max, endurance_current - action_cost + regen * delta_time)`

## Behavior Thresholds
1. `> 60`: normal state.
2. `30 to 60`: perceptible fatigue, slightly reduced precision.
3. `< 30`: critical zone, increased error risk.
4. `0`: short exhaustion state (limited inputs for 1.2 s).

## Gameplay Effects by Threshold
1. `<= 60`: grab speed `-5%`.
2. `<= 30`: input precision `-12%`, reaction time `+10%`.
3. `== 0`: dyno is impossible, priority on safe repositioning.

## Mode Modifiers
1. Arcade:
1. Endurance recovery `+15%`.
2. Action costs `-10%`.
2. Simulation:
1. Endurance recovery `-10%`.
2. Action costs `+8%`.

## Difficulty Modifiers
1. Easy: global cost `-10%`, regen `+10%`.
2. Medium: reference.
3. Hard: global cost `+10%`, regen `-8%`.

## Interactions with Other Systems
1. High local arm fatigue (`>80`) applies a `+10%` endurance malus on each hand action.
2. Low grip (`<30`) increases the cost of emergency corrections.
3. Focus temporarily suspends the precision malus but not the endurance cost.
4. COG offset penalty increases all costs when body is far from wall (see [Physics Model](physics_model.md)).
5. Wall angle multiplier: overhangs and roofs amplify endurance drain (see [Physics Model](physics_model.md)).
6. Good footwork and weight distribution reduce effective drain by up to 40% (see [Body Mechanics](body_mechanics.md)).
7. Endurance threshold `<30` triggers intense breathing audio (see [Audio](audio.md)).

## Physics Integration
Endurance costs are modulated by the physics model:
1. `effective_cost = base_cost × (1 + cog_offset_penalty) × wall_angle_endurance_mult`.
2. Proper foot placement (high `weight_ratio_feet`) reduces effective cost.
3. Heel hook / knee bar rest positions enable recovery rates comparable to ground rest.

## UI and Feedback
1. Clear and stable endurance gauge (no aggressive animations).
2. Progressive color change:
1. Green `>60`
2. Orange `30-60`
3. Red `<30`
3. Subtle visual pulse at `<=15` to signal urgency.

## Balancing Rules
1. A normal route must be completable without hitting 0 if the player uses 2 to 3 effective rest stops.
2. An aggressive style must remain viable but riskier.
3. Runs must not turn into passive regen waiting.

## Target Telemetry
1. Time spent below 30 endurance per run.
2. Average number of consecutive costly actions.
3. Number of falls preceded by endurance <= 20.
4. Total active rest duration per run.

## Validation Criteria
1. 70% of players intuitively identify useful rest zones.
2. Less than 20% of MVP runs fail due to "unexplained" exhaustion.
3. In Arcade mode, completion rate increases by at least 15% vs Simulation on the same route.
4. Median run duration stays between 5 and 15 minutes after calibration.

## Tuning Reference
Source of truth for numerical values: [Gameplay Variables](../04_technical/variables_gameplay.md).
In case of discrepancy, the value in `variables_gameplay.md` prevails.

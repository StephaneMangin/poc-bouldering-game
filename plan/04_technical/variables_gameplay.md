# Gameplay Variables (tuning source of truth)

## Objective
Centralize all gameplay values to speed up balancing and prevent regressions.

## Convention
1. Naming: `snake_case`.
2. Units explicit in the name when necessary (`_per_s`, `_seconds`).
3. Each variable: default value + tested range.

## Endurance
1. `endurance_max = 100` (80-130)
2. `endurance_regen_rest_per_s = 6` (3-9)
3. `endurance_cost_grab = 2` (1-4)
4. `endurance_cost_reposition = 4` (2-6)
5. `endurance_cost_dyno = 12` (8-16)

## Grip
1. `adherence_max = 100` (80-120)
2. `adherence_loss_base_per_s = 1.5` (1.0-2.5)
3. `adherence_loss_burst = 5` (3-8)
4. `chalk_restore_value = 35` (20-45)
5. `chalk_cooldown_seconds = 20` (15-30)

## Local Fatigue
1. `fatigue_gain_jug_per_s = 4` (2-6)
2. `fatigue_gain_crimp_per_s = 7` (5-10)
3. `fatigue_gain_sloper_per_s = 11` (8-14)
4. `fatigue_recover_rest_per_s = 10` (6-12)
5. `fatigue_critical_threshold = 80` (70-90)

## Score
1. `score_base_easy = 1000`
2. `score_base_medium = 1600`
3. `score_base_hard = 2400`
4. `score_fall_penalty_arcade = 150`
5. `score_no_fall_bonus_simulation = 400`

## Mode Multipliers
1. `arcade_endurance_regen_mult = 1.15`
2. `simulation_endurance_regen_mult = 0.90`
3. `arcade_fatigue_gain_mult = 0.90`
4. `simulation_fatigue_gain_mult = 1.12`

## Difficulty Multipliers
1. `easy_global_tolerance_mult = 1.10`
2. `medium_global_tolerance_mult = 1.00`
3. `hard_global_tolerance_mult = 0.90`

## Rebinding and UX
1. `input_rebind_conflict_block = true`
2. `input_rebind_apply_without_restart = true`
3. `ui_prompt_min_interval_seconds = 2.5`

## Physics Model (COG & Weight Distribution)
1. `cog_max_offset = 0.6` (0.4-0.8) — max horizontal distance from COG to wall before fall.
2. `cog_offset_penalty_start = 0.2` (0.1-0.3) — offset where endurance penalty begins.
3. `cog_offset_penalty_mult = 2.0` (1.5-3.0) — endurance cost multiplier at max offset.
4. `weight_ratio_hands_vertical = 0.35` (0.25-0.45) — reference hand weight on vertical.
5. `weight_ratio_hands_overhang_strong = 0.70` (0.60-0.85) — hand weight on strong overhang.
6. `weight_ratio_hands_roof = 0.90` (0.80-1.00) — hand weight on roof.

## Wall Angle Multipliers
1. `wall_angle_endurance_mult_slab = 0.6` (0.4-0.8)
2. `wall_angle_endurance_mult_vertical = 1.0`
3. `wall_angle_endurance_mult_overhang = 1.6` (1.3-2.0)
4. `wall_angle_endurance_mult_roof = 2.5` (2.0-3.0)
5. `wall_angle_fatigue_mult_slab = 0.5` (0.3-0.7)
6. `wall_angle_fatigue_mult_vertical = 1.0`
7. `wall_angle_fatigue_mult_overhang = 1.8` (1.4-2.2)
8. `wall_angle_fatigue_mult_roof = 3.0` (2.5-3.5)
9. `wall_angle_grip_drain_mult_slab = 0.7` (0.5-0.9)
10. `wall_angle_grip_drain_mult_vertical = 1.0`
11. `wall_angle_grip_drain_mult_overhang = 1.4` (1.2-1.6)
12. `wall_angle_grip_drain_mult_roof = 2.0` (1.7-2.5)

## Pendular & Dyno Physics
1. `swing_recovery_window_s = 0.8` (0.5-1.2) — time to catch a hold during barn-door swing.
2. `dyno_base_force = 8.0` (6.0-10.0) — launch force for dynamic moves.
3. `dyno_catch_window_s = 0.3` (0.2-0.5) — catch window at dyno apex.
4. `fall_height_heavy_threshold = 3.0` (2.5-4.0) — fall height for heavy landing.
5. `slab_body_friction = 0.4` (0.2-0.6) — body-wall friction on slab.

## Body Mechanics
1. `grip_type_fatigue_mult_jug = 0.6` (0.4-0.8)
2. `grip_type_fatigue_mult_open = 0.8` (0.6-1.0)
3. `grip_type_fatigue_mult_pinch = 1.0`
4. `grip_type_fatigue_mult_crimp = 1.3` (1.1-1.5)
5. `grip_type_fatigue_mult_gaston = 1.4` (1.2-1.6)
6. `grip_type_fatigue_mult_undercling = 1.5` (1.3-1.7)
7. `heel_hook_weight_transfer = 0.20` (0.15-0.25) — weight shifted off hands.
8. `toe_hook_weight_transfer = 0.12` (0.08-0.18)
9. `knee_bar_weight_transfer = 0.90` (0.80-1.00) — nearly full rest.
10. `drop_knee_reach_bonus = 0.15` (0.10-0.20) — meters of extra reach.
11. `twist_lock_reach_bonus = 0.12` (0.08-0.16)
12. `overextend_fatigue_mult = 1.5` (1.3-1.8) — penalty for reaching > 90% max.
13. `arm_length = 0.65` (0.55-0.75) — base reach from shoulder.
14. `max_hip_rotation_deg = 60` (45-75)
15. `max_torso_twist_deg = 45` (30-60)
16. `body_tension_base = 0.5` (0.3-0.7) — base L1 tension, scales with level.

## Audio
1. `audio_breathing_switch_threshold = 30` — endurance below which breathing becomes labored.
2. `audio_heartbeat_fatigue_threshold = 80` — arm fatigue above which heartbeat plays.
3. `audio_fall_doppler_min_velocity = 2.0` — minimum velocity for pitch shift.
4. `audio_music_duck_endurance = 40` — endurance below which music ducks.

## Tuning Versioning
1. Add `tuning_version` to saves.
2. Log `tuning_version` for each run in telemetry.
3. Any variable modification must be tracked in the internal changelog.

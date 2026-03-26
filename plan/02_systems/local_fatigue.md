# Local Fatigue System

## Objective
Simulate forearm fatigue to reinforce route reading, body positioning, and effort/rest alternation.

## Main Variables
1. `fatigue_left`: [0..100]
2. `fatigue_right`: [0..100]
3. `fatigue_zone`: `fresh`, `loaded`, `critical`
4. `load_state`: `resting`, `light_load`, `heavy_load`

## Base Accumulation (per arm)
1. Good hold (jug): `+4 / s`
2. Average hold (crimp): `+7 / s`
3. Bad hold (sloper): `+11 / s`
4. Explosive move (dyno): `+8` instant on both arms
5. Correction after slip: `+6` on the active arm

## Posture Coefficients
1. Balanced posture (engaged feet, hips close): `x0.80`
2. Neutral posture: `x1.00`
3. Extended posture (straight arms, few footholds): `x1.20`
4. Near-total suspension on one arm: `x1.35`

## Grip Texture Coefficients (grip consistency)
1. Use `grip_texture_hold` from the active hold.
2. Use `grip_texture_wall` from the wall zone.
3. `fatigue_grip_factor = (grip_texture_hold * grip_texture_wall)`

Interpretation:
1. The more slippery the hold/wall, the more the holding effort increases.
2. A rough texture limits fatigue accumulation.

## Recovery (per arm)
1. Active rest on good hold + foothold: `-10 / s`
2. Partial rest (average hold): `-6 / s`
3. Unloaded arm during alternation: `-4 / s`
4. Minimal recovery during continuous suspension: `0 / s`

## Update Formula (tick)
`fatigue_arm = clamp(0, 100, fatigue_arm + fatigue_gain - fatigue_recovery)`

Recommended detail:
`fatigue_gain = base_gain * posture_factor * fatigue_grip_factor * mode_factor * difficulty_factor`

## Thresholds and Effects
1. `0 to 59` (`fresh`): no malus.
2. `60 to 79` (`loaded`): grab precision `-15%` on the affected arm.
3. `80 to 100` (`critical`):
1. Slip risk `+20%` on that arm.
2. Endurance cost of that arm's actions `+10%`.
3. Hold lock time `+12%`.

## Overload State
If `fatigue_left >= 95` or `fatigue_right >= 95` for more than 1.5 s:
1. Trigger `overload warning` (audio + visual).
2. Block dynos for 1.0 s.
3. Prioritize stabilization actions.

## Mode Modifiers
1. Arcade:
1. Accumulation `-10%`.
2. Recovery `+10%`.
2. Simulation:
1. Accumulation `+12%`.
2. Recovery `-8%`.

## Difficulty Modifiers
1. Easy: accumulation `-12%`, effective critical threshold at 85.
2. Medium: reference.
3. Hard: accumulation `+10%`, effective critical threshold at 75.

## Interactions with Other Systems
1. Critical fatigue + low grip strongly increases micro-slip frequency.
2. Critical fatigue increases endurance cost on hand actions.
3. Focus temporarily reduces the precision malus, without stopping fatigue accumulation.
4. Weight distribution modulates fatigue gain: more weight on hands = faster fatigue (see [Physics Model](physics_model.md)).
5. Wall angle amplifies fatigue on overhangs and roofs (see [Physics Model](physics_model.md)).
6. Grip type multiplier: crimp/gaston/undercling drain faster than jug/open hand (see [Body Mechanics](body_mechanics.md)).
7. Foot techniques (heel hook, knee bar) can transfer load off arms, reducing fatigue gain (see [Body Mechanics](body_mechanics.md)).

## Physics Integration
Fatigue gains are modulated by the physics model:
1. `effective_fatigue_gain = base_gain × grip_type_fatigue_mult × weight_on_arm × wall_angle_fatigue_mult × posture_coeff`.
2. Good foot placement and body positioning drastically reduce arm fatigue.
3. Knee bar provides hands-free rest (fatigue gain → 0 on both arms).

## UI and Feedback
1. Two arm indicators (`left`, `right`) subtle but permanent.
2. Progressive color change by zone (`fresh`, `loaded`, `critical`).
3. Subtle visual tremor on the affected hand in critical zone.
4. Contextual message: "alternate arms" after 3 s in overload.

## Contextual UI/UX Feedback (multi-parameter)

### Design Rule
Feedback must always answer a player question:
1. What is happening right now?
2. Why?
3. What should I do next?

### Local Muscle Fatigue (arm)
1. `Loaded` zone (60-79):
1. Slight reddening of the affected forearm.
2. More audible breathing.
3. Arm gauge turns orange.
2. `Critical` zone (80+):
1. Pronounced reddening + stylized visible veins.
2. Visible tremor on the active hand.
3. Subtle heartbeat in background audio.
4. Short message: "rest or alternate the arm".
3. Overload (`>=95` sustained):
1. Brief red flash on the arm indicator.
2. Muffled alert sound (not aggressive).
3. Temporary "dyno blocked" icon.

### Posture and Load Distribution
1. Balanced posture:
1. Body silhouette displayed in blue/green.
2. Smooth animation, no jitter.
2. Extended or unbalanced posture:
1. Silhouette shifts to amber/red on the overloaded side.
2. Directional indicator shows where to reposition hips.
3. More present muscle tension sound.

### Positive Progression Feedback
1. "Clean combo" after 3 clean moves:
1. Mini-banner: "clean sequence".
2. Brief rewarding SFX (subtle).
2. Effective rest recovery:
1. Gauge refills with a stabilization effect.
2. Message: "good foothold, keep it up".

### Feedback Accessibility
1. Every critical visual signal must have an audio or text equivalent.
2. Color-blind option: alternative palette for `loaded/critical` states.
3. Effect intensity slider (tremor, vignette, pulse).
4. All contextual prompts can be reduced after learning.

## Balancing Rules
1. Alternating arms must be clearly rewarded.
2. Good posture must reduce fatigue perceptibly.
3. Fatigue failures must be attributable to readable choices.

## Target Telemetry
1. Average time spent in `critical` zone per arm.
2. Number of slips with fatigue > 80.
3. Left/right distribution (detect arm usage bias).
4. Rate of dynos cancelled due to overload.

## Validation Criteria
1. 70% of players understand the benefit of alternating arms after 2 runs.
2. Less than 20% of fatigue failures are judged "unfair" in playtest.
3. Players using rest zones reduce their time in the critical zone by at least 25%.
4. In Arcade mode, severe overload occurs at least 15% less often than in Simulation.

## Tuning Reference
Source of truth for numerical values: [Gameplay Variables](../04_technical/variables_gameplay.md).
In case of discrepancy, the value in `variables_gameplay.md` prevails.

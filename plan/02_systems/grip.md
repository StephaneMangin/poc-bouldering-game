# Grip System

## Objective
Represent hand/hold contact quality to add a readable tactical layer: rhythm, chalk management, and environment adaptation.

## Main Variables
1. `adherence_max`: 100
2. `adherence_current`: [0..100]
3. `chalk_cooldown`: 20 s
4. `surface_state`: `dry`, `wet`, `dusty`
5. `grip_texture_hold`: grip coefficient of the hold (material/texture)
6. `grip_texture_wall`: grip coefficient of the local wall section (secondary influence)
7. `grip_texture_total`: final coefficient applied to grip loss

## Grip Coefficients (hold + wall)
`grip_texture_total = grip_texture_hold * grip_texture_wall`

Recommended values:
1. `rough` hold: `grip_texture_hold = 0.85`
2. `standard` hold: `grip_texture_hold = 1.00`
3. `smooth` hold: `grip_texture_hold = 1.15`
4. `slippery` hold: `grip_texture_hold = 1.30`
5. `rough` wall: `grip_texture_wall = 0.95`
6. `standard` wall: `grip_texture_wall = 1.00`
7. `polished/wet` wall: `grip_texture_wall = 1.10`

Interpretation:
1. Coefficient < 1.0: grip loss slowed down.
2. Coefficient > 1.0: grip loss accelerated.

## Grip Loss (base medium difficulty)
1. Passive loss while climbing: `-1.5 / s`
2. Abrupt move: `-5` instant
3. Emergency correction after slip: `-6`
4. Extended contact on smooth hold (sloper): `-2 / s` additional

## Environmental Modifiers
1. Dry surface: multiplier `x1.0`
2. Wet surface: multiplier `x1.8`
3. Dusty surface: multiplier `x1.4`
4. Strong wind zone (optional): aim precision malus `+8%` if grip < 40

## Recovery / Restoration
1. Chalk (`active action`): `+35` grip instant
2. Chalk cooldown: 20 s
3. Rest bonus in stable zone: `+1 / s` (capped at 50 without chalk)

## Update Formula (tick)
`adherence_current = clamp(0, adherence_max, adherence_current - total_loss * delta_time + total_gains)`

Recommended calculation detail:
`total_loss = base_loss * env_modifier * grip_texture_total * mode_modifier * difficulty_modifier`

## Behavior Thresholds
1. `> 60`: reliable contact, no malus.
2. `30 to 60`: degraded contact, micro-risks.
3. `< 30`: critical zone, high slip risk.
4. `< 15`: danger zone, abrupt actions strongly penalized.

## Gameplay Effects by Threshold
1. `<= 60`: micro-slip chance `+8%` on average holds.
2. `<= 30`: slip chance `+25%` on bad/smooth holds.
3. `<= 15`: unable to maintain a fragile hold for long (>1.2 s).

## Grip Type Differentiation
Different grip types modify the effective grip drain and max hold duration.
See [Body Mechanics](body_mechanics.md) for full grip type definitions.

1. Jug: grip drain ×0.7 — easy to hold, slow loss.
2. Open Hand: grip drain ×0.9 — moderate, good for slopers.
3. Pinch: grip drain ×1.0 — reference.
4. Crimp: grip drain ×1.2 — fast drain but high load capacity.
5. Gaston: grip drain ×1.3 — high lateral stress.
6. Undercling: grip drain ×1.3 — high vertical stress.

### Hold Orientation Impact
1. Hold angle relative to pull direction affects grip efficiency.
2. Ideal angle (within 30° of pull vector): no penalty.
3. Off-angle (30°-60°): grip drain ×1.2.
4. Bad angle (>60°): grip drain ×1.5, micro-slip chance +15%.

## Physics Integration
Grip drain is modulated by the physics model (see [Physics Model](physics_model.md)):
1. `effective_grip_drain = base_drain × grip_type_coeff × weight_on_hand × wall_angle_grip_mult × env_modifier`.
2. Weight distribution directly affects grip: more weight on hands = faster drain.
3. Wall angle amplifies drain on overhangs and roofs.

## Interactions with Other Systems
1. Low endurance (`<30`) + low grip (`<30`) triggers cumulative precision malus.
2. High local arm fatigue increases the micro-slip penalty.
3. Focus temporarily reduces the precision effect, but not the raw grip loss.
4. Weight distribution (see [Physics Model](physics_model.md)) modulates effective grip drain.
5. Grip type (see [Body Mechanics](body_mechanics.md)) modulates fatigue and drain rates.

## Mode Modifiers
1. Arcade:
1. Passive loss `-15%`.
2. Chalk `+10` effectiveness.
2. Simulation:
1. Passive loss `+10%`.
2. Standard chalk (no bonus).

## Difficulty Modifiers
1. Easy: global loss `-10%`.
2. Medium: reference.
3. Hard: global loss `+12%`, effective critical threshold at 35 instead of 30.

## UI and Feedback
1. Grip gauge separate from endurance.
2. Subtle visual indicator on hands when grip < 30.
3. Contextual notification "Chalk recommended" if grip < 25 and cooldown finished.
4. Specific SFX on micro-slip to inform without startling.

## Contextual UI/UX Feedback (grip)

### Hold Quality at the Moment of Limb Placement
1. `Good` hold:
1. Short green halo around the hold.
2. Dry, stable sound (clean catch).
3. Hand/foot reticle stays thin and fixed.
2. `Average` hold:
1. Subtle amber halo.
2. Rougher sound.
3. Reticle pulses slightly to signal "hold, but be careful".
3. `Bad/Smooth` hold:
1. Orange-red halo with micro dust particles.
2. Slippery friction SFX.
3. Reticle distorts slightly (ellipse) to signal instability.
4. `Slippery` hold (high grip texture):
1. Accentuated specular highlight on the hold.
2. Micro "droplet" or "slick" icon near the hand.
3. Subtle prompt "chalk recommended" if cooldown finished.

### Overall Grip
1. Grip `30-60`:
1. Chalk traces fade faster on the hands.
2. More unstable friction SFX.
2. Grip `<30`:
1. Visual "micro-slip" effect (slight palm detach).
2. UI suggests `chalk` action via button animation.
3. Grip `<15`:
1. Very subtle screen vignette to signal urgency.
2. Short red border around the grip gauge.

### Grip Feedback Accessibility
1. Every critical visual signal must have an audio or text equivalent.
2. Color-blind option to clearly distinguish grip states.
3. Effect intensity slider (pulse, vignette, micro-slip flash).

## Balancing Rules
1. Chalk must be useful, not mandatory every 15 seconds.
2. Good route reading must allow anticipating slippery segments.
3. Falls caused by grip must remain explainable through feedback.

## Target Telemetry
1. Average chalk usages per run.
2. Fall rate with grip < 30.
3. Cumulative time in critical grip zone.
4. Micro-slip rate per hold type.
5. Fall rate per `grip_texture_hold` category (`rough`, `smooth`, `slippery`).

## Validation Criteria
1. 75% of players understand the utility of chalk in under 2 runs.
2. Less than 15% of grip-related falls are perceived as arbitrary in playtest.
3. Arcade mode significantly reduces grip-related falls (>= 20% vs Simulation).
4. Chalk usage remains a tactical choice and not a systematic spam.
5. 80% of players correctly identify hold quality via feedback without reading explanatory text.

## Tuning Reference
Source of truth for numerical values: [Gameplay Variables](../04_technical/variables_gameplay.md).
In case of discrepancy, the value in `variables_gameplay.md` prevails.

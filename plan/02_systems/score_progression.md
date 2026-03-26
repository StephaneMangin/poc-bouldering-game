# Score and Progression System

## Objective
Reward technical mastery, encourage replayability, and offer readable progression without excessive grind.

## Score Formula (run)
`score_total = route_base + time_bonus + fluidity_bonus + technique_bonus + risk_bonus - penalties`

## Components

### Route Base
1. Easy: `1000`
2. Medium: `1600`
3. Hard: `2400`

### Time Bonus
`time_bonus = max(0, 600 - time_in_seconds) * 2`

### Fluidity Bonus (`0..400`)
1. Few long pauses.
2. Few emergency corrections.
3. Good arm alternation.

### Technique Bonus (`0..300`)
1. `+50` per valid advanced move (`heel hook`, `flag`, `controlled dyno`).
2. Cap: `+300`.

### Risk Bonus (`0..200`)
1. Hard sections completed without falling.
2. Maintaining precision with low resources.

### Penalties
1. Fall in Arcade: `-150` per fall.
2. Broken fragile hold: `-80`.
3. Focus abuse (> 4 uses): `-100`.
4. Run abandon: score valid but time bonus cancelled.

## Multipliers
1. Simulation mode with no fall: `x1.10`.
2. Clean streak (no emergency correction for 30 s): `x1.05` temporary.
3. No multipliers stackable beyond `x1.20`.

## Player Progression

### Run XP
`xp_run = 100 + (score_total / 40)`

### Levels (1 to 10)
1. L1: tutorial + full assisted mode.
2. L2: `+10 endurance_max`.
3. L3: dyno cost reduced by `-2`.
4. L4: chalk cooldown `-3 s`.
5. L5: unlocks `Heel Hook`.
6. L6: fatigue recovery `+2 / s` during active rest.
7. L7: unlocks `Flag`.
8. L8: fluidity bonus `+10%`.
9. L9: unlocks `Controlled Dyno`.
10. L10: expert route + cosmetic reward.

### Recommended XP Curve
1. L1 -> L2: `250 XP`
2. L2 -> L3: `350 XP`
3. L3 -> L4: `500 XP`
4. L4 -> L5: `700 XP`
5. L5 -> L6: `900 XP`
6. L6 -> L7: `1200 XP`
7. L7 -> L8: `1500 XP`
8. L8 -> L9: `1900 XP`
9. L9 -> L10: `2400 XP`

## Non-Power Rewards
1. Glove/shoe skins.
2. Style badges (`fluid`, `precise`, `enduring`).
3. Ghost cosmetic traces.

## Anti-Exploit
1. Repeated micro-moves in place: diminishing score returns.
2. Farming on too-easy route: XP decreases after 5 identical consecutive runs.
3. Technique bonus valid only if execution is contextually useful.

## Post-Run Debrief
Display:
1. Score detailed by component.
2. 1 highlight of the attempt.
3. 1 concrete improvement axis.
4. XP progression and next unlock.

## Target Telemetry
1. Score distribution per route.
2. Average time to reach L5 and L10.
3. Utilization rate of unlocked techniques.
4. Run restart rate within 60 s after debrief.

## Validation Criteria
1. 70% of players understand why their score went up or down.
2. The first significant unlock (L5) is reachable within 60 to 120 minutes of play.
3. Players replay the same route to optimize their score, not only for XP.
4. Less than 10% of runs show obvious farming behavior.

## Tuning Reference
Source of truth for numerical values: [Gameplay Variables](../04_technical/variables_gameplay.md).
In case of discrepancy, the value in `variables_gameplay.md` prevails.

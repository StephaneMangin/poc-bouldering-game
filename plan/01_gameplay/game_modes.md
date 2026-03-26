# Game Modes

## Objective
Offer two complementary experiences:
1. `Arcade`: fast learning and immediate replayability.
2. `Simulation`: higher tension and strong consequences.

## Cross-Mode Rule (controls)
Regardless of mode, keyboard keys remain configurable from the interface (`Options > Controls`).
The game mode must never block rebinding.

## Arcade Mode

### Intent
Favor run enjoyment, fast progression, and immediate restarts.

### Main Rules
1. Fall: repositioned at the last stable checkpoint.
2. Fall penalty: `+12 s` to the timer and `-150` points.
3. Assists active by default: wider auto-aim, more lenient timing tolerance.
4. More permissive resources:
1. Faster endurance recovery.
2. Lower critical grip threshold.

### Win and Fail
1. Win: reach the top.
2. Fail: voluntary abandon only (a fall does not end the run).

### Target Audience
1. New players.
2. Score-oriented players and short session players.

## Simulation Mode

### Intent
Reinforce risk management and the satisfaction of technical mastery.

### Main Rules
1. Fall: immediate end of the attempt.
2. No-fall bonus: `+400` points.
3. Reduced assists by default: stricter auto-aim, more demanding timing.
4. Stricter resources:
1. Local fatigue penalizes bad posture faster.
2. Rest is required for lasting recovery.

### Win and Fail
1. Win: reach the top without falling.
2. Fail: fall or abandon.

### Target Audience
1. Players seeking challenge and precision.
2. Players wanting a feel closer to simulation.

## Difficulty Parameters (independent of mode)
1. `Easy`: wide auto-aim window, reduced endurance cost, enhanced feedback.
2. `Medium`: reference values.
3. `Hard`: narrow auto-aim, increased fatigue cost, reduced error margins.

Note: a player can choose `Arcade + Hard` or `Simulation + Easy`.

## Recommended Initial Balancing
1. Base values calibrated on `Medium`.
2. Mode modifiers:
1. Arcade: `+15%` endurance recovery, `-10%` local fatigue accumulation.
2. Simulation: `-10%` endurance recovery, `+12%` local fatigue accumulation.
3. Difficulty modifiers:
1. Easy: `+10%` global tolerance.
2. Hard: `-10%` global tolerance.

## Mode Selection UX
1. Selection screen with a clear comparison of consequences (`fall`, `assists`, `score`).
2. Estimated run preview (duration and risk level).
3. Visual reminder of the chosen mode during the run.

## Validation Criteria
1. 90% of players understand the `Arcade` vs `Simulation` difference after reading the mode screen.
2. 70% of new players complete at least one route in `Arcade` in under 15 minutes.
3. 50% of engaged players try `Simulation` in their first session.
4. Changing mode does not reset custom key bindings.

## Reference tuning
Source of truth for numerical values: [Gameplay Variables](../04_technical/variables_gameplay.md).
In case of discrepancy, the value in `variables_gameplay.md` prevails.

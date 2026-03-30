# Game Loop

## Core Functional Loop (invariant — must be satisfied at all times)

This is the primary gameplay sequence. It must never regress.

1. **Walk** — Player moves with WASD/ZQSD/arrows (IdleGround state)
2. **Approach wall** — Player walks toward a climbing wall
3. **Target a hold** — Mouse cursor highlights a reachable hold (yellow)
4. **Place first limb** — Click or Q/E/A/D places a hand/foot on the hold → enters Climbing mode
5. **Climb** — Alternate hand and foot placements:
   - Select a hold with mouse → press limb key (Q=LH, E=RH, A=LF, D=RF w/ click=auto)
   - Body rises/shifts as limbs are placed (pelvis follows contacts)
   - Legs push body up, arms reach for higher holds
   - If a hold is out of reach, move feet up first
6. **Top out or fall** — Reach the top, or lose all supports → fall
7. **Land** — Press Space to land → returns to IdleGround (Walking mode)
8. **Repeat** — Walk to wall and climb again

### Constraints
- A limb can only reach a hold within its physical chain length from the body
- The body position is computed from where limbs are placed (not independent)
- At least 1 hand + 1 opposite foot (cross-limb) required for stable grip
- Endurance, grip adherence, and local fatigue drain during climbing

## Objective
Deliver short, readable, and replayable runs where every attempt teaches the player something.

## Macro Loop (complete session)
1. Choose a route and a mode (`Arcade` or `Simulation`).
2. Read the wall (quick preview + key points).
3. Climb while managing endurance, grip, and local fatigue.
4. Finish or fall.
5. Read the debrief (score, errors, highlights).
6. Restart with a clear improvement goal.

## Micro Loop (run)

### 1) Pre-Run (15 to 30 s)
1. The player selects the route and difficulty.
2. The game shows:
1. Dominant hold types.
2. Likely rest zones.
3. High-risk segment.
3. The player picks a mini-goal:
1. Finish without a fall.
2. Improve the time.
3. Optimize fluidity.

### 2) Ascent (5 to 12 min)
1. The player alternates:
1. Hand placement.
2. Foot placement.
3. Weight transfer.
4. Recovery on stable positions.
2. The game reinforces readability via feedback:
1. Visual: slip, arm overload, fragile hold.
2. Audio: breathing, friction, micro rhythm break.
3. UI: simple, non-intrusive gauges.
3. Progressive tension:
1. Opening segment: route learning.
2. Middle segment: fatigue and tactical choices.
3. Final segment: execution under pressure.

### 3) Run Result (15 to 25 s)
1. `Success`: final score + performance details.
2. `Failure`:
1. Arcade: resume from last stable checkpoint with penalty.
2. Simulation: run ends with immediate analysis.

### 4) Actionable Debrief (20 to 40 s)
The debrief must always show:
1. What worked well (e.g. good rest management).
2. What was costly (e.g. 3 consecutive abrupt moves).
3. A concrete recommendation for the next attempt.

## Target Rhythm
1. Complete attempt: 5 to 15 minutes.
2. Debrief + restart: under 45 seconds.
3. Optimal session: 3 to 6 runs in 30 minutes.

## Win and Fail Conditions
1. Win: reach the anchor/top within the mode's constraints.
2. Primary failure: fall caused by a cumulation of readable bad decisions.
3. Secondary failure: voluntary abandon (fatigue or bad plan).

## Iteration Principles
1. If players don't understand why they fall, prioritize readability.
2. If runs exceed 15 minutes median, simplify the route or action costs.
3. If players don't restart quickly, improve debrief quality.

# Design Pillars

## Positioning
The game takes an "Accessible Simulation" approach: climbing technique is central, but the player retains enough assistance to stay in the fun zone.

## Pillars

### 1) Readability First
The player must quickly understand holds, risks, and the impact of their choices.

Design rules:
1. Hold types must be visually recognizable in under one second.
2. Mistakes must produce clear feedback (slip, overload, local fatigue).
3. The camera must prioritize the next useful holds.

### 2) Technique > Raw Reflex
The right movement sequence must be more rewarding than an aggressive execution.

Design rules:
1. Footwork and weight transfer reduce endurance cost.
2. Dynamic moves must be powerful but risky.
3. The score rewards fluidity and clean climbing.

### 3) Progressive Physical Tension
The player must feel the rising pressure without being punished arbitrarily.

Design rules:
1. Local fatigue increases with continuous arm effort.
2. Rest zones exist and allow real recovery.
3. A fall results from a cumulation of readable bad decisions.

### 4) Active Accessibility
The game must quickly welcome new players without erasing depth.

Design rules:
1. Short, interactive tutorial (< 3 min).
2. Adaptive assists in easy mode (wider auto-aim, reduced penalties).
3. Assists can be disabled for expert players.

### 5) Replayability in Short Sessions
Every attempt must make the player want to restart immediately.

Design rules:
1. A complete run lasts 5 to 15 minutes.
2. The player clearly sees what they can improve in the debrief.
3. Content reuses the same routes with variations (weather, hold state, constraints).

## Anti-Pillars
1. Opaque punishing simulation: forbidden.
2. Pure arcade without route reading: forbidden.
3. Complex controls in the first minute: forbidden.
4. Failure without actionable explanation: forbidden.

## Validation Metrics (vision)
1. 80% of players understand basic controls in under 2 minutes.
2. 70% of players can explain why they fell after a failure.
3. 60% of players voluntarily restart at least one attempt within 10 minutes.
4. Average feedback readability score >= 7/10 in playtest.
5. Median MVP run duration between 5 and 15 minutes.

## Decision Check (go/no-go)
Before adding a feature, verify:
1. Does it improve readability or technique?
2. Does it reinforce progressive tension without arbitrary frustration?
3. Does it preserve accessibility in the first 5 minutes?
4. Does it increase replayability in short sessions?

If the answer is no to at least 2 questions, the feature is deferred or removed.

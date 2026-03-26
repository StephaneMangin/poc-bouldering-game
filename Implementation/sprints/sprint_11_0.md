# Sprint 11.0 Checklist (5 days)

## Sprint Objective
Implement mantling (top-out), knee bar, bicycle techniques, and advanced route variants.

## Definition of Done
1. Mantling (top-out) functional when both hands on top edge.
2. Knee bar technique: hands-free rest in specific geometry.
3. Bicycle technique (toe hook + heel hook opposition) functional.
4. Back step refinement.
5. R4/R5 updated with sections requiring new techniques.

## Day 1
1. Implement mantling system: detect top-edge contact, push animation.
2. Guard conditions: endurance > 20, arm fatigue < 90.
3. Add FSM `Mantling` state transitions.

## Day 2
1. Implement knee bar: detect opposing surfaces (knee + foot).
2. Weight transfer → near-zero arm load (`knee_bar_weight_transfer = 0.90`).
3. Connect to fatigue recovery (arm fatigue drops during knee bar).

## Day 3
1. Implement bicycle (combined toe hook + heel hook in opposition).
2. High leg fatigue cost but strong overhang stability.
3. Implement back step (hip rotation + outside edge foot).

## Day 4
1. Update R4 with knee bar rest opportunity.
2. Update R5 with bicycle and mantling requirements.
3. Create 1-2 route variants using new techniques (fragile holds, alternate beta).

## Day 5
1. Internal playtest: all advanced techniques on R4/R5.
2. Tune technique fatigue costs.
3. Verify unlock progression (knee bar L7, bicycle L8).

## Validation Criteria
1. Mantling completes the route cleanly (no floating at top).
2. Knee bar provides meaningful rest mid-climb.
3. Bicycle prevents barn-door on specific overhang sections.
4. All techniques feel distinct and rewarding.

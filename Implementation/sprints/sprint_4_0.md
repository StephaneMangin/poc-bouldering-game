# Sprint 4.0 Checklist (5 days)

## Sprint Objective
Implement body mechanics: grip type differentiation, basic foot techniques, and flag/torso rotation.

## Definition of Done
1. Hold affordances define `allowedGripTypes[]` per hold.
2. Grip type selection affects fatigue and grip drain rates.
3. Edging and smearing foot techniques functional.
4. Flag (balance leg extension) functional.
5. Basic torso rotation (±16° → ±45°) controlled by input.

## Day 1
1. Implement `BodyMechanicsSystem`.
2. Extend `HoldComponent` with `allowedGripTypes[]` property.
3. Implement grip type auto-selection (best compatible = lowest fatigue mult).

## Day 2
1. Connect grip type fatigue multipliers to `LocalFatigueSystem`.
2. Connect grip type drain multipliers to `GripSystem`.
3. Update 4 hold prefabs with appropriate `allowedGripTypes`.

## Day 3
1. Implement edging foot technique (standard precise placement).
2. Implement smearing (wall surface support, no hold needed, slab only).
3. Connect foot techniques to weight distribution model.

## Day 4
1. Implement flag (leg extension for COG balance).
2. Expand torso rotation range with player input.
3. Connect rotation to reach model (twist lock bonus for lateral reach).

## Day 5
1. Tune grip type multipliers on test wall.
2. Internal playtest: verify technique choice impacts performance.
3. List max 5 frictions for Sprint 5.0.

## Validation Criteria
1. Players can feel the difference between jug and crimp fatigue.
2. Smearing only works on slab angles.
3. Flag visibly prevents barn-door in lateral moves.
4. Torso rotation extends reach toward out-of-range holds.

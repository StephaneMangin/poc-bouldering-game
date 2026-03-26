# MVP Backlog

## Priority P0 (mandatory)
1. Stable hands/feet climbing control.
2. Endurance + grip + local fatigue.
3. **Physics model: COG + weight distribution** (see [Physics Model](../02_systems/physics_model.md)).
4. **PhysicMaterials: wall friction, hold rubber, crash pad** (see [Physics Model](../02_systems/physics_model.md)).
5. **Wall angle physics** (slab → vertical → overhang → roof scaling).
6. **Audio system: climbing sounds + gym ambiance + breathing** (see [Audio](../02_systems/audio.md)).
7. 5 playable MVP routes with **color coding**.
8. Arcade and Simulation modes.
9. Score + post-run debrief.
10. Keyboard rebinding via interface.

## Priority P1 (high value)
1. **Grip type differentiation** (jug, crimp, open, pinch, gaston, undercling).
2. **Foot techniques** (heel hook, toe hook, smearing, edging).
3. **Torso rotation** (flag, drop knee, twist lock, back step).
4. **Dyno ballistics** (launch vector, catch window, gravity arc).
5. **Art direction**: hold materials, wall textures, gym environment assets.
6. **Route color coding**: real gym convention (green/blue/red + tags).
7. Local ghost run.
8. **Pendular physics** (barn-door swing + recovery).
9. Style badges (fluid/precise).
10. Accessibility feedback options.

## Priority P2 (post-MVP)
1. **Visual effects**: chalk particles, finger animation per grip type, muscle feedback.
2. **Mantling** (top-out push animation).
3. **Knee bar / bicycle** techniques.
4. **Camera replay / beta view**.
5. **Gym decorative elements** (benches, chalk buckets, background climbers).
6. **Breathing animation** tied to endurance state.
7. Online leaderboard.
8. Additional biome.
9. Daily challenges.

## Key User Stories
1. As a new player, I want to understand the controls in under 2 minutes.
2. As a competitive player, I want a detailed score to optimize my runs.
3. As a keyboard player, I want to remap my keys without restarting.
4. As a player, I want to feel physical tension building through audio and visual feedback.
5. As an experienced climber, I want to use real climbing techniques (heel hook, drop knee) for better performance.
6. As a player, I want to see and hear the impact of my weight distribution on climbing performance.

## Global MVP Acceptance Criterion
1. 70% of players complete at least one route in Arcade within 15 min.
2. Failure causes are readable in 80% of test runs.
3. The debrief proposes at least one concrete improvement action.

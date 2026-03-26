# 14-Week Roadmap

## S1.0-S1.1: Control and Feel Prototype (DONE)
1. Hands/feet control + stable camera.
2. Humanoid mannequin + IK + orbital camera.
3. Physics-driven fall + lane-organized wall.
4. Ground locomotion.
5. Gate: playable climbing loop for 5 min without blocking.

## S2.0: Physics Model Foundation
1. COG computation + weight distribution.
2. PhysicMaterials (wall, holds, crash pad).
3. Wall angle physics (slab → vertical → overhang).
4. Basic body-wall contact.
5. Gate: COG visibly affects climbing effort.

## S3.0: Resource Systems + Physics Integration
1. Endurance + grip + local fatigue driven by physics model.
2. Chalk action + cooldown.
3. Resource HUD.
4. Gate: resources respond realistically to body position and wall angle.

## S4.0: Body Mechanics & Grip Types
1. Grip type differentiation (jug, crimp, open, pinch).
2. Basic foot techniques (edging, smearing).
3. Flag + basic torso rotation.
4. Hold affordances (allowedGripTypes).
5. Gate: technique choice visibly impacts performance.

## S5.0: Advanced Techniques & Dyno
1. Heel hook, toe hook implementation.
2. Drop knee / twist lock.
3. Dyno ballistics (launch, catch window, gravity arc).
4. Pendular physics (barn-door swing + recovery).
5. Gate: 4 techniques usable on test wall.

## S6.0: Audio System
1. Climbing contact sounds (shoe, hand, chalk).
2. Breathing / physical effort audio.
3. Gym ambiance (background music, atmosphere).
4. Feedback signals (grip warning, fatigue pulse, fall impact).
5. Gate: audio enhances climbing readability.

## S7.0: Arcade/Simulation Modes + Scoring
1. Mode rules + multipliers.
2. Fall handling per mode.
3. Detailed score + debrief with recommendations.
4. XP progression L1-L10 + unlocks.
5. Gate: score is understandable, progression motivating.

## S8.0: Routes R1-R3 + Art Direction
1. R1, R2, R3 blockout with wall panel variety.
2. Route color coding (green, blue).
3. Hold materials + wall textures (first art pass).
4. Gym environment base (crash pads, lighting).
5. Gate: 3 routes playable with distinct visual identity.

## S9.0: Routes R4-R5 + Difficulty Curve
1. R4 blockout (overhang, advanced techniques).
2. R5 blockout (roof, all techniques).
3. Easy/Medium/Hard difficulty pass.
4. Advanced techniques required by route design.
5. Gate: 5 routes with consistent difficulty progression.

## S10.0: UX, Rebinding & Visual Polish
1. Complete `Options > Controls` menu + rebinding.
2. Accessibility feedback options.
3. Visual effects (chalk particles, muscle feedback).
4. Finger animation per grip type.
5. Gate: polished UX and visual feedback.

## S11.0: Mantling, Knee Bar & Advanced Moves
1. Mantling (top-out) implementation.
2. Knee bar + bicycle techniques.
3. Back step refinement.
4. Advanced route variants using new techniques.
5. Gate: all planned techniques functional.

## S12.0: External Playtests
1. 2 external sessions (novices + challenge players).
2. UX/control friction analysis.
3. Audio/visual feedback evaluation.
4. P0 fixes integrated and verified.
5. Gate: MVP completion criteria near threshold.

## S13.0: Polish & Balancing
1. Final balancing pass (resources, scoring, difficulty).
2. Gym environment completion (decorative elements).
3. Camera polish (replay angle, beta view).
4. Performance verification (60 FPS target).
5. Gate: all systems tuned and stable.

## S14.0: MVP Freeze & Demo
1. Scope freeze (no new feature).
2. Critical bug fixes + stability.
3. Demo packaging + release notes.
4. Go/no-go checklist validation.
5. Gate: presentable MVP build.

## Planning Risks
1. Over-investment in animation before gameplay validation.
2. Physics model complexity delaying resource integration.
3. Audio asset production timeline.
4. Technique count overwhelming player learning curve.
5. Scoring too late (delays replayability).
6. Incomplete rebinding (UX blocker).

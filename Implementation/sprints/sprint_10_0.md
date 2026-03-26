# Sprint 10.0 Checklist (5 days)

## Sprint Objective
Visual polish: chalk particles, finger animations, muscle feedback, accessibility options.

## Definition of Done
1. Chalk dust particles on hand-on-hold contact and chalk application.
2. Finger animation varies per grip type (crimp curl, open spread, pinch opposition).
3. Muscle feedback: forearm redness + visible veins at critical fatigue.
4. Breathing animation tied to endurance state.
5. Accessibility feedback options (intensity, colorblindness).

## Day 1
1. Implement `VisualEffectsSystem`.
2. Create chalk dust particle system (emit from hands on contact + chalk action).
3. Tune particle lifetime, spread, gravity.

## Day 2
1. Implement per-grip-type finger animation blending.
2. Crimp: curled fingers, thumb lock.
3. Open hand: relaxed spread.
4. Pinch: thumb opposition.
5. Jug: full wrap.

## Day 3
1. Implement muscle feedback visuals: forearm redness at fatigue > 60.
2. Add stylized vein overlay at fatigue > 80.
3. Hand tremor animation at fatigue > 80 (enhance existing).

## Day 4
1. Implement breathing animation (chest/shoulder rise/fall tied to endurance).
2. Add accessibility feedback options (toggle visual intensity, colorblind modes).
3. Connect options to HUD color scheme.

## Day 5
1. Visual polish playtest: verify effects enhance readability without distraction.
2. Performance verification (particle budget, shader cost).
3. Fix visual artifacts.

## Validation Criteria
1. Chalk particles are visible but not distracting.
2. Players can distinguish grip types by finger animation.
3. Fatigue visual feedback matches audio breathing state.
4. 60 FPS maintained with all effects active.

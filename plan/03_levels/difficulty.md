# Difficulty

## Objective
Offer three clear tiers (`Easy`, `Medium`, `Hard`) without changing the identity of the routes.

## Principles
1. Difficulty adjusts error margins, not the basic controls.
2. The player must understand why the same route is more demanding.
3. UI/UX feedback remains identical across difficulties.

## Adjusted Parameters
1. Hold auto-aim window.
2. Endurance costs.
3. Local fatigue accumulation.
4. Grip loss.
5. Fall penalties.

## Scaling Table
1. Easy:
1. Wide auto-aim.
2. Endurance: global cost `-10%`.
3. Fatigue: accumulation `-12%`.
4. Grip: global loss `-10%`.
5. Arcade fall: penalty `-20%`.
2. Medium:
1. Reference tuning.
3. Hard:
1. Narrow auto-aim.
2. Endurance: global cost `+10%`.
3. Fatigue: accumulation `+10%`.
4. Grip: global loss `+12%`.
5. Arcade fall: penalty `+20%`.

## Recommended Gating
1. Easy always available.
2. Medium unlocked by default.
3. Hard recommended after 2 medium completions (not locked).

## Post-Playtest Adaptation
1. If completion rate < 35% on a route at Medium, reduce one parameter by max 5% per iteration.
2. If completion rate > 80% on Hard, increase a constraint by 3 to 5%.
3. Always change one parameter block at a time to isolate the effect.

## Validation Criteria
1. Expected completion gap: Easy > Medium > Hard.
2. Players must not perceive "unfair" difficulty without associated feedback.
3. Mastery progression between difficulties must be measurable (fewer falls, better fluidity score).

## Tuning Reference
Source of truth for numerical values: [Gameplay Variables](../04_technical/variables_gameplay.md).
In case of discrepancy, the value in `variables_gameplay.md` prevails.

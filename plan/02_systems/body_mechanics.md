# Body Mechanics

## Objective
Model realistic climbing body mechanics so that technique choice (grip type, foot placement, torso rotation) directly impacts physics, fatigue, and score.

## Grip Types

### Principle
Each hold affords specific grip types. The chosen grip affects fatigue rate, max load, and injury risk.

### Types
1. **Jug** (full hand wrap): easiest, lowest fatigue, highest max load. Primarily large holds.
2. **Crimp** (closed fingers, thumb optional): high load on small edges, fast fatigue, finger injury risk at overload.
3. **Open Hand** (relaxed fingers): moderate load, low fatigue, good for slopers. Less precise.
4. **Pinch** (thumb opposition): moderate fatigue, requires thumb strength. Specific to pinch holds.
5. **Gaston** (outward push with elbow out): high shoulder fatigue, used on side-pulls facing away.
6. **Undercling** (palm-up pull): high bicep fatigue, used on bottom-facing holds. Effective when feet are high.

### Fatigue Multipliers (per grip type)
1. Jug: ×0.6 (reference minimum).
2. Open Hand: ×0.8.
3. Pinch: ×1.0 (reference).
4. Crimp: ×1.3.
5. Gaston: ×1.4.
6. Undercling: ×1.5.

### Hold-Grip Compatibility
1. Each `HoldComponent` exposes `allowedGripTypes[]`.
2. A grip type not in the list cannot be used on that hold.
3. Player auto-selects the best compatible grip (lowest fatigue) unless manually overridden.

## Foot Techniques

### Principle
Feet are the primary support. Good footwork reduces hand load and extends endurance.

### Techniques
1. **Edging** (toe edge on small foothold): standard, precise, moderate stability.
2. **Smearing** (rubber sole flat on wall surface): no hold needed, friction-dependent, slab only.
3. **Heel Hook** (heel on hold, pull with hamstring): transfers weight to leg, reduces arm fatigue by 30-50%. Requires hold above or behind hip.
4. **Toe Hook** (top of foot on hold, pull with shin): used in overhangs to prevent barn-door. Requires hold above or to the side.
5. **Bicycle** (toe hook + heel hook in opposition): locks body in overhang, very effective but high leg fatigue.
6. **Knee Bar** (knee + foot in opposition against features): hands-free rest in specific geometry. Zero arm fatigue while active.

### Weight Transfer Modifiers
1. Edging: shifts 50-60% of weight to foot.
2. Smearing: shifts 30-40% of weight to foot (friction-dependent).
3. Heel Hook: shifts 40-50% of weight to hooking leg, reduces `weight_ratio_hands` by 0.15-0.25.
4. Toe Hook: anti-swing stabilization, reduces `weight_ratio_hands` by 0.10-0.15.
5. Knee Bar: shifts 80-100% of weight to legs, `weight_ratio_hands` → 0.0.

### Unlock Progression
1. Edging: available from L1.
2. Smearing: available from L3.
3. Heel Hook: available from L5.
4. Toe Hook: available from L6.
5. Knee Bar: available from L7.
6. Bicycle: available from L8.

## Torso Rotation & Hip Techniques

### Principle
Torso rotation extends reach, reduces arm load, and enables advanced body positions.

### Techniques
1. **Flag** (extending a leg out for balance): shifts COG, prevents barn-door on lateral moves. Available from L2.
2. **Drop Knee / Egyptian** (rotating hips in, dropping inside knee): extends reach by 15-20 cm, reduces arm load by 20%. Available from L4.
3. **Twist Lock** (rotating torso to extend reach with outside arm): extends effective reach by 10-15 cm. Available from L3.
4. **Back Step** (turning hips into the wall, outside edge of foot): reduces COG offset on steep walls. Available from L3.

### Rotation Limits
1. Max hip rotation: ±60° from wall-facing neutral.
2. Max torso twist: ±45° from hips.
3. Rotation speed: ~120°/s (not instant).

## Anatomical Reach Model

### Principle
Reach is constrained by skeleton proportions, not a flat radius. Shoulder position, arm length, and body orientation define the reachable volume.

### Rules
1. Base reach radius = `arm_length = 0.65` m (from shoulder joint).
2. Effective reach = shoulder position + arm_length + torso rotation bonus.
3. Drop knee adds +0.15 m effective vertical reach.
4. Twist lock adds +0.12 m effective lateral reach.
5. Stretch limit: reaching > 90% of max produces `overextend_fatigue_mult = 1.5`.
6. Reach below feet requires undercling or specific body position.

## Mantling (Top-Out)

### Principle
Finishing a boulder problem requires pushing over the top of the wall — a distinct movement from regular climbing.

### Rules
1. Triggered when both hands are on the top edge.
2. Player must push down (not pull) — uses tricep/shoulder (different fatigue pool).
3. Requires endurance > 20% and arm fatigue < 90%.
4. Animation: hands push, legs swing up, stand on top.
5. Available from L1 (simplified) — refined animation at L6.

## Tension & Body Awareness

### Principle
Core body tension connects upper and lower body. Without tension, feet cut loose on overhangs.

### Rules
1. `body_tension` = derived value from COG stability + contact point geometry.
2. Low body tension → feet cut (lose foot contact unexpectedly).
3. High body tension → efficient overhang climbing, less swing.
4. Body tension improves with player level (passive bonus L1-L10).

## Tuning Reference
Source of truth for numerical values: [Gameplay Variables](../04_technical/variables_gameplay.md).
In case of discrepancy, the value in `variables_gameplay.md` prevails.

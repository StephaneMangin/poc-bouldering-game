# Method: Diagnosing a Character Sunk into the Ground (Unity CharacterController)

## Problem Context

The character is visible in-game but its feet (or entire body) clip through the ground surface. The CharacterController (CC) appears to work (no infinite fall) but the visual model is not aligned with the physical surface.

This type of bug is insidious because it rarely comes from a single cause — it's often the combination of multiple geometric misalignments that stack up.

---

## Diagnostic Methodology

### 1. Map the Complete Geometric Chain

Before touching anything, document the **transform stack** of the character:

```
Player (Transform)
  └── VisualContainer (Transform + Scale)
      └── Model/Animator (Transform, possibly offset)
          └── Skeleton (bones, feet)
  └── CharacterController (height, center, radius, skinWidth)
```

**For each node, record:**
- `localPosition` (especially Y)
- `localScale` (especially Y)
- CC values: `height`, `center.y`, `radius`, `skinWidth`

### 2. Compute CC Bottom in World Coordinates

```
CC_bottom_local = center.y - height/2
CC_bottom_world = player.position.y + CC_bottom_local
```

This point must coincide with the contact surface. If `CC_bottom_world ≠ surface_Y`, the CC is either sunken or floating.

### 3. Compute Visual Feet Position in World Coordinates

Account for **all** scales and offsets in the chain:

```
feet_local_Y = model.localPosition.y × parentScale.y
feet_world_Y = player.position.y + feet_local_Y
```

If `feet_world_Y ≠ CC_bottom_world`, there is a CC/visual misalignment.

### 4. Check Animation Import Settings

For Humanoid animations, check in `.fbx.meta` files:

| Parameter | Effect |
|---|---|
| `heightFromFeet: 1` | Root Y position is recalculated from feet → feet pinned to ground |
| `heightFromFeet: 0` | Root Y position uses original clip value → possible offset |
| `keepOriginalPositionY: 0` | Root Y is free (best practice with CC) |
| `keepOriginalPositionY: 1` | Root Y is locked to clip value |

**Rule:** For a character driven by CharacterController (no Y root motion), use `heightFromFeet: 1` + `keepOriginalPositionY: 0`.

### 5. Check Consistency Between CC and Scaled Model

If the visual container has a scale ≠ 1:

```
CC_height_correct = model_height_original × scale.y
CC_center_y_correct = CC_height_correct / 2
```

The CC must be sized for the model **as it appears**, not for the original model.

---

## Pitfalls to Avoid

### ❌ Patching the Symptom Instead of the Cause
Adding offsets (`footGroundOffset`, `spawnY += 0.05`) to compensate for a misalignment you don't understand. These patches stack up, interact with each other, and mask the real problem.

### ❌ Modifying a Parameter Without Understanding the Geometric Chain
Changing `skinWidth`, `center.y`, or `spawn.y` in isolation without computing the full transform stack. A fix on one link can break another link.

### ❌ Ignoring the Visual Container Scale
A scale of `(0.7, 0.9, 0.7)` on a parent transforms a 1.8m height into an effective 1.62m. The CC must reflect this reality, not the original FBX dimensions.

### ❌ Ignoring Animation Import Settings
`heightFromFeet` is an import parameter that's often overlooked. With `heightFromFeet: 0`, Unity's Humanoid system positions the skeleton root at the "original" height from the Mixamo/FBX clip, which doesn't necessarily match the game's ground surface.

### ❌ Assuming the Model is at localPosition.y = 0
Imported FBX prefabs sometimes have a Y offset due to export (Blender, Mixamo). This offset is multiplied by the parent's scale.

### ❌ Ignoring Differences Between Animation Variants
Verify that **all** clips (idle, walk, land, etc.) share the same import settings. A single clip with `heightFromFeet: 0` can cause a visible vertical jump at transitions.

### ❌ Only Testing in the Editor
`CC.isGrounded` can behave differently in editor vs batch mode vs build. Always validate with automated tests that check absolute Y position, not just `isGrounded`.

---

## Verification Checklist

- [ ] **CC dimensions**: `height` and `center.y` match the scaled model (not the original FBX)
- [ ] **CC bottom**: `center.y - height/2 = 0` (feet at the Player transform origin)
- [ ] **Model localPosition.y**: `0` (no Y offset in the hierarchy)
- [ ] **Humanoid animations**: `heightFromFeet: 1` on all clips
- [ ] **Humanoid animations**: `keepOriginalPositionY: 0` on all clips
- [ ] **Spawn position**: `player.y` places `CC_bottom_world` on the ground surface
- [ ] **Consistency**: bootstrapper/factory code uses the same values as the scene
- [ ] **Tests**: dimension oracles in tests reflect the corrected values

---

## Concrete Example: Bouldering Game (poc-bouldering-game)

### Symptom
The character walked but appeared **under the ground** (feet clipping through the crash pad).

### Initial Configuration (buggy)

```
Player (0, 0.3, 0.8)                    ← CC bottom world = 0.3 + 0.0 = 0.3 ✓
  ├── CC: height=1.8, center=(0, 0.9, 0) ← bottom local = 0.9 - 0.9 = 0.0
  └── BodyVisual (scale 0.7, 0.9, 0.7)
      └── Mannequin (0, 0.1449, 0)       ← Y offset × scale = 0.13m
CrashPad top: Y = 0.3
Male Walk animations: heightFromFeet = 0 (6 clips)
```

### 3 Root Causes Identified

1. **`heightFromFeet: 0`** on all 6 Male Walk clips — the skeleton root was positioned by the original Mixamo clip value, not recalculated from the feet. Female Walk clips correctly had `heightFromFeet: 1`.

2. **Mannequin `localPosition.y = 0.14486`** — a Mixamo export offset, multiplied by the parent's Y scale (0.9), created a ~0.13m offset between the visual feet and the CC bottom.

3. **CC sized for the original model** — `height=1.8, center.y=0.9` corresponded to the raw FBX, but the BodyVisual at `scale.y=0.9` made the effective model 1.62m. The CC enclosed empty space below the feet.

### Fixes Applied

| File | Before | After |
|---|---|---|
| 6× Male Walk `.fbx.meta` | `heightFromFeet: 0` | `heightFromFeet: 1` |
| Scene — Mannequin | `localPosition.y: 0.14486` | `localPosition.y: 0` |
| Scene — CC | `height: 1.8, center.y: 0.9` | `height: 1.62, center.y: 0.81` |
| PrototypeBootstrapper.cs | `height=1.8f, center.y=0.9f` | `height=1.62f, center.y=0.81f` |
| GroundLocomotionTests.cs | `ExpectedCCHeight=1.8f` | `ExpectedCCHeight=1.62f` |

### Resulting Geometry (corrected)

```
Player (0, 0.3, 0.8)                      ← CC bottom world = 0.3 ✓
  ├── CC: height=1.62, center=(0, 0.81, 0) ← bottom local = 0.81 - 0.81 = 0.0
  └── BodyVisual (scale 0.7, 0.9, 0.7)
      └── Mannequin (0, 0, 0)              ← feet at Player origin ✓
CrashPad top: Y = 0.3                      ← = CC bottom world ✓
Animations: heightFromFeet = 1              ← feet pinned to ground ✓
```

### Key Lesson
The problem was not in any script — it was a **configuration** inconsistency between three systems (animation import, transform hierarchy, CC dimensions). No code patch could fix it cleanly; only aligning the entire geometric chain resolved the problem at the root.


## Post-Resolution Summary

Summary of corrections — systemic approach, no patches:

Root cause: 3 geometric inconsistencies between the physical CC and the visual model.

1. Animations — heightFromFeet: 0 → 1 (6 `.meta` files)

All Male Walk clips (idle, walking, landing, start/stop_walking, walking_backward) had `heightFromFeet: 0` while Female Walk correctly had `1`.
`heightFromFeet: 0` = the Humanoid system uses the "original" root Y position instead of recalculating from feet → incorrect vertical alignment.
Files: idle.fbx.meta, walking.fbx.meta, landing.fbx.meta, + 3 others.

2. Scene — Mannequin localPosition.y: 0.14486 → 0

The Mannequin Y offset inside BodyVisual (scaled 0.9 vertically) created a 0.13m gap between the CC bottom and the visual feet.
Now: visual feet = CC bottom = CrashPad surface.
File: VerticalPrototype.unity.

3. Scene — CC height: 1.8 → 1.62, center.y: 0.9 → 0.81

The CC was sized for a 1.8m model but the BodyVisual Y scale of 0.9 makes an effective 1.62m model.
Now: height = 1.8 × 0.9 = 1.62, center.y = height/2 = 0.81.
CC bottom = 0.81 - 0.81 = 0.0 relative to Player → consistent with feet.
File: VerticalPrototype.unity.

Associated cleanups:
- `m_EditorClassIdentifier` corrected: ClimbingStateMachineDriver → PlayerStateMachineDriver (GUID was already correct).
- PrototypeBootstrapper.cs: CC dimensions synchronized.
- GroundLocomotionTests.cs: CC oracles updated.
Resulting geometry:

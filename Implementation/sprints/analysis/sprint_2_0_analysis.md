# Sprint 2.0 Codebase Analysis — Physics Model Foundation

**Status:** Sprint 1.1 COMPLETE | Next: Sprint 2.0 Physics Model Implementation
**Gate Criterion:** COG visibly affects climbing effort

---

## Project Structure

### Directory Tree: Assets/BoulderingGym/
```
Scripts/
  ├─ Core/                           # Shared systems
  │  ├─ Domain/TuningConfig.cs       # ScriptableObject (currently minimal)
  │  ├─ Events/                      # Event bus (InMemoryEventBus, GameEvent, EventType)
  │  ├─ Input/InputReader.cs         # Legacy + NewInputSystem support
  │  └─ Utilities/                   # TwoBoneIKSolver, WallGeometry
  ├─ Features/Climbing/              # Main climbing feature
  │  ├─ Domain/                      # FSM (5 states: IdleGround, Reach, GripStable, Falling, RunEnd)
  │  │  └─ States/                   # Individual state implementations
  │  ├─ Application/ClimbingStateMachine.cs
  │  └─ Presentation/                # Controllers (Player, Visual, Camera, Holds, Locomotion)
  ├─ Infrastructure/Persistence/JsonSaveService.cs
  └─ Tools/Editor/Sprint*PrototypeBootstrapper.cs
Prefabs/
  └─ Holds/Hold_{Crimp,Jug,Sloper,Pinch}.prefab
Scenes/VerticalPrototype.unity
Data/Gameplay/
```

---

## 39 C# Scripts Summary

### Core Systems
1. **TuningConfig** — ScriptableObject with grab/move/endurance basics (INCOMPLETE for S2.0)
2. **InMemoryEventBus** — Dictionary-based pub/sub (5 events: HoldGrabbed, HoldSlip, FatigueCritical, RunCompleted, RunFailed)
3. **InputReader** — Keyboard + mouse (layout-safe ZQSD/WASD)
4. **TwoBoneIKSolver** — 2-bone analytical IK with bend hints
5. **WallGeometry** — Wall normal, lateral, approach calculations

### FSM & States
- **ClimbingStateMachine** — State machine orchestrator
- **Base:** ClimbingState abstract with HandleTrigger()
- **Known States (5/13):**
  - IdleGround → Reach (on grab request)
  - Reach → GripStable (successful grab) or IdleGround (failed)
  - GripStable → Reposition|Resting|Falling (on release/support loss)
  - Falling → IdleGround (on land)
  - RunEnd → IdleGround (restart)

### Player Controllers
1. **PlayerHandsController** — Hand anchors, best-hold detection (radius + angle), grab/release logic
   - Methods: TryFindBestHold(), TryGrab(), Release(), SetIkEnabled()
2. **HipsMovementController** — Ground movement, wall distance clamping
3. **FallPhysicsController** — Fall state sync, Rigidbody config, reset-on-low-Y
4. **ClimbingStateMachineDriver** — Event bridge, hold registry integration

### Visual Controllers
1. **ClimberVisualOrchestrator** — Master coordinator, ground walk metrics
2. **ClimberAnimatorController** — Animator parameter updates (ClimbingState, moveX/Z, speed, is_grounded)
3. **ClimberPostureController** — Torso/head rotation toward COG support center
4. **ClimberIKController** — 2-bone IK application for all limbs, foot grounding
5. **ClimberFeedbackController** — Material color feedback by state
6. **HumanoidRigMapping** — Rig auto-detection, validation
7. **AnimatorIKRelay** — OnAnimatorIK → orchestrator bridge

### Hold & Layout
1. **HoldComponent** — Hold data (quality, gripTexture), feedback color, gripPoint transform
2. **HoldRegistry** — Find/cache all holds in scene
3. **HoldLaneLayoutController** — Grid placement (3 lanes × 4 holds, start Y, spacing)

### Camera & Locomotion
1. **CameraFollowController** — Orbit (mouse RMB), zoom (wheel), recenter (C key)
2. **GroundRootMotionDriver** — Root motion to world space, distance clamping

### UI & Persistence
1. **PlaytestTuningOverlay** — F1 hidden/visible, sliders for grab range / move speed / camera smooth
2. **JsonSaveService** — Simple JSON persistence

---

## Current Physics Setup

### Rigidbody Configuration
- **Default (climbing):** Kinematic, FreezeRotation, FreezePositionZ
- **Falling:** Dynamic, useGravity=true, FreezeRotation, FreezePositionZ
- **Constraints:** Player never rotates in X/Y; Z frozen except during fall

### Physics Colliders
- **Walls:** Box colliders on wall panels (no Physics Materials)
- **Holds:** Box colliders on prefabs (no Physics Materials)
- **Player:** Capsule collider (kinematic during climb)
- **Contact Detection:** Only grab radius (3D distance) + angle check; NO physics contacts

### PhysicMaterials
**NONE EXIST** — This is S2.0 Task P0

Required (per variables_gameplay.md):
- PM_Wall_Rough: static=0.8, dynamic=0.6
- PM_Wall_Smooth: static=0.5, dynamic=0.3
- PM_Hold_Rubber: static=0.9, dynamic=0.7
- PM_Hold_Wet: static=0.3, dynamic=0.2
- PM_CrashPad: static=0.9, dynamic=0.8, bounciness=0.15
- PM_Shoe_Rubber: static=0.95, dynamic=0.8

### Physics-Related Gaps
1. **NO COG computation** — Support center is just average of hand/foot positions (no mass weighting)
2. **NO weight distribution** — All limbs treated equally; no weight_on_hand / weight_on_feet calculation
3. **NO wall angle detection** — Angle not measured; all multipliers stay at 1.0
4. **NO stability polygon** — Can't detect if COG is outside support
5. **NO body-wall contact** — Player body never contacts wall surface for friction
6. **NO swing physics** — BarnDoor state exists but no pendular math
7. **NO dyno ballistics** — DynamicMove state exists but no launch vector calculation

---

## Hold/Wall Structure

### Hold Prefabs
- **Files:** Hold_Crimp, Hold_Jug, Hold_Sloper, Hold_Pinch
- **Structure:** 
  - HoldComponent script with HoldData (holdQuality enum, gripTextureHold float 0.5-2.0)
  - gripPoint transform (child) for IK target
  - Box collider
  - Renderer with standard material
- **Quality Enum:** Fragile, Standard, Reliable (affects grab success rate)
- **Texture:** 0.5=slippery, 1.0=standard, 2.0=sticky

### Wall Geometry
Built by Sprint1_1PrototypeBootstrapper:
- **WallDef struct:** xMin, xMax, height, scaleZ (thickness), angleX (degrees)
- **Panels:** 
  - Slab 0° (left wall)
  - Vertical 0° (center-left & center-right)
  - Overhang -15° (right-left)
  - Overhang -30° (right-center)
  - Overhang -45° (right-right)
- **Placement:** Center auto-computed so bottom-front edge sits at Y=0, Z=WallBaseZ
- **Embedded holds:** Small offset from surface so mostly embedded, only front cap visible

### Hold Placement (HoldLaneLayoutController)
- **Lanes:** Left X=-2.8, Center X=-0.2, Right X=2.4
- **Rows:** startY=1.2, verticalSpacing=0.9 (4 holds per lane)
- **Jitter:** horizontalJitter=0.16 (alternating rows offset)
- **Depth:** baseZ=1.7 + laneZStep=0.08 per lane
- **Refresh:** Calls HoldRegistry.Refresh() after layout

---

## TuningConfig ScriptableObject (Current vs. Required)

### Currently Populated
```csharp
grabRange = 1.6f
grabAngleMax = 35f
hipsMoveSpeed = 2.0f
enduranceMax = 100f
enduranceCostGrab = 2f
```

### MISSING for Sprint 2.0 (From variables_gameplay.md)
**Physics Model (COG & Weight Distribution):**
- cog_max_offset = 0.6 (0.4-0.8)
- cog_offset_penalty_start = 0.2 (0.1-0.3)
- cog_offset_penalty_mult = 2.0 (1.5-3.0)
- weight_ratio_hands_vertical = 0.35
- weight_ratio_hands_overhang_strong = 0.70
- weight_ratio_hands_roof = 0.90

**Wall Angle Multipliers (6 × 3 = 18 values):**
- wall_angle_endurance_mult_{slab,vertical,overhang,roof}
- wall_angle_fatigue_mult_{slab,vertical,overhang,roof}
- wall_angle_grip_drain_mult_{slab,vertical,overhang,roof}

**Pendular & Dyno:**
- swing_recovery_window_s = 0.8
- dyno_base_force = 8.0
- dyno_catch_window_s = 0.3
- fall_height_heavy_threshold = 3.0
- slab_body_friction = 0.4

---

## Input System

### Keyboard Layout (Layout-Safe)
- **Z/W/Arrow-Up:** Move forward toward wall
- **Q/A/Arrow-Left:** Move left
- **D/Arrow-Right:** Move right
- **S/Arrow-Down:** Move backward away from wall
- **J:** Left grab
- **E/L:** Right grab
- **U:** Release left
- **O:** Release right
- **R:** Release both
- **Space:** Land (testing)
- **F:** Force fall (testing)
- **C:** Camera recenter
- **Shift:** Sprint (affects walk speed)
- **Mouse RMB:** Orbit camera
- **Mouse Wheel:** Zoom
- **F1:** Toggle overlay

### Input Reader Features
- #if ENABLE_INPUT_SYSTEM conditional (falls back to KeyCode)
- Consume pattern (GetKeyDown once per frame)
- Read pattern (GetKey for continuous, e.g., movement)

---

## Event Bus & Events

### InMemoryEventBus Implementation
- Dictionary<EventType, List<Action<GameEvent>>>
- Subscribe/Unsubscribe/Publish pattern
- Snapshot iteration (safe removal during publish)

### Current EventTypes (5)
- HoldGrabbed → published in GripStableState.OnEnter()
- HoldSlip → (defined but never published)
- FatigueCritical → (defined but never published)
- RunCompleted → (defined but never published)
- RunFailed → published in FallingState.OnEnter()

### MISSING for Sprint 2.0
- COG_UNSTABLE
- BARN_DOOR_SWING
- FOOT_CUT
- TECHNIQUE_ACTIVATED
- ON_STATE_ENTER / ON_STATE_EXIT
- ON_LANDING

---

## IK System

### TwoBoneIKSolver
- **Algorithm:** Analytical 2-bone IK with bend hints
- **Inputs:** upper bone, lower bone, end effector, target position, bend hint position
- **Clamping:** Clamps reach to [|L1-L2|, L1+L2] range
- **Bend calculation:** Projects bend hint onto target plane, uses Pythagoras to solve angles
- **No limits:** Solver doesn't enforce joint angle constraints (done post-solve)

### Runtime IK Application
**In ClimberIKController.ApplyLimbPose():**
1. L/R arms: TwoBoneIKSolver(upperArm, forearm, hand, handTarget, elbowHint)
2. L/R legs: TwoBoneIKSolver(upperLeg, lowerLeg, foot, footTarget, kneeHint)
3. Post-IK: ApplyJointLimits() clamps to elbowPitchRange [5°-150°], kneePitchRange [8°-160°], hip/shoulder swing ranges

### Hand Targets
- **During climb (GripStable/Reach):** Use grabbed hold's gripPoint or PlayerHands.LeftHandAnchor position
- **During ground walk:** Swing based on walk cycle phase + direction
- **During fall:** Drop down 0.35 units below torso

### Foot Targets
- **During climb:** FindBestFoothold() scores holds by (vertical offset, lateral offset, depth)
- **During ground walk:** Stride cycle with phase-based left/right offset, animated lift
- **During fall:** Drop down 0.25 units below pelvis

### Ground IK from Animator
- **Foot raycast:** Shoot ray downward from foot, match foot rotation to ground normal
- **AnimatorIKRelay:** OnAnimatorIK(layerIndex) → ClimberVisualOrchestrator.HandleAnimatorIK()

---

## Key Patterns & Conventions

### Null Safety
- Every component resolution wrapped in null checks
- Default fallbacks (e.g., `wallNormalReference != null ? wallNormalReference.forward : Vector3.back`)
- Early returns on null context

### Component Caching
- Animator parameters hashed on Awake (StringToHash)
- Hold array cached in HoldRegistry
- Initial rotations saved in ClimberVisualOrchestrator._initialLocalRotations

### Lazy Resolution
```csharp
if (driver == null) { driver = GetComponent<ClimbingStateMachineDriver>(); }
if (visualRoot == null) { visualRoot = transform.Find("BodyVisual") ?? transform; }
```

### Authority Pattern
- **Movement:** HipsMovementController owns position
- **Visuals:** ClimberVisualOrchestrator owns all animation/pose
- **Physics:** FallPhysicsController owns Rigidbody state
- **Input:** ClimbingStateMachineDriver owns state transitions

### Naming Conventions
- MonoBehaviours: PascalCase (PlayerHandsController)
- Methods: PascalCase (TryGrab, SetIkEnabled)
- Private fields: _camelCase (_leftCurrentHold)
- Serialized fields: camelCase (tuningConfig)
- Tuning variables: snake_case in data (cog_max_offset, see variables_gameplay.md)

### Serialization Best Practices
- All tuning in TuningConfig ScriptableObject, never hard-coded
- ScriptableObject reference marked [SerializeField] with null resolution fallback
- Editor tools (Sprint*PrototypeBootstrapper) for scene setup

---

## Identified Gaps for Sprint 2.0

### Physics Model (P0 — Blocks S2.0 Gate)
| Gap | Current State | Required | Impact |
|-----|---------------|----------|--------|
| COG Computation | Support center = avg(hand, foot) | Weighted average + mass | Weight distribution foundation |
| Weight Distribution | All limbs equal | weight_on_hand/feet by angle & COG offset | Grip/fatigue scaling |
| Wall Angle Detection | Static (not used) | Measure angle from wall normal | Multiplier selection |
| COG Offset Penalty | Not computed | offset/cog_max_offset → endurance cost | Physics-driven cost |
| Stability Polygon | No polygon | Convex hull of contacts | Detect instability |
| Body-Wall Contact | No collision check | Raycast from hips toward wall | Slab friction bonus |

### Resource Systems (P1 — Needed for S3.0)
- No grip drain calculation
- No fatigue gain rate (only skill gates remain)
- No endurance cost scaling by physics

### FSM Extensions (P1 — Prep for future)
- BarnDoor state exists but no swing math
- DynamicMove state missing entirely
- Missing 8 states per plan (Reposition, Resting, SlipRecover, Mantling, TechniqueActive, Landing, + 2 more)

### Event System (P2)
- Only 2 of 11+ planned events published

---

## Design Decisions from Plan

### Physics Model Approach (from physics_model.md)
- **Hybrid kinematic/physics:** Player stays kinematic during climbing, physics forces calculated to drive outcomes
- **COG-driven:** Center of gravity determines stability, endurance drain, fall direction
- **Weight distribution:** Hands/feet carry different loads based on COG offset and wall angle
- **No full skeleton physics:** Only track hand/foot contact points + body COM

### Tuning Source of Truth
- **File:** plan/04_technical/variables_gameplay.md
- **Convention:** snake_case, units explicit (_per_s, _seconds)
- **Ranges:** Default value + tested range for each variable
- **Versioning:** tuning_version tracked in saves and telemetry

### Reference Values for S2.0
- wall_angle_endurance_mult: 0.6 (slab) → 1.0 (vertical) → 2.5 (roof)
- wall_angle_fatigue_mult: 0.5 (slab) → 1.0 (vertical) → 3.0 (roof)
- weight_ratio_hands: 0.35 (vertical) → 0.90 (roof)
- cog_max_offset: 0.6 m before forced barn-door/fall

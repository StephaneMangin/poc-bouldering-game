# Sprint 3.2 Checklist (1 day)

## Sprint Objective
Fix the FSM Reach→GripStable guard so that EditMode tests can validate state transitions
without depending on MonoBehaviour (`PlayerHandsController` is not instantiable in EditMode).

## Definition of Done
1. Domain layer decoupled from `PlayerHandsController` concrete type.
2. FSM state guards compile and run in EditMode tests with a pure‑C# stub.
3. All EditMode FSM tests pass (StateMachineTests + ClimbingGameLoopTests).
4. Zero runtime regression — PlayMode tests and live session unaffected.

## Root Cause
`PlayerContext.PlayerHands` was typed as `PlayerHandsController` (a `MonoBehaviour`).
EditMode tests cannot instantiate MonoBehaviours, so the field was `null`. The guard in
`ReachState.HandleTrigger`:
```csharp
context.PlayerHands != null && context.PlayerHands.HasCrossLimbSupport
```
always evaluated to `false` → `GrabSucceeded` looped back to `Reach` instead of
transitioning to `GripStable`. This cascaded into every test that navigates through
Reach→GripStable.

## Key Deliverables

### Day 1 — Interface extraction + test stubs
1. Create `IPlayerHands` interface in `Features/Player/Domain/` with the 3 members
   used by FSM states: `HasAnySupport`, `HasCrossLimbSupport`, `SetIkEnabled(bool)`.
2. Change `PlayerContext.PlayerHands` type from `PlayerHandsController` to `IPlayerHands`.
3. Make `PlayerHandsController` implement `IPlayerHands` (no behavior change).
4. Add `StubPlayerHands` (mutable stub) to both EditMode test files.
5. Update test `SetUp` to inject stub with `HasCrossLimbSupport = true`.
6. Fix two "no support" tests to toggle the stub flag before the guard fires.
7. Fix `GameLoop_FullCycle` assertion: `GrabRequested` from `GripStable` stays
   in `GripStable` (match the FSM design, not the outdated test expectation).

## Architecture Decisions
1. **Minimal interface** — `IPlayerHands` exposes only the 3 members consumed by the
   domain (states). The rich API (`TryPlaceLimb`, `ReleaseLimb`, anchors, etc.) stays
   on the concrete `PlayerHandsController` for presentation‑layer consumers.
2. **No driver change** — `PlayerStateMachineDriver.PlayerHands` stays typed
   `PlayerHandsController` (used by UI, IK, posture code). The implicit upcast to
   `IPlayerHands` when setting `context.PlayerHands = playerHands` is sufficient.
3. **Mutable stub** — `StubPlayerHands` has settable properties so individual tests
   can toggle support flags (e.g. `HasCrossLimbSupport = false` before `ReleaseRequested`).

## Validation Criteria
1. All EditMode StateMachineTests pass (26 tests).
2. All EditMode ClimbingGameLoopTests pass (9 tests).
3. Zero compilation errors across the project.
4. PlayMode FSMTransitionTests compile (runtime behavior unchanged).
5. Live editor session: grab/release loop still works as in Sprint 3.1.

## Test Summary
| Suite | File | Tests | Status |
|---|---|---|---|
| State Machine | `StateMachineTests.cs` | 26 | Fixed (stub injected) |
| Game Loop | `ClimbingGameLoopTests.cs` | 9 | Fixed (stub + assertion) |
| IK Pipeline | `IKPipelineTests.cs` | 20 | Unchanged |
| Posture Physics | `ClimbingPosturePhysicsTests.cs` | 14 | Unchanged |
| Body Solver | `ClimbingBodySolverTests.cs` | 20 | Unchanged |
| **Total** | | **89** | |

## Files Changed (vs Sprint 3.1)
- **New** (1 file): `IPlayerHands.cs` (interface, 16 lines)
- **Modified** (4 files): `PlayerContext.cs` (type change), `PlayerHandsController.cs`
  (implements `IPlayerHands`), `StateMachineTests.cs` (stub + 2 test fixes),
  `ClimbingGameLoopTests.cs` (stub + 1 assertion fix)
- **Net delta**: ~+30 / −5 lines

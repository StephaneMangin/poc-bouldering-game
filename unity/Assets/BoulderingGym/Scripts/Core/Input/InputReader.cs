using Project.Features.Climbing.Domain;
using UnityEngine;

namespace Project.Core.Input
{
    /// <summary>
    /// Unified input reader — Point &amp; Place control scheme.
    /// Uses physical key positions (Use Physical Keys enabled by default since 2022.1).
    /// KeyCode maps to QWERTY physical positions regardless of OS keyboard layout.
    ///
    /// AZERTY label → Physical position → KeyCode:
    ///   Q → QWERTY A → KeyCode.A    |    A → QWERTY Q → KeyCode.Q
    ///   Z → QWERTY W → KeyCode.W    |    E → QWERTY E → KeyCode.E
    ///
    /// Movement (ZQSD):  Q=Left, D=Right, Z=Forward, S=Backward
    /// Limbs:            A=LeftHand, E=RightHand, Q=LeftFoot, D=RightFoot
    /// Quick-place:      Left-click
    /// Release all:      R
    /// Fall:             F
    /// Land:             Space
    /// Chalk:            C
    /// Sprint:           Shift
    /// Hip rotation:     ← / →     (Sprint 4.0)
    /// Torso twist:      PgUp / PgDn (Sprint 4.0)
    /// Flag (free leg):  G          (Sprint 4.0)
    ///
    /// Note: Q/D overlap between movement and limbs.
    /// The driver handles context: movement in IdleGround, limbs in Reach/GripStable.
    ///
    /// Input source is injected via <see cref="OverrideSource"/> so the UAT playtest
    /// harness can drive the game from a scenario file without touching production code.
    /// </summary>
    public sealed class InputReader : MonoBehaviour
    {
        private IInputSource _source = new UnityInputSource();

        /// <summary>Swap the underlying input source (used by tests + UAT scenario runner).</summary>
        public void OverrideSource(IInputSource source)
        {
            _source = source ?? new UnityInputSource();
        }

        // ── Movement (WASD + ZQSD + arrows) ──

        public Vector2 ReadMove()
        {
            var h = 0f;
            var v = 0f;

            // Left: Q
            if (_source.GetKey(KeyCode.A)) h -= 1f;
            // Right: D
            if (_source.GetKey(KeyCode.D)) h += 1f;
            // Forward: Z
            if (_source.GetKey(KeyCode.W)) v += 1f;
            // Backward: S
            if (_source.GetKey(KeyCode.S)) v -= 1f;

            return new Vector2(h, v);
        }

        // ── Limb selection (AZERTY: A / E / Q / D) ──
        // KeyCode = physical QWERTY position (Use Physical Keys enabled).
        // AZERTY 'A' = physical Q → KeyCode.Q    AZERTY 'Q' = physical A → KeyCode.A

        public LimbId? ConsumeActiveLimb()
        {
            if (_source.GetKeyDown(KeyCode.Q)) return LimbId.LeftHand;
            if (_source.GetKeyDown(KeyCode.E)) return LimbId.RightHand;
            if (_source.GetKeyDown(KeyCode.A)) return LimbId.LeftFoot;
            if (_source.GetKeyDown(KeyCode.D)) return LimbId.RightFoot;
            return null;
        }

        // ── Quick-place (left-click) ──

        public bool ConsumeQuickPlacePressed()
        {
            return _source.GetMouseButtonDown(0);
        }

        /// <summary>
        /// Screen-space cursor position. Centralized so callers don't reach into UnityEngine.Input directly
        /// (single migration point when moving to the New Input System per ADR-005).
        /// </summary>
        public Vector3 ReadMousePosition()
        {
            return _source.MousePosition;
        }

        // ── Global actions ──

        public bool ConsumeReleaseAllPressed()
        {
            return _source.GetKeyDown(KeyCode.R);
        }

        public bool ConsumeFallPressed()
        {
            return _source.GetKeyDown(KeyCode.F);
        }

        public bool ConsumeLandPressed()
        {
            return _source.GetKeyDown(KeyCode.Space);
        }

        public bool ReadSprint()
        {
            return _source.GetKey(KeyCode.LeftShift)
                || _source.GetKey(KeyCode.RightShift);
        }

        public bool ConsumeChalkPressed()
        {
            return _source.GetKeyDown(KeyCode.C);
        }

        // ── Body orientation (Sprint 4.0) ──
        // Arrows = hip rotation axis (Left = -1, Right = +1).
        // Page Up / Page Down = torso twist axis.
        // Q/D are taken by limb selection — using arrows avoids the conflict.

        public float ReadHipRotationAxis()
        {
            var v = 0f;
            if (_source.GetKey(KeyCode.LeftArrow)) v -= 1f;
            if (_source.GetKey(KeyCode.RightArrow)) v += 1f;
            return v;
        }

        public float ReadTorsoTwistAxis()
        {
            var v = 0f;
            if (_source.GetKey(KeyCode.PageUp)) v += 1f;
            if (_source.GetKey(KeyCode.PageDown)) v -= 1f;
            return v;
        }

        /// <summary>True while the player asks for a flag (free leg extension for lateral balance).</summary>
        public bool ReadFlagHeld()
        {
            return _source.GetKey(KeyCode.G);
        }

        // ── Gym session (Sprint 16.0) ──

        /// <summary>Space: let go of the wall and drop to the mats.</summary>
        public bool ConsumeDropPressed() => _source.GetKeyDown(KeyCode.Space);

        /// <summary>Right mouse button held: the mouse orbits the camera.</summary>
        public bool ReadOrbitHeld() => _source.GetMouseButton(1);

        public float ReadZoom() => _source.ScrollDelta;

        public bool ConsumePausePressed() => _source.GetKeyDown(KeyCode.Escape);

        /// <summary>Tab: problem list of the gym.</summary>
        public bool ConsumeBrowserPressed() => _source.GetKeyDown(KeyCode.Tab);

        /// <summary>R: back to the start of the current problem.</summary>
        public bool ConsumeRestartPressed() => _source.GetKeyDown(KeyCode.R);

        public bool ConsumeHelpPressed() => _source.GetKeyDown(KeyCode.F1);

        /// <summary>Enter: start climbing from the title screen.</summary>
        public bool ConsumeConfirmPressed() => _source.GetKeyDown(KeyCode.Return);
    }
}

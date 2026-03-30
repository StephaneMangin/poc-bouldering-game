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
    ///
    /// Note: Q/D overlap between movement and limbs.
    /// The driver handles context: movement in IdleGround, limbs in Reach/GripStable.
    /// </summary>
    public sealed class InputReader : MonoBehaviour
    {
        // ── Movement (WASD + ZQSD + arrows) ──

        public Vector2 ReadMove()
        {
            var h = 0f;
            var v = 0f;

            // Left: Q
            if (UnityEngine.Input.GetKey(KeyCode.A)) h -= 1f;
            // Right: D
            if (UnityEngine.Input.GetKey(KeyCode.D)) h += 1f;
            // Forward: Z
            if (UnityEngine.Input.GetKey(KeyCode.W)) v += 1f;
            // Backward: S
            if (UnityEngine.Input.GetKey(KeyCode.S)) v -= 1f;

            return new Vector2(h, v);
        }

        // ── Limb selection (AZERTY: A / E / Q / D) ──
        // KeyCode = physical QWERTY position (Use Physical Keys enabled).
        // AZERTY 'A' = physical Q → KeyCode.Q    AZERTY 'Q' = physical A → KeyCode.A

        public LimbId? ConsumeActiveLimb()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Q)) return LimbId.LeftHand;
            if (UnityEngine.Input.GetKeyDown(KeyCode.E)) return LimbId.RightHand;
            if (UnityEngine.Input.GetKeyDown(KeyCode.A)) return LimbId.LeftFoot;
            if (UnityEngine.Input.GetKeyDown(KeyCode.D)) return LimbId.RightFoot;
            return null;
        }

        // ── Quick-place (left-click) ──

        public bool ConsumeQuickPlacePressed()
        {
            return UnityEngine.Input.GetMouseButtonDown(0);
        }

        /// <summary>
        /// Screen-space cursor position. Centralized so callers don't reach into UnityEngine.Input directly
        /// (single migration point when moving to the New Input System per ADR-005).
        /// </summary>
        public Vector3 ReadMousePosition()
        {
            return UnityEngine.Input.mousePosition;
        }

        // ── Global actions ──

        public bool ConsumeReleaseAllPressed()
        {
            return UnityEngine.Input.GetKeyDown(KeyCode.R);
        }

        public bool ConsumeFallPressed()
        {
            return UnityEngine.Input.GetKeyDown(KeyCode.F);
        }

        public bool ConsumeLandPressed()
        {
            return UnityEngine.Input.GetKeyDown(KeyCode.Space);
        }

        public bool ReadSprint()
        {
            return UnityEngine.Input.GetKey(KeyCode.LeftShift)
                || UnityEngine.Input.GetKey(KeyCode.RightShift);
        }

        public bool ConsumeChalkPressed()
        {
            return UnityEngine.Input.GetKeyDown(KeyCode.C);
        }
    }
}
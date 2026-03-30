using Project.Features.Climbing.Domain;
using UnityEngine;

namespace Project.Core.Input
{
    /// <summary>
    /// Unified input reader — Point &amp; Place control scheme.
    ///
    /// Movement:  WASD (QWERTY) / Arrow keys (ground locomotion)
    /// Limbs:     Q = Left Hand, E = Right Hand, A = Left Foot, D = Right Foot
    /// Quick-place: Left-click (places suggested limb on targeted hold)
    /// Release all: R
    /// Fall:      F
    /// Land:      Space
    /// Chalk:     C
    /// Sprint:    Shift
    ///
    /// Note: A/D overlap between movement and limbs.
    /// The driver handles context: movement in IdleGround, limbs in Reach/GripStable.
    /// </summary>
    public sealed class InputReader : MonoBehaviour
    {
        // ── Movement (WASD + ZQSD + arrows) ──

        public Vector2 ReadMove()
        {
            var h = 0f;
            var v = 0f;

            // Left: A + Arrow
            if (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
            // Right: D + Arrow
            if (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow)) h += 1f;
            // Forward: W + Arrow
            if (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow)) v += 1f;
            // Backward: S + Arrow
            if (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow)) v -= 1f;

            return new Vector2(h, v);
        }

        // ── Limb selection (Q / E / A / D) ──

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
using UnityEngine;

namespace Project.Core.Input
{
    /// <summary>
    /// Low-level input abstraction consumed by <see cref="InputReader"/>.
    /// Default impl wraps <c>UnityEngine.Input</c>; tests and the UAT playtest
    /// harness inject a scripted source to drive the game from a scenario file.
    /// </summary>
    public interface IInputSource
    {
        bool GetKey(KeyCode key);

        bool GetKeyDown(KeyCode key);

        bool GetMouseButtonDown(int button);

        bool GetMouseButton(int button);

        Vector3 MousePosition { get; }

        /// <summary>Mouse wheel movement this frame, positive away from the user.</summary>
        float ScrollDelta { get; }
    }
}

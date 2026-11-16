using UnityEngine;

namespace Project.Core.Input
{
    /// <summary>Default <see cref="IInputSource"/> — delegates to <c>UnityEngine.Input</c>.</summary>
    public sealed class UnityInputSource : IInputSource
    {
        public bool GetKey(KeyCode key) => UnityEngine.Input.GetKey(key);

        public bool GetKeyDown(KeyCode key) => UnityEngine.Input.GetKeyDown(key);

        public bool GetMouseButtonDown(int button) => UnityEngine.Input.GetMouseButtonDown(button);

        public bool GetMouseButton(int button) => UnityEngine.Input.GetMouseButton(button);

        public Vector3 MousePosition => UnityEngine.Input.mousePosition;

        public float ScrollDelta => UnityEngine.Input.mouseScrollDelta.y;
    }
}

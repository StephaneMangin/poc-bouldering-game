using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Project.Core.Input
{
    public sealed class InputReader : MonoBehaviour
    {
        [SerializeField] private bool useLegacyFallback = true;

#if ENABLE_INPUT_SYSTEM
        private InputAction _moveAction;
        private InputAction _leftGrabAction;
        private InputAction _rightGrabAction;
        private InputAction _releaseLeftAction;
        private InputAction _releaseRightAction;
        private InputAction _releaseAllAction;
        private InputAction _fallAction;
        private InputAction _landAction;
        private InputAction _cameraRecenterAction;
        private InputAction _sprintAction;
#endif

        private void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            EnsureActionsCreated();
            _moveAction.Enable();
            _leftGrabAction.Enable();
            _rightGrabAction.Enable();
            _releaseLeftAction.Enable();
            _releaseRightAction.Enable();
            _releaseAllAction.Enable();
            _fallAction.Enable();
            _landAction.Enable();
                _cameraRecenterAction.Enable();
            _sprintAction.Enable();
#endif
        }

        private void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            _moveAction?.Disable();
            _leftGrabAction?.Disable();
            _rightGrabAction?.Disable();
            _releaseLeftAction?.Disable();
            _releaseRightAction?.Disable();
            _releaseAllAction?.Disable();
            _fallAction?.Disable();
            _landAction?.Disable();
                _cameraRecenterAction?.Disable();
            _sprintAction?.Disable();
#endif
        }

        public Vector2 ReadMove()
        {
#if ENABLE_INPUT_SYSTEM
            if (_moveAction != null)
            {
                return _moveAction.ReadValue<Vector2>();
            }
#endif
            if (!useLegacyFallback)
            {
                return Vector2.zero;
            }

            var horizontal = 0f;
            var vertical = 0f;

            if (UnityEngine.Input.GetKey(KeyCode.Q) || UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow))
            {
                horizontal -= 1f;
            }

            if (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow))
            {
                horizontal += 1f;
            }

            if (UnityEngine.Input.GetKey(KeyCode.Z) || UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow))
            {
                vertical += 1f;
            }

            if (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow))
            {
                vertical -= 1f;
            }

            return new Vector2(horizontal, vertical);
        }

        public bool ConsumeLeftGrabPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (_leftGrabAction != null && _leftGrabAction.WasPressedThisFrame())
            {
                return true;
            }
#endif
            return useLegacyFallback && (UnityEngine.Input.GetKeyDown(KeyCode.J) || UnityEngine.Input.GetMouseButtonDown(0));
        }

        public bool ConsumeRightGrabPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (_rightGrabAction != null && _rightGrabAction.WasPressedThisFrame())
            {
                return true;
            }
#endif
            return useLegacyFallback && (UnityEngine.Input.GetKeyDown(KeyCode.E) || UnityEngine.Input.GetKeyDown(KeyCode.L) || UnityEngine.Input.GetMouseButtonDown(1));
        }

        public bool ConsumeReleaseLeftPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (_releaseLeftAction != null && _releaseLeftAction.WasPressedThisFrame())
            {
                return true;
            }
#endif
            return useLegacyFallback && UnityEngine.Input.GetKeyDown(KeyCode.U);
        }

        public bool ConsumeReleaseRightPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (_releaseRightAction != null && _releaseRightAction.WasPressedThisFrame())
            {
                return true;
            }
#endif
            return useLegacyFallback && UnityEngine.Input.GetKeyDown(KeyCode.O);
        }

        public bool ConsumeReleaseAllPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (_releaseAllAction != null && _releaseAllAction.WasPressedThisFrame())
            {
                return true;
            }
#endif
            return useLegacyFallback && UnityEngine.Input.GetKeyDown(KeyCode.R);
        }

        public bool ConsumeFallPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (_fallAction != null && _fallAction.WasPressedThisFrame())
            {
                return true;
            }
#endif
            return useLegacyFallback && UnityEngine.Input.GetKeyDown(KeyCode.F);
        }

        public bool ConsumeLandPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (_landAction != null && _landAction.WasPressedThisFrame())
            {
                return true;
            }
#endif
            return useLegacyFallback && UnityEngine.Input.GetKeyDown(KeyCode.Space);
        }

        public bool ReadSprint()
        {
#if ENABLE_INPUT_SYSTEM
            if (_sprintAction != null)
            {
                return _sprintAction.IsPressed();
            }
#endif
            return useLegacyFallback && (UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift));
        }

        public bool ConsumeCameraRecenterPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (_cameraRecenterAction != null && _cameraRecenterAction.WasPressedThisFrame())
            {
                return true;
            }
#endif
            return useLegacyFallback && UnityEngine.Input.GetKeyDown(KeyCode.C);
        }

#if ENABLE_INPUT_SYSTEM
        private void EnsureActionsCreated()
        {
            if (_moveAction != null)
            {
                return;
            }

            _moveAction = new InputAction("Move", InputActionType.Value);
            AddMoveComposite(_moveAction, "<Keyboard>/z", "<Keyboard>/s", "<Keyboard>/q", "<Keyboard>/d");
            AddMoveComposite(_moveAction, "<Keyboard>/w", "<Keyboard>/s", "<Keyboard>/a", "<Keyboard>/d");
            AddMoveComposite(_moveAction, "<Keyboard>/upArrow", "<Keyboard>/downArrow", "<Keyboard>/leftArrow", "<Keyboard>/rightArrow");
            _moveAction.AddBinding("<Gamepad>/leftStick");

            _leftGrabAction = new InputAction("LeftGrab", InputActionType.Button);
            _leftGrabAction.AddBinding("<Keyboard>/j");
            _leftGrabAction.AddBinding("<Mouse>/leftButton");
            _leftGrabAction.AddBinding("<Gamepad>/leftTrigger");

            _rightGrabAction = new InputAction("RightGrab", InputActionType.Button);
            _rightGrabAction.AddBinding("<Keyboard>/e");
            _rightGrabAction.AddBinding("<Keyboard>/l");
            _rightGrabAction.AddBinding("<Mouse>/rightButton");
            _rightGrabAction.AddBinding("<Gamepad>/rightTrigger");

            _releaseLeftAction = new InputAction("ReleaseLeft", InputActionType.Button, "<Keyboard>/u");
            _releaseLeftAction.AddBinding("<Gamepad>/leftShoulder");
            _releaseRightAction = new InputAction("ReleaseRight", InputActionType.Button, "<Keyboard>/o");
            _releaseRightAction.AddBinding("<Gamepad>/rightShoulder");
            _releaseAllAction = new InputAction("ReleaseAll", InputActionType.Button, "<Keyboard>/r");
            _releaseAllAction.AddBinding("<Gamepad>/buttonNorth");
            _fallAction = new InputAction("ForceFall", InputActionType.Button, "<Keyboard>/f");
            _fallAction.AddBinding("<Gamepad>/buttonEast");
            _landAction = new InputAction("Land", InputActionType.Button, "<Keyboard>/space");
            _landAction.AddBinding("<Gamepad>/buttonSouth");

            _cameraRecenterAction = new InputAction("CameraRecenter", InputActionType.Button, "<Keyboard>/c");
            _cameraRecenterAction.AddBinding("<Gamepad>/rightStickPress");

            _sprintAction = new InputAction("Sprint", InputActionType.Button);
            _sprintAction.AddBinding("<Keyboard>/leftShift");
            _sprintAction.AddBinding("<Keyboard>/rightShift");
            _sprintAction.AddBinding("<Gamepad>/leftStickPress");
        }

        private static void AddMoveComposite(InputAction action, string up, string down, string left, string right)
        {
            action.AddCompositeBinding("2DVector")
                .With("Up", up)
                .With("Down", down)
                .With("Left", left)
                .With("Right", right);
        }
#endif
    }
}
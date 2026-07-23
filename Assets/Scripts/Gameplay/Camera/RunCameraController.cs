using UnityEngine;
using UnityEngine.InputSystem;

namespace GemTD.Gameplay.CameraControl
{
    /// <summary>
    /// Orthographic isometric run camera: WASD / MMB pan, scroll zoom, Q/E yaw snaps.
    /// Locked look: pitched board (not top-down). See GDD / UI-SPEC.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class RunCameraController : MonoBehaviour
    {
        [SerializeField] float pitchDegrees = 40f;
        [SerializeField] float yawDegrees = 45f;
        [SerializeField] float yawStepDegrees = 45f;
        [SerializeField] float distance = 22f;
        [SerializeField] float panSpeed = 40f;
        [SerializeField] float mousePanSpeed = 0.02f;
        [SerializeField] float zoomSpeed = 4f;
        [SerializeField] float minOrthoSize = 4f;
        [SerializeField] float maxOrthoSize = 10f;
        [SerializeField] Vector3 focus = new Vector3(4f, 0f, 4f);

        Camera _camera;
        InputAction _move;
        InputAction _lookDelta;
        InputAction _zoom;
        InputAction _middleButton;
        InputAction _rotateLeft;
        InputAction _rotateRight;
        InputActionMap _map;

        void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;

            _map = new InputActionMap("RunCamera");
            _move = _map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            _lookDelta = _map.AddAction("LookDelta", InputActionType.Value, "<Mouse>/delta");
            _zoom = _map.AddAction("Zoom", InputActionType.Value, "<Mouse>/scroll");
            _middleButton = _map.AddAction("Middle", InputActionType.Button, "<Mouse>/middleButton");
            _rotateLeft = _map.AddAction("RotateLeft", InputActionType.Button, "<Keyboard>/q");
            _rotateRight = _map.AddAction("RotateRight", InputActionType.Button, "<Keyboard>/e");
            _map.Enable();

            ApplyPose();
        }

        void OnDestroy()
        {
            _map?.Dispose();
        }

        void LateUpdate()
        {
            if (_camera == null) return;

            if (_rotateLeft.WasPressedThisFrame())
                yawDegrees -= yawStepDegrees;
            if (_rotateRight.WasPressedThisFrame())
                yawDegrees += yawStepDegrees;

            var yaw = Quaternion.Euler(0f, yawDegrees, 0f);
            var right = yaw * Vector3.right;
            var forward = yaw * Vector3.forward;

            var move = _move.ReadValue<Vector2>();
            if (move.sqrMagnitude > 0.0001f)
                focus += (right * move.x + forward * move.y) * (panSpeed * Time.unscaledDeltaTime);

            if (_middleButton.IsPressed())
            {
                var mouse = _lookDelta.ReadValue<Vector2>();
                focus += (-right * mouse.x - forward * mouse.y) * mousePanSpeed;
            }

            var scroll = _zoom.ReadValue<Vector2>().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _camera.orthographicSize = Mathf.Clamp(
                    _camera.orthographicSize - scroll * zoomSpeed * 0.01f,
                    minOrthoSize,
                    maxOrthoSize);
            }

            ApplyPose();
        }

        void ApplyPose()
        {
            var rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
            transform.rotation = rotation;
            transform.position = focus - rotation * Vector3.forward * distance;
            _camera.orthographic = true;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            pitchDegrees = Mathf.Clamp(pitchDegrees, 15f, 70f);
            yawStepDegrees = Mathf.Max(1f, yawStepDegrees);
            distance = Mathf.Max(1f, distance);
            minOrthoSize = Mathf.Max(0.5f, minOrthoSize);
            maxOrthoSize = Mathf.Max(minOrthoSize, maxOrthoSize);
            if (_camera == null)
                _camera = GetComponent<Camera>();
            if (_camera != null)
                ApplyPose();
        }
#endif
    }
}

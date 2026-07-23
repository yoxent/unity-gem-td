using UnityEngine;
using UnityEngine.InputSystem;
using GemTD.Gameplay.Map;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Run
{
    /// <summary>Click expand markers, place Ballista, select towers.</summary>
    public sealed class RunInputController : MonoBehaviour
    {
        [SerializeField] Camera worldCamera;
        [SerializeField] LayerMask clickMask = ~0;

        GameCompositionRoot _root;
        InputAction _click;

        public void Bind(GameCompositionRoot root)
        {
            _root = root;
            if (worldCamera == null)
                worldCamera = Camera.main;
        }

        void OnEnable()
        {
            _click = new InputAction("Click", InputActionType.Button, "<Mouse>/leftButton");
            _click.Enable();
        }

        void OnDisable()
        {
            _click?.Disable();
            _click?.Dispose();
            _click = null;
        }

        void Update()
        {
            if (_root == null || _click == null || !_click.WasPressedThisFrame())
                return;

            if (Mouse.current == null || worldCamera == null)
                return;

            var screen = Mouse.current.position.ReadValue();
            var ray = worldCamera.ScreenPointToRay(screen);

            if (Physics.Raycast(ray, out var hit, 500f, clickMask))
            {
                var marker = hit.collider.GetComponentInParent<ExpandMarkerView>();
                if (marker != null)
                {
                    _root.TryConfirmExpand(marker.Cell);
                    return;
                }

                var tower = hit.collider.GetComponentInParent<TowerView>();
                if (tower != null)
                {
                    _root.SelectTower(tower);
                    return;
                }
            }

            var plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(ray, out var enter))
                return;

            var world = ray.GetPoint(enter);
            _root.TryPlaceAtWorld(world);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using GemTD.Core;
using GemTD.Gameplay.Map;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Run
{
    /// <summary>Click expand markers, place towers, select towers, targeting hotkeys.</summary>
    public sealed class RunInputController : MonoBehaviour
    {
        [SerializeField] Camera worldCamera;
        [SerializeField] LayerMask clickMask = ~0;

        // RMB drag threshold in pixels — moves beyond this suppress the deselect-on-release.
        const float RmbDragThresholdPx = 4f;

        GameCompositionRoot _root;
        InputAction _click;
        InputAction _rightClick;
        InputAction _escape;

        bool _rmbDragging;
        Vector2 _rmbPressPos;

        readonly List<RaycastResult> _uiHits = new List<RaycastResult>(16);

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

            _rightClick = new InputAction("RightClick", InputActionType.Button, "<Mouse>/rightButton");
            _rightClick.Enable();

            _escape = new InputAction("Escape", InputActionType.Button, "<Keyboard>/escape");
            _escape.Enable();
        }

        void OnDisable()
        {
            _click?.Disable();
            _click?.Dispose();
            _click = null;

            _rightClick?.Disable();
            _rightClick?.Dispose();
            _rightClick = null;

            _escape?.Disable();
            _escape?.Dispose();
            _escape = null;
        }

        void Update()
        {
            if (_escape != null && _escape.WasPressedThisFrame())
            {
                GameEvents.RaiseRequestCloseTopMost();
                return;
            }

            if (GameSettings.IsPanelOpen)
                return;

            HandleHotkeys();

            if (_root == null)
                return;

            // RMB: track press to detect drag vs click.
            // Camera also pans on RMB hold; only fire cancel layer on release if it was a clean click.
            if (_rightClick != null)
            {
                if (_rightClick.WasPressedThisFrame())
                {
                    _rmbDragging = false;
                    _rmbPressPos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
                }

                if (_rightClick.IsPressed() && Mouse.current != null)
                {
                    var delta = Mouse.current.position.ReadValue() - _rmbPressPos;
                    if (delta.magnitude > RmbDragThresholdPx)
                        _rmbDragging = true;
                }

                if (_rightClick.WasReleasedThisFrame() && !_rmbDragging)
                {
                    if (!IsPointerOverUi())
                        HandleCancelLayer();
                }
            }

            if (_click == null || !_click.WasPressedThisFrame())
                return;

            // Inventory / build bar / Tower Details clicks must not count as empty-board.
            if (IsPointerOverUi())
                return;

            if (Mouse.current == null || worldCamera == null)
                return;

            var screen = Mouse.current.position.ReadValue();
            var ray = worldCamera.ScreenPointToRay(screen);

            if (Physics.Raycast(ray, out var hit, 500f, clickMask))
            {
                var marker = hit.collider.GetComponentInParent<ExpandMarkerView>();
                if (marker != null
                    && _root.States != null
                    && _root.States.Current == RunStateId.Plan
                    && !_root.States.ExpandSatisfiedThisCycle)
                {
                    if (_root.TryConfirmChunkExpand(marker.ChunkCoord))
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
            var kb = Keyboard.current;
            var shift = kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);

            if (_root.HasPlaceTowerSelected)
            {
                _root.TryPlaceAtWorld(world, keepPlacementSelected: shift);
                return;
            }

            // Plan-phase expand: resolve world -> chunk coord and confirm if legal.
            if (_root.States != null && _root.States.Current == RunStateId.Plan
                && !_root.States.ExpandSatisfiedThisCycle)
            {
                if (_root.TryChunkExpandAtWorld(world))
                    return;
            }

            // Empty board click — deselect tower (hides Tower Details).
            _root.ClearTowerSelection();
        }

        /// <summary>
        /// Place mode first, then tower selection. Returns true if something was cleared.
        /// </summary>
        bool HandleCancelLayer()
        {
            if (_root.HasPlaceTowerSelected)
            {
                _root.ClearPlaceTower();
                return true;
            }

            if (_root.HasSelectedTower)
            {
                _root.ClearTowerSelection();
                return true;
            }

            return false;
        }

        bool IsPointerOverUi()
        {
            var es = EventSystem.current;
            if (es == null || Mouse.current == null)
                return false;

            var eventData = new PointerEventData(es)
            {
                position = Mouse.current.position.ReadValue()
            };

            _uiHits.Clear();
            es.RaycastAll(eventData, _uiHits);
            return _uiHits.Count > 0;
        }

        void HandleHotkeys()
        {
            if (_root == null)
                return;

            var kb = Keyboard.current;
            if (kb == null)
                return;

            var speed = _root.Speed;
            var states = _root.States;

            // Speed: 1/2/3 (harmless in any state; only matters in Combat but settable anywhere).
            if (speed != null)
            {
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
                    speed.SetSpeed(1f);
                else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
                    speed.SetSpeed(2f);
                else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)
                    speed.SetSpeed(4f);
            }

            // Space: toggle pause (Plan: resume-to-Combat only if Expand satisfied; Draft: ignored; Combat: active).
            if (kb.spaceKey.wasPressedThisFrame && states != null)
            {
                var s = states.Current;
                if (s == RunStateId.Combat)
                {
                    speed?.TogglePause();
                }
                else if (s == RunStateId.Plan && states.ExpandSatisfiedThisCycle)
                {
                    speed?.TogglePause(); // resume from Plan pause
                }
                // Draft / Victory / Defeat: ignored (modal / run-end).
            }

            var ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
            var planOrCombat = states != null &&
                (states.Current == RunStateId.Plan || states.Current == RunStateId.Combat);

            if (planOrCombat && _root.HasSelectedTower)
            {
                if (ctrl && kb.cKey.wasPressedThisFrame)
                    _root.CopySelectedTargeting();
                else if (ctrl && kb.vKey.wasPressedThisFrame)
                    _root.PasteSelectedTargeting();
                // R / Shift+R (cycle priority / apply-scope) are intentionally disabled
                // until the Tower Details targeting UI is re-enabled.
            }

            // C: Codex — skip when Ctrl held (copy). Plan + Combat only.
            if (!ctrl && kb.cKey.wasPressedThisFrame && states != null)
            {
                var s = states.Current;
                if (s == RunStateId.Plan || s == RunStateId.Combat)
                    _root.ToggleCodexPanel();
            }
        }
    }
}

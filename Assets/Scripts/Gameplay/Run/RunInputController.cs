using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using GemTD.Gameplay.Map;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Run
{
    /// <summary>Click expand markers, place towers, select towers, targeting hotkeys.</summary>
    public sealed class RunInputController : MonoBehaviour
    {
        [SerializeField] Camera worldCamera;
        [SerializeField] LayerMask clickMask = ~0;

        GameCompositionRoot _root;
        InputAction _click;
        InputAction _rightClick;
        InputAction _escape;
        InputAction _cycleAim;
        InputAction _cycleScope;

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

            // R alone — cycle aim mode (no modifiers).
            _cycleAim = new InputAction("CycleAim", InputActionType.Button);
            _cycleAim.AddBinding("<Keyboard>/r");
            _cycleAim.Enable();

            // Shift+R only — cycle apply scope (explicit modifier; not Ctrl).
            _cycleScope = new InputAction("CycleScope", InputActionType.Button);
            _cycleScope.AddCompositeBinding("OneModifier")
                .With("modifier", "<Keyboard>/shift")
                .With("binding", "<Keyboard>/r");
            _cycleScope.Enable();
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

            _cycleAim?.Disable();
            _cycleAim?.Dispose();
            _cycleAim = null;

            _cycleScope?.Disable();
            _cycleScope?.Dispose();
            _cycleScope = null;
        }

        void Update()
        {
            HandleHotkeys();

            if (_root == null)
                return;

            // Esc: cancel place → else deselect tower (BTD-style).
            if (_escape != null && _escape.WasPressedThisFrame())
            {
                if (HandleCancelLayer())
                    return;
            }

            // RMB (GDD): cancel place → else deselect tower / close Tower Details.
            // Ignore when over UI so Tower Details / inventory right-clicks don't steal focus.
            if (_rightClick != null && _rightClick.WasPressedThisFrame())
            {
                if (IsPointerOverUi())
                    return;

                if (HandleCancelLayer())
                    return;
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
            var kb = Keyboard.current;
            var shift = kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);

            if (_root.HasPlaceTowerSelected)
            {
                _root.TryPlaceAtWorld(world, keepPlacementSelected: shift);
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

            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
                _root.SetPlaceTower(0);
            else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
                _root.SetPlaceTower(1);
            else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)
                _root.SetPlaceTower(2);

            // Draft greybox: Z/X/C pick cards, V skip, B discard slot 0 (Plan), N/M replace Yes/No, comma complete replace slot 0.
            if (_root.States != null && _root.States.Current == RunStateId.Draft)
            {
                if (kb.zKey.wasPressedThisFrame)
                    _root.RequestDraftPick(0);
                else if (kb.xKey.wasPressedThisFrame)
                    _root.RequestDraftPick(1);
                else if (kb.cKey.wasPressedThisFrame)
                    _root.RequestDraftPick(2);
                else if (kb.vKey.wasPressedThisFrame)
                    _root.RequestDraftSkip();
                else if (kb.nKey.wasPressedThisFrame)
                    _root.RequestDraftReplaceYes();
                else if (kb.mKey.wasPressedThisFrame)
                    _root.RequestDraftReplaceNo();
                else if (kb.commaKey.wasPressedThisFrame)
                    _root.RequestDraftReplaceComplete(0);
                return;
            }

            if (_root.States != null && _root.States.Current == RunStateId.Plan && kb.bKey.wasPressedThisFrame)
                _root.RequestDiscardAt(0);

            if (kb.cKey.wasPressedThisFrame)
                _root.ToggleCodexPanel();

            // Scope first: Shift+R composite steals the chord so plain R does not also fire aim.
            if (_cycleScope != null && _cycleScope.WasPressedThisFrame())
            {
                _root.CycleTargetingScope();
                return;
            }

            if (_cycleAim != null && _cycleAim.WasPressedThisFrame())
            {
                // Ignore when Ctrl/Alt held (OS/editor chords); Scope is Shift-only above.
                if (kb.ctrlKey.isPressed || kb.altKey.isPressed)
                    return;
                _root.CycleTargetingMode();
            }
        }
    }
}

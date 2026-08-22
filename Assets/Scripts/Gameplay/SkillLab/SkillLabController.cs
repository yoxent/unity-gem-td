using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using GemTD.Core;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.SkillLab
{
    public sealed class SkillLabController : MonoBehaviour
    {
        [SerializeField] TowerDefinition ballista;
        [SerializeField] TowerDefinition cannon;
        [SerializeField] GemDefinition[] draftGems;
        [SerializeField] EnemyDefinition dummyDefinition;
        [SerializeField] AttackOverlayView overlay;
        [SerializeField] Camera worldCamera;
        [SerializeField] Transform towerView;
        [SerializeField] SkillLabDummyView[] dummyViews;

        readonly SkillLabSession _session = new SkillLabSession();
        InputAction _escape;
        bool _draggingTower;
        int _draggingDummy = -1;

        public SkillLabSession Session => _session;

        void Awake()
        {
            if (ballista == null) Debug.LogError("SkillLabController: ballista is not assigned.", this);
            if (cannon == null) Debug.LogError("SkillLabController: cannon is not assigned.", this);
            if (draftGems == null || draftGems.Length == 0) Debug.LogError("SkillLabController: draftGems is not assigned.", this);
            if (dummyDefinition == null) Debug.LogError("SkillLabController: dummyDefinition is not assigned.", this);
            if (overlay == null) Debug.LogError("SkillLabController: overlay is not assigned.", this);
            if (worldCamera == null) Debug.LogError("SkillLabController: worldCamera is not assigned.", this);
            if (towerView == null) Debug.LogError("SkillLabController: towerView is not assigned.", this);
            if (dummyViews == null || dummyViews.Length < DummyField.PinCount)
                Debug.LogError("SkillLabController: dummyViews must have 10 entries.", this);

            _session.BindCatalog(draftGems);
            if (ballista != null)
                _session.SetTowerDef(ballista);
            if (dummyDefinition != null)
                _session.Dummies.Init(dummyDefinition);
            _session.TowerPosition = DummyField.DefaultTowerPosition;
        }

        void OnEnable()
        {
            _escape = new InputAction("Escape", InputActionType.Button, "<Keyboard>/escape");
            _escape.Enable();
        }

        void OnDisable()
        {
            _escape?.Disable();
            _escape?.Dispose();
            _escape = null;
        }

        void Update()
        {
            if (_escape != null && _escape.WasPressedThisFrame())
            {
                BackToMenu();
                return;
            }

            TickDrag();
        }

        void LateUpdate()
        {
            if (towerView != null)
                towerView.position = _session.TowerPosition;

            if (dummyViews != null)
            {
                for (var i = 0; i < dummyViews.Length; i++)
                {
                    var view = dummyViews[i];
                    var dummy = _session.Dummies.GetDummy(i);
                    if (view == null || dummy == null)
                        continue;
                    view.transform.position = dummy.WorldPosition;
                }
            }

            if (overlay != null)
            {
                overlay.SetTrace(_session.LastTrace);
                overlay.SetRangeRing(_session.TowerPosition, _session.Range);
            }
        }

        public void SelectBallista()
        {
            if (ballista != null)
                _session.SetTowerDef(ballista);
        }

        public void SelectCannon()
        {
            if (cannon != null)
                _session.SetTowerDef(cannon);
        }

        public void SetSocket(int index, GemId id)
        {
            _session.SetSocket(index, id);
        }

        public void Fire()
        {
            _session.Fire();
        }

        public void ClearOverlay()
        {
            _session.ClearOverlay();
        }

        public void ResetPins()
        {
            _session.ResetPins();
        }

        public void BackToMenu()
        {
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        void TickDrag()
        {
            if (Mouse.current == null)
                return;

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (SkillLabWorldDrag.IsPointerOverUi())
                    return;
                if (SkillLabWorldDrag.TryPick(worldCamera, towerView, dummyViews, out var tower, out var dummyIndex))
                {
                    _draggingTower = tower;
                    _draggingDummy = dummyIndex;
                }
            }

            if (Mouse.current.leftButton.isPressed && (_draggingTower || _draggingDummy >= 0))
            {
                if (!SkillLabWorldDrag.TryGetGroundPoint(worldCamera, out var point))
                    return;
                if (_draggingTower)
                    _session.TowerPosition = point;
                else
                {
                    var dummy = _session.Dummies.GetDummy(_draggingDummy);
                    dummy?.SetWorldPosition(point);
                }
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                _draggingTower = false;
                _draggingDummy = -1;
            }
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using GemTD.Core;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.SkillLab
{
    public sealed class SkillLabController : MonoBehaviour
    {
        const string DefaultTowerName = "Fireball";

        [Tooltip("Gameplay-ready pool (same asset as run draft). Picker still omits aura-only entries.")]
        [SerializeField] TowerCatalog towerCatalog;
        [SerializeField] GemDefinition[] draftGems;
        [SerializeField] EnemyDefinition dummyDefinition;
        [SerializeField] AttackOverlayView overlay;
        [SerializeField] Camera worldCamera;
        [SerializeField] Transform towerView;
        [SerializeField] TowerView towerPrefab;
        [SerializeField] TowerView spellTowerPrefab;
        [SerializeField] TowerView slamTowerPrefab;
        [SerializeField] TowerView strikeTowerPrefab;
        [SerializeField] TowerView bowTowerPrefab;
        [SerializeField] TowerView attackTowerPrefab;
        [SerializeField] TowerView auraTowerPrefab;
        [SerializeField] TowerView curseTowerPrefab;
        [SerializeField] SkillLabDummyView[] dummyViews;
        [SerializeField] EffectView projectilePrefab;
        [SerializeField] EffectView slamEffectPrefab;
        [SerializeField] EffectView aftershockEffectPrefab;
        [SerializeField] EffectView fallEffectPrefab;

        readonly SkillLabSession _session = new SkillLabSession();
        readonly List<EffectView> _effectViews = new List<EffectView>(32);
        ViewObjectPool<EffectView> _projectilePool;
        ViewObjectPool<EffectView> _slamEffectPool;
        ViewObjectPool<EffectView> _aftershockEffectPool;
        ViewObjectPool<EffectView> _fallEffectPool;
        InputAction _escape;
        bool _draggingTower;
        int _draggingDummy = -1;
        TowerView _liveView;
        TowerView _boundPrefab;

        public SkillLabSession Session => _session;

        void Awake()
        {
            if (towerCatalog == null) Debug.LogError("SkillLabController: towerCatalog is not assigned.", this);
            if (draftGems == null || draftGems.Length == 0) Debug.LogError("SkillLabController: draftGems is not assigned.", this);
            if (dummyDefinition == null) Debug.LogError("SkillLabController: dummyDefinition is not assigned.", this);
            if (overlay == null) Debug.LogError("SkillLabController: overlay is not assigned.", this);
            if (worldCamera == null) Debug.LogError("SkillLabController: worldCamera is not assigned.", this);
            if (towerView == null) Debug.LogError("SkillLabController: towerView is not assigned.", this);
            if (dummyViews == null || dummyViews.Length < DummyField.PinCount)
                Debug.LogError("SkillLabController: dummyViews must have 10 entries.", this);
            if (projectilePrefab == null)
                Debug.LogError("SkillLabController: projectilePrefab is not assigned.", this);
            else
            {
                _projectilePool = new ViewObjectPool<EffectView>(
                    projectilePrefab,
                    transform,
                    EffectViewBinder.BoltPrewarm);
                _projectilePool.Prewarm(EffectViewBinder.BoltPrewarm);
            }
            if (slamEffectPrefab != null)
            {
                _slamEffectPool = new ViewObjectPool<EffectView>(
                    slamEffectPrefab,
                    transform,
                    EffectViewBinder.SlamPrewarm);
                _slamEffectPool.Prewarm(EffectViewBinder.SlamPrewarm);
            }
            if (aftershockEffectPrefab != null)
            {
                _aftershockEffectPool = new ViewObjectPool<EffectView>(
                    aftershockEffectPrefab,
                    transform,
                    EffectViewBinder.AftershockPrewarm);
                _aftershockEffectPool.Prewarm(EffectViewBinder.AftershockPrewarm);
            }
            if (fallEffectPrefab != null)
            {
                _fallEffectPool = new ViewObjectPool<EffectView>(
                    fallEffectPrefab,
                    transform,
                    EffectViewBinder.FallPrewarm);
                _fallEffectPool.Prewarm(EffectViewBinder.FallPrewarm);
            }

            _session.BindCatalog(draftGems);
            if (towerCatalog != null)
            {
                _session.BindTowers(towerCatalog.GetTowersOrEmpty());
                var index = _session.IndexOfDisplayName(DefaultTowerName);
                if (index < 0 && _session.Towers.Length > 0)
                    index = 0;
                if (index >= 0)
                    _session.SelectTower(index);
            }
            if (dummyDefinition != null)
                _session.Dummies.Init(dummyDefinition);
            _session.TowerPosition = DummyField.DefaultTowerPosition;
            ApplyTowerView();
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
            _session.ClearOverlay();
            SyncEffectViews();
        }

        void Update()
        {
            if (_escape != null && _escape.WasPressedThisFrame())
            {
                BackToMenu();
                return;
            }

            TickDrag();
            _session.TickVolley(Time.deltaTime);
            if (_liveView != null)
            {
                _liveView.TickAnimator(Time.deltaTime, 1f);
                _session.ResolveQueuedAnimationActions();
            }
            FlashHitsFromDamage();
        }

        void LateUpdate()
        {
            if (_liveView != null)
                _liveView.PlaceOnPad(_session.TowerPosition);
            else if (towerView != null)
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

            SyncEffectViews();
        }

        public void SelectTower(int index)
        {
            _session.SelectTower(index);
            ApplyTowerView();
        }

        public void SetSocket(int index, GemId id)
        {
            _session.SetSocket(index, id);
        }

        public void Fire()
        {
            ClearHitFlashes();
            _session.Fire();
            FlashHitsFromDamage();
            FlashHitsFromHex();
        }

        public void ClearOverlay()
        {
            _session.ClearOverlay();
            ClearHitFlashes();
        }

        public void ResetPins()
        {
            _session.ResetPins();
            ClearHitFlashes();
        }

        public void BackToMenu()
        {
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        Transform PickRoot =>
            _liveView != null ? _liveView.transform : towerView;

        TowerView ResolveTowerViewPrefab()
        {
            return TowerViewPrefabResolver.Resolve(
                _session.Tower != null ? _session.Tower.Def : null,
                towerPrefab,
                auraTowerPrefab,
                curseTowerPrefab,
                slamTowerPrefab,
                strikeTowerPrefab,
                bowTowerPrefab,
                attackTowerPrefab,
                spellTowerPrefab);
        }

        void ApplyTowerView()
        {
            var prefab = ResolveTowerViewPrefab();
            if (prefab == null)
                return;

            if (_liveView == null || _boundPrefab != prefab)
            {
                if (_liveView != null)
                    Destroy(_liveView.gameObject);
                _liveView = Instantiate(prefab, transform);
                _boundPrefab = prefab;
                if (towerView != null)
                    towerView.gameObject.SetActive(false);
            }

            _liveView.SetCombatActionHandler(_session.QueueAnimationAction);
            _liveView.Bind(_session.Tower, _session.TowerPosition);
        }

        void FlashHitsFromDamage()
        {
            if (dummyViews == null)
                return;

            for (var i = 0; i < DummyField.PinCount; i++)
            {
                if (i >= dummyViews.Length || dummyViews[i] == null)
                    continue;
                var dummy = _session.Dummies.GetDummy(i);
                if (dummy == null || dummy.LastDamageSource == null)
                    continue;

                dummyViews[i].FlashHit();
                dummy.LastDamageSource = null;
            }
        }

        void FlashHitsFromHex()
        {
            if (dummyViews == null || _session.Statuses == null)
                return;

            for (var i = 0; i < DummyField.PinCount; i++)
            {
                if (i >= dummyViews.Length || dummyViews[i] == null)
                    continue;
                var dummy = _session.Dummies.GetDummy(i);
                if (dummy == null || !_session.Statuses.HasAnyCurse(dummy))
                    continue;

                dummyViews[i].FlashHit();
            }
        }

        void SyncEffectViews()
        {
            EffectViewBinder.SyncLive(
                _effectViews,
                _session.Projectiles,
                _session.EffectPayloads,
                _projectilePool,
                _slamEffectPool,
                _aftershockEffectPool,
                _fallEffectPool);
        }

        void TickDrag()
        {
            if (Mouse.current == null)
                return;

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (SkillLabWorldDrag.IsPointerOverUi())
                    return;
                if (SkillLabWorldDrag.TryPick(worldCamera, PickRoot, dummyViews, out var tower, out var dummyIndex))
                {
                    _session.StopVolley();
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

        void ClearHitFlashes()
        {
            if (dummyViews == null)
                return;

            for (var i = 0; i < dummyViews.Length; i++)
            {
                if (dummyViews[i] != null)
                    dummyViews[i].ClearHitFlash();
            }
        }
    }
}

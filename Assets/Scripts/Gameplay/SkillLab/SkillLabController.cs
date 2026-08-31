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

        [SerializeField] TowerCatalog towerCatalog;
        [SerializeField] GemDefinition[] draftGems;
        [SerializeField] EnemyDefinition dummyDefinition;
        [SerializeField] AttackOverlayView overlay;
        [SerializeField] Camera worldCamera;
        [SerializeField] Transform towerView;
        [SerializeField] SkillLabDummyView[] dummyViews;
        [SerializeField] ProjectileView projectilePrefab;
        [SerializeField] ProjectileView slamEffectPrefab;
        [SerializeField] ProjectileView aftershockEffectPrefab;

        readonly SkillLabSession _session = new SkillLabSession();
        readonly List<ProjectileView> _projectileViews = new List<ProjectileView>(32);
        ViewObjectPool<ProjectileView> _projectilePool;
        ViewObjectPool<ProjectileView> _slamEffectPool;
        ViewObjectPool<ProjectileView> _aftershockEffectPool;
        InputAction _escape;
        bool _draggingTower;
        int _draggingDummy = -1;

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
                _projectilePool = new ViewObjectPool<ProjectileView>(
                    projectilePrefab,
                    transform,
                    ProjectileViewBinder.BoltPrewarm);
                _projectilePool.Prewarm(ProjectileViewBinder.BoltPrewarm);
            }
            if (slamEffectPrefab != null)
            {
                _slamEffectPool = new ViewObjectPool<ProjectileView>(
                    slamEffectPrefab,
                    transform,
                    ProjectileViewBinder.SlamPrewarm);
                _slamEffectPool.Prewarm(ProjectileViewBinder.SlamPrewarm);
            }
            if (aftershockEffectPrefab != null)
            {
                _aftershockEffectPool = new ViewObjectPool<ProjectileView>(
                    aftershockEffectPrefab,
                    transform,
                    ProjectileViewBinder.AftershockPrewarm);
                _aftershockEffectPool.Prewarm(ProjectileViewBinder.AftershockPrewarm);
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
            SyncProjectileViews();
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
            FlashHitsFromDamage();
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

            SyncProjectileViews();
        }

        public void SelectTower(int index)
        {
            _session.SelectTower(index);
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

        void SyncProjectileViews()
        {
            ProjectileViewBinder.SyncLive(
                _projectileViews,
                _session.Projectiles,
                _session.EffectPayloads,
                _projectilePool,
                _slamEffectPool,
                _aftershockEffectPool);
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

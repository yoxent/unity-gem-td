using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using GemTD.Core;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Grid;
using GemTD.Gameplay.Map;
using GemTD.Gameplay.Run;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay
{
    /// <summary>
    /// Scene composition root. Owns service lifetimes for a Run.
    /// </summary>
    public sealed class GameCompositionRoot : MonoBehaviour
    {
        public static GameCompositionRoot Instance { get; private set; }

        [Header("Data")]
        [SerializeField] RunConfig runConfig;
        [SerializeField] TowerDefinition ballistaDef;
        [SerializeField] TowerDefinition cannonDef;
        [SerializeField] TowerDefinition beaconDef;
        [SerializeField] WaveDefinition[] waves;
        [SerializeField] GemDefinition[] draftPool;

        [Header("Scene")]
        [SerializeField] GridBoardView gridView;
        [SerializeField] RunInputController inputController;
        [SerializeField] Transform poolRoot;

        [Header("Prefabs")]
        [SerializeField] EnemyView enemyPrefab;
        [SerializeField] ProjectileView projectilePrefab;
        [SerializeField] TowerView towerPrefab;
        [SerializeField] ExpandMarkerView expandMarkerPrefab;

        [Header("Tuning")]
        [SerializeField] float projectileSpeed = 20f;

        public RunClock Clock { get; private set; }
        public RunStateMachine States { get; private set; }
        public RunEconomy Economy { get; private set; }
        public GemInventory Inventory { get; private set; }
        public TowerPlacementService Placement { get; private set; }
        public WaveController WaveController { get; private set; }
        public DraftService Draft { get; private set; }
        public int CurrentWaveNumber => WaveController != null ? WaveController.CurrentWaveNumber : 0;
        public bool HasSelectedTower => Placement != null && Placement.Selected != null;
        public bool SelectedHasSocketedGems =>
            Placement?.Selected != null && Placement.Selected.HasSocketedGems;
        public bool CanStartWave =>
            States != null
            && States.Current == RunStateId.Plan
            && States.ExpandSatisfiedThisCycle
            && WaveController != null;
        public string PlaceTowerName =>
            _placeDef != null && !string.IsNullOrEmpty(_placeDef.DisplayName)
                ? _placeDef.DisplayName
                : (_placeDef != null ? _placeDef.name : "None");
        public TargetingMode SelectedTargetingMode =>
            HasSelectedTower ? Placement.Selected.TargetingMode : TargetingMode.First;
        public TargetingApplyScope CurrentApplyScope => _applyScope;

        TowerDefinition _placeDef;
        TargetingApplyScope _applyScope = TargetingApplyScope.ThisTower;

        GridBoard _board;
        PathGraph _path;
        MapExpandService _expand;
        EnemyRegistry _registry;
        CombatDirector _combat;
        BeaconAuraSystem _beaconAura;
        GemModifierPipeline _pipeline;
        EnemySpawnerGate _spawnerGate;

        readonly List<TowerRuntime> _towers = new List<TowerRuntime>(16);
        readonly List<TowerView> _towerViews = new List<TowerView>(16);
        readonly List<EnemyView> _enemyViews = new List<EnemyView>(32);
        readonly List<ProjectileView> _projectileViews = new List<ProjectileView>(32);
        readonly List<ExpandMarkerView> _markers = new List<ExpandMarkerView>(16);
        readonly List<Vector2Int> _legalExpands = new List<Vector2Int>(16);
        readonly List<Vector2Int> _polylineCells = new List<Vector2Int>(16);
        readonly List<Vector3> _polylineWorld = new List<Vector3>(16);
        readonly List<Vector2Int> _spawnTips = new List<Vector2Int>(8);

        ViewObjectPool<EnemyView> _enemyPool;
        ViewObjectPool<ProjectileView> _projectilePool;

        InputAction _debugAdvance;
        InputActionMap _debugMap;
        bool _loggedExpandSkip;
        int _nextTipIndex;
        HomeBaseView _homeMarker;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            GameEvents.ClearAll();

            _placeDef = ballistaDef;

            Clock = new RunClock();
            States = new RunStateMachine(Clock);
            States.StateChanged += OnStateChanged;

            BootstrapServices();
            SetupPools();

            if (inputController != null)
                inputController.Bind(this);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _debugMap = new InputActionMap("RunDebug");
            _debugAdvance = _debugMap.AddAction("AdvancePhase", InputActionType.Button);
            _debugAdvance.AddBinding("<Keyboard>/f5");
            _debugMap.Enable();
#endif
        }

        void Start()
        {
            States.StartRun();
            BeginDraftOffer(allowSkip: false);
        }

        void OnDestroy()
        {
            if (States != null)
                States.StateChanged -= OnStateChanged;

            if (Instance == this)
                Instance = null;

            GameEvents.ClearAll();
            _debugMap?.Dispose();
            _enemyPool?.Clear();
            _projectilePool?.Clear();
        }

        void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            TryDebugAdvance();
#endif
            if (States == null
                || States.Current == RunStateId.Defeat
                || States.Current == RunStateId.VictorySummary)
                return;

            var dt = Clock.DeltaTime;
            if (dt > 0f)
            {
                WaveController?.Tick(dt, _spawnerGate);
                TickEnemies(dt);

                if (States.Current == RunStateId.Combat)
                {
                    var cellSize = gridView != null ? gridView.CellSize : 1f;
                    _beaconAura?.Tick(_towers, _registry, cellSize);
                    _combat?.Tick(dt, _towers, _registry, _pipeline);
                }
            }

            SyncProjectileViews();
            SyncEnemyViews();
        }

        void BootstrapServices()
        {
            var gold = runConfig != null ? runConfig.StartingGold : 100;
            var lives = runConfig != null ? runConfig.StartingLives : 20;
            var endWaveGold = runConfig != null ? runConfig.EndWaveGold : 25;

            _board = new GridBoard(8, 8);
            _path = new PathGraph(8, 8);
            _path.BindBoard(_board);
            _path.SetHome(0, 3);
            for (var x = 0; x <= 7; x++)
                _path.SetPathTile(x, 3, true);

            if (gridView != null)
            {
                gridView.Bind(_board, _path);
            }

            EnsureHomeMarker();

            Economy = new RunEconomy(gold, lives);
            GameEvents.RaiseGoldChanged(Economy.Gold);
            GameEvents.RaiseLivesChanged(Economy.Lives);

            var capacity = runConfig != null && runConfig.InventoryCapacity > 0
                ? runConfig.InventoryCapacity
                : 10;
            Inventory = new GemInventory(capacity);
            // Starter draft supplies the first gem — do not seed LMP+Chain.

            Draft = new DraftService(new System.Random());

            _expand = new MapExpandService(_path, _board);
            Placement = new TowerPlacementService(_board, _path, _expand, Economy);
            _registry = new EnemyRegistry();
            var cellSize = gridView != null ? gridView.CellSize : 1f;
            _combat = new CombatDirector(cellSize, projectileSpeed);
            _beaconAura = new BeaconAuraSystem();
            _pipeline = new GemModifierPipeline();

            _spawnerGate = new EnemySpawnerGate(SpawnEnemy, () => CountLivingEnemies());

            var waveDefs = waves != null && waves.Length > 0
                ? waves
                : System.Array.Empty<WaveDefinition>();
            if (waveDefs.Length == 0)
                Debug.LogError("[GemTD] No wave definitions assigned on GameCompositionRoot.");
            else
                WaveController = new WaveController(waveDefs, States, Economy, endWaveGold);
        }

        void SetupPools()
        {
            var parent = poolRoot != null ? poolRoot : transform;
            if (enemyPrefab != null)
                _enemyPool = new ViewObjectPool<EnemyView>(enemyPrefab, parent);
            if (projectilePrefab != null)
                _projectilePool = new ViewObjectPool<ProjectileView>(projectilePrefab, parent);
        }

        void OnStateChanged(RunStateId prev, RunStateId next)
        {
            if (IsCombatPhase(prev) && !IsCombatPhase(next))
            {
                _combat?.ClearProjectiles();
                SyncProjectileViews();
            }

            if (next == RunStateId.Plan)
            {
                _loggedExpandSkip = false;
                if (!States.ExpandSatisfiedThisCycle)
                    RefreshExpandMarkers();
                else
                    ClearExpandMarkers();
            }
            else if (next == RunStateId.Draft)
            {
                ClearExpandMarkers();
                // Mid-run drafts (starter already began in Start).
                if (prev == RunStateId.Combat)
                    BeginDraftOffer(allowSkip: true);
            }
            else
            {
                ClearExpandMarkers();
            }
        }

        static bool IsCombatPhase(RunStateId state) =>
            state == RunStateId.Combat || state == RunStateId.Boss || state == RunStateId.Endless;

        void BeginDraftOffer(bool allowSkip)
        {
            if (Draft == null)
                return;

            var usable = 0;
            if (draftPool != null)
            {
                for (var i = 0; i < draftPool.Length; i++)
                {
                    if (draftPool[i] != null)
                        usable++;
                }
            }

            if (usable < 3)
            {
                Debug.LogError(
                    "[GemTD] draftPool needs at least 3 assigned gems on GameCompositionRoot " +
                    $"(found {usable}). Run menu: Gem TD / Phase 2 PR4 Wire Draft Pool + Waves");
                return;
            }

            Draft.BeginOffer(draftPool, allowSkip);
            Debug.Log(
                $"[GemTD] Draft offer ({(allowSkip ? "skip OK" : "must pick")}): " +
                $"{Draft.CurrentOffer[0].DisplayName} / {Draft.CurrentOffer[1].DisplayName} / {Draft.CurrentOffer[2].DisplayName}");
        }

        void RefreshExpandMarkers()
        {
            ClearExpandMarkers();
            if (_expand == null || expandMarkerPrefab == null || gridView == null)
                return;

            if (States.Current != RunStateId.Plan || States.ExpandSatisfiedThisCycle)
                return;

            var count = _expand.CollectLegalExpands(_legalExpands);
            if (count == 0)
            {
                if (!_loggedExpandSkip)
                {
                    Debug.Log("[GemTD] No legal expands — waive expand requirement.");
                    _loggedExpandSkip = true;
                }
                States.WaiveExpandRequirement();
                return;
            }

            for (var i = 0; i < _legalExpands.Count; i++)
            {
                var cell = _legalExpands[i];
                var marker = Instantiate(expandMarkerPrefab, transform);
                marker.Bind(cell, gridView.CellToWorld(cell));
                _markers.Add(marker);
            }
        }

        void ClearExpandMarkers()
        {
            for (var i = 0; i < _markers.Count; i++)
            {
                if (_markers[i] != null)
                    Destroy(_markers[i].gameObject);
            }
            _markers.Clear();
        }

        public bool TryConfirmExpand(Vector2Int cell)
        {
            if (States.Current != RunStateId.Plan || States.ExpandSatisfiedThisCycle)
                return false;

            if (!_expand.TryExpand(cell))
            {
                Debug.Log($"[GemTD] Expand rejected at {cell}");
                return false;
            }

            gridView?.SetPath(_path);
            ClearExpandMarkers();
            States.NotifyExpandDone();
            return true;
        }

        public void TryPlaceAtWorld(Vector3 world)
        {
            if (gridView == null || _placeDef == null || Placement == null)
                return;

            var phase = States.Current;
            if (phase != RunStateId.Plan && phase != RunStateId.Combat)
                return;

            var cell = gridView.WorldToCell(world);
            if (!Placement.TryPlace(_placeDef, cell, phase, out var tower))
            {
                Debug.Log($"[GemTD] Place rejected at {cell} (phase={phase}, gold={Economy.Gold})");
                return;
            }

            _towers.Add(tower);
            if (towerPrefab != null)
            {
                var view = Instantiate(towerPrefab, transform);
                view.Bind(tower, gridView.CellToWorld(cell));
                _towerViews.Add(view);
            }
        }

        public void SetPlaceTower(int index)
        {
            switch (index)
            {
                case 0:
                    if (ballistaDef != null)
                        _placeDef = ballistaDef;
                    break;
                case 1:
                    if (cannonDef != null)
                        _placeDef = cannonDef;
                    break;
                case 2:
                    if (beaconDef != null)
                        _placeDef = beaconDef;
                    break;
            }
        }

        public void CycleTargetingMode()
        {
            if (!HasSelectedTower)
                return;

            var selected = Placement.Selected;
            var next = (TargetingMode)(((int)selected.TargetingMode + 1) % 4);
            TargetingService.Apply(next, _applyScope, selected, _towers);
        }

        public void CycleTargetingScope()
        {
            _applyScope = (TargetingApplyScope)(((int)_applyScope + 1) % 3);
        }

        public void SelectTower(TowerView view)
        {
            if (view == null || view.Runtime == null)
                return;

            Placement.Selected = view.Runtime;
            for (var i = 0; i < _towerViews.Count; i++)
            {
                var tv = _towerViews[i];
                if (tv != null)
                    tv.SetSelected(tv == view);
            }
        }

        public void RequestStartWave()
        {
            if (!CanStartWave)
                return;

            WaveController.StartWave();
            GameEvents.RaiseWaveChanged(WaveController.CurrentWaveNumber);
        }

        public void RequestSellSelected()
        {
            var selected = Placement?.Selected;
            if (selected == null)
                return;

            if (!Placement.TrySell(selected, States.Current, Inventory))
            {
                Debug.Log($"[GemTD] Sell rejected (phase={States.Current})");
                return;
            }

            for (var i = _towerViews.Count - 1; i >= 0; i--)
            {
                if (_towerViews[i] == null || _towerViews[i].Runtime != selected)
                    continue;
                Destroy(_towerViews[i].gameObject);
                _towerViews.RemoveAt(i);
            }

            _towers.Remove(selected);
            ClearSelectionHighlight();
        }

        public void RequestSocket(GemId id)
        {
            if (States.Current != RunStateId.Plan && States.Current != RunStateId.Combat)
            {
                Debug.Log("[GemTD] Socket frozen outside Plan/Combat.");
                return;
            }

            var tower = Placement?.Selected;
            if (tower == null)
                return;

            if (!Inventory.TryTake(id, out var gem))
                return;

            var socketed = false;
            for (var i = 0; i < tower.Sockets.Length; i++)
            {
                if (tower.TrySocket(gem, i, allowSocket: true))
                {
                    socketed = true;
                    break;
                }
            }

            if (!socketed)
                Inventory.TryAdd(gem);
        }

        /// <summary>Socket the gem in a specific bag slot onto the selected tower.</summary>
        public void RequestSocketFromInventory(int inventoryIndex)
        {
            if (States.Current != RunStateId.Plan && States.Current != RunStateId.Combat)
            {
                Debug.Log("[GemTD] Socket frozen outside Plan/Combat.");
                return;
            }

            var tower = Placement?.Selected;
            if (tower == null)
            {
                Debug.Log("[GemTD] Select a tower before socketing from inventory.");
                return;
            }

            if (!Inventory.TryTakeAt(inventoryIndex, out var gem))
                return;

            var socketed = false;
            for (var i = 0; i < tower.Sockets.Length; i++)
            {
                if (tower.TrySocket(gem, i, allowSocket: true))
                {
                    socketed = true;
                    break;
                }
            }

            if (!socketed)
            {
                Inventory.TryAdd(gem);
                Debug.Log($"[GemTD] Could not socket {gem.DisplayName} (full sockets or duplicate GemId).");
            }
        }

        public void RequestUnsocket(int socketIndex)
        {
            if (States.Current != RunStateId.Plan && States.Current != RunStateId.Combat)
                return;

            var tower = Placement?.Selected;
            if (tower == null || Inventory == null)
                return;

            if (Inventory.FreeSlotCount <= 0)
            {
                Debug.Log("[GemTD] Unsocket blocked — inventory full (discard first).");
                return;
            }

            if (!tower.TryUnsocket(socketIndex, out var gem, allowSocket: true))
                return;

            if (!Inventory.TryAdd(gem))
                tower.TrySocket(gem, socketIndex, allowSocket: true);
        }

        public void RequestDiscardAt(int inventoryIndex)
        {
            if (States.Current != RunStateId.Plan || Inventory == null)
            {
                Debug.Log("[GemTD] Discard only allowed in Plan.");
                return;
            }

            if (!Inventory.TryDiscardAt(inventoryIndex, out var discarded))
                return;

            Debug.Log($"[GemTD] Discarded {discarded.DisplayName} from inventory slot {inventoryIndex}.");
        }

        /// <summary>
        /// Inventory slot click: draft-replace complete, else socket onto selected tower,
        /// or discard when Shift held in Plan.
        /// </summary>
        public void RequestInventorySlotClick(int inventoryIndex, bool shiftDiscard)
        {
            if (States == null || Inventory == null)
                return;

            if (States.Current == RunStateId.Draft
                && Draft != null
                && Draft.ReplacePhase == DraftReplacePhase.AwaitingInventoryPick)
            {
                RequestDraftReplaceComplete(inventoryIndex);
                return;
            }

            if (shiftDiscard && States.Current == RunStateId.Plan)
            {
                RequestDiscardAt(inventoryIndex);
                return;
            }

            RequestSocketFromInventory(inventoryIndex);
        }

        public void RequestDraftPick(int offerIndex)
        {
            if (States.Current != RunStateId.Draft || Draft == null || !Draft.IsActive)
                return;

            if (!Draft.TryPick(offerIndex, Inventory, out var resolved))
                return;

            if (resolved)
            {
                States.DraftResolved();
                return;
            }

            if (Draft.ReplacePhase == DraftReplacePhase.AwaitingConfirm)
                Debug.Log("[GemTD] Bag full — ConfirmReplaceYes/No, then pick inventory slot.");
        }

        public void RequestDraftSkip()
        {
            if (States.Current != RunStateId.Draft || Draft == null || !Draft.IsActive)
                return;

            if (!Draft.TrySkip(Economy, 75, out var resolved) || !resolved)
                return;

            States.DraftResolved();
        }

        public void RequestDraftReplaceYes() => Draft?.ConfirmReplaceYes();
        public void RequestDraftReplaceNo() => Draft?.ConfirmReplaceNo();
        public void RequestDraftReplaceCancel() => Draft?.CancelReplace();

        public void RequestDraftReplaceComplete(int inventoryIndex)
        {
            if (States.Current != RunStateId.Draft || Draft == null || Inventory == null)
                return;

            if (!Draft.TryCompleteReplace(inventoryIndex, Inventory, out var resolved) || !resolved)
                return;

            States.DraftResolved();
        }

        void SpawnEnemy(EnemyDefinition def)
        {
            if (def == null || gridView == null || _path == null)
                return;

            _path.CollectSpawnTips(_spawnTips);
            if (_spawnTips.Count == 0)
            {
                Debug.LogWarning("[GemTD] No spawn tips — cannot spawn.");
                return;
            }

            var tip = SpawnTipScheduler.Next(_spawnTips, ref _nextTipIndex);

            if (!_path.TryGetWaypointPolyline(tip, _polylineCells))
                return;

            _polylineWorld.Clear();
            for (var i = 0; i < _polylineCells.Count; i++)
                _polylineWorld.Add(gridView.CellToWorld(_polylineCells[i]));

            var runtime = new EnemyRuntime();
            runtime.Init(def, _polylineWorld);
            _registry.Register(runtime);

            if (_enemyPool != null)
            {
                var view = _enemyPool.Get();
                view.Bind(runtime);
                _enemyViews.Add(view);
            }
        }

        int CountLivingEnemies()
        {
            var count = 0;
            for (var i = 0; i < _registry.Count; i++)
            {
                var e = _registry.GetAt(i);
                if (e != null && e.IsAlive)
                    count++;
            }
            return count;
        }

        void TickEnemies(float dt)
        {
            for (var i = _registry.Count - 1; i >= 0; i--)
            {
                var enemy = _registry.GetAt(i);
                if (enemy == null)
                    continue;

                if (!enemy.IsAlive)
                {
                    var killGold = enemy.Definition != null ? enemy.Definition.KillGold : 0;
                    Economy.GrantKillGold(killGold);
                    RemoveEnemy(enemy);
                    continue;
                }

                if (enemy.TickMove(dt))
                {
                    var leak = 1;
                    if (enemy.Definition != null && enemy.Definition.LeakDamage > 0)
                        leak = enemy.Definition.LeakDamage;
                    Economy.LoseLife(leak);
                    RemoveEnemy(enemy);
                    if (Economy.IsDefeated)
                        States.TriggerDefeat();
                }
            }
        }

        void RemoveEnemy(EnemyRuntime enemy)
        {
            _registry.Unregister(enemy);
            for (var i = _enemyViews.Count - 1; i >= 0; i--)
            {
                var view = _enemyViews[i];
                if (view == null || view.Runtime != enemy)
                    continue;

                view.Clear();
                _enemyViews.RemoveAt(i);
                if (_enemyPool != null)
                    _enemyPool.Release(view);
                else if (view != null)
                    Destroy(view.gameObject);
            }
        }

        void SyncEnemyViews()
        {
            for (var i = 0; i < _enemyViews.Count; i++)
                _enemyViews[i]?.SyncTransform();
        }

        void SyncProjectileViews()
        {
            if (_combat == null)
                return;

            var projectiles = _combat.Projectiles;

            while (_projectileViews.Count > projectiles.Count)
            {
                var last = _projectileViews[_projectileViews.Count - 1];
                _projectileViews.RemoveAt(_projectileViews.Count - 1);
                if (last != null)
                {
                    last.Clear();
                    if (_projectilePool != null)
                        _projectilePool.Release(last);
                    else
                        Destroy(last.gameObject);
                }
            }

            for (var i = 0; i < projectiles.Count; i++)
            {
                if (i >= _projectileViews.Count)
                {
                    if (_projectilePool == null)
                        break;
                    var view = _projectilePool.Get();
                    view.Bind(projectiles[i]);
                    _projectileViews.Add(view);
                }
                else
                {
                    var view = _projectileViews[i];
                    if (view.Runtime != projectiles[i])
                        view.Bind(projectiles[i]);
                    else
                        view.SyncTransform();
                }
            }
        }

        void EnsureHomeMarker()
        {
            if (gridView == null || _path == null)
                return;

            if (_homeMarker == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = "HomeBase";
                go.transform.SetParent(transform, false);
                go.transform.localScale = new Vector3(0.85f, 0.3f, 0.85f);
                var col = go.GetComponent<Collider>();
                if (col != null)
                    Destroy(col);
                _homeMarker = go.AddComponent<HomeBaseView>();

                var renderer = go.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    var block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block);
                    var color = new Color(0.85f, 0.25f, 0.3f);
                    block.SetColor("_BaseColor", color);
                    block.SetColor("_Color", color);
                    renderer.SetPropertyBlock(block);
                }
            }

            _homeMarker.Bind(_path.Home, gridView.CellToWorld(_path.Home));
        }

        void ClearSelectionHighlight()
        {
            for (var i = 0; i < _towerViews.Count; i++)
                _towerViews[i]?.SetSelected(false);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void TryDebugAdvance()
        {
            if (_debugAdvance == null || !_debugAdvance.WasPressedThisFrame())
                return;

            switch (States.Current)
            {
                case RunStateId.Plan:
                    if (!States.ExpandSatisfiedThisCycle)
                    {
                        if (_legalExpands.Count > 0)
                            TryConfirmExpand(_legalExpands[0]);
                        else
                            States.WaiveExpandRequirement();
                    }
                    else
                    {
                        RequestStartWave();
                    }
                    break;
                case RunStateId.Draft:
                    if (Draft != null && Draft.IsActive && Draft.CurrentOffer.Count > 0)
                        RequestDraftPick(0);
                    break;
                case RunStateId.Combat:
                    States.WaveCleared(offerDraft: false);
                    break;
            }
        }
#endif
    }
}

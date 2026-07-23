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
        [SerializeField] WaveDefinition[] waves;

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
        public int CurrentWaveNumber => WaveController != null ? WaveController.CurrentWaveNumber : 0;
        public bool HasSelectedTower => Placement != null && Placement.Selected != null;
        public bool SelectedHasSocketedGems =>
            Placement?.Selected != null && Placement.Selected.HasSocketedGems;

        GridBoard _board;
        PathGraph _path;
        MapExpandService _expand;
        EnemyRegistry _registry;
        CombatDirector _combat;
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
            if (States == null || States.Current == RunStateId.Defeat)
                return;

            var dt = Clock.DeltaTime;
            if (dt > 0f)
            {
                WaveController?.Tick(dt, _spawnerGate);
                TickEnemies(dt);

                if (States.Current == RunStateId.Combat)
                    _combat?.Tick(dt, _towers, _registry, _pipeline);
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

            var capacity = 6;
            if (runConfig != null && runConfig.SeedGems != null && runConfig.SeedGems.Length > capacity)
                capacity = runConfig.SeedGems.Length;
            Inventory = new GemInventory(capacity);
            if (runConfig != null)
                Inventory.Seed(runConfig.SeedGems);

            _expand = new MapExpandService(_path, _board);
            Placement = new TowerPlacementService(_board, _path, _expand, Economy);
            _registry = new EnemyRegistry();
            var cellSize = gridView != null ? gridView.CellSize : 1f;
            _combat = new CombatDirector(cellSize, projectileSpeed);
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
            if (next == RunStateId.Expand)
                RefreshExpandMarkers();
            else
                ClearExpandMarkers();
        }

        void RefreshExpandMarkers()
        {
            ClearExpandMarkers();
            if (_expand == null || expandMarkerPrefab == null || gridView == null)
                return;

            var count = _expand.CollectLegalExpands(_legalExpands);
            if (count == 0)
            {
                if (!_loggedExpandSkip)
                {
                    Debug.Log("[GemTD] No legal expands — auto-skip Expand → Build.");
                    _loggedExpandSkip = true;
                }
                States.ExpandConfirmed();
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
            if (States.Current != RunStateId.Expand)
                return false;

            if (!_expand.TryExpand(cell))
            {
                Debug.Log($"[GemTD] Expand rejected at {cell}");
                return false;
            }

            gridView?.SetPath(_path);
            ClearExpandMarkers();
            States.ExpandConfirmed();
            return true;
        }

        public void TryPlaceAtWorld(Vector3 world)
        {
            if (gridView == null || ballistaDef == null || Placement == null)
                return;

            var phase = States.Current;
            if (phase != RunStateId.Build && phase != RunStateId.Combat)
                return;

            var cell = gridView.WorldToCell(world);
            if (!Placement.TryPlace(ballistaDef, cell, phase, out var tower))
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
            if (States.Current != RunStateId.Build || WaveController == null)
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
            if (States.Current != RunStateId.Build)
            {
                Debug.Log("[GemTD] Socket frozen outside Build.");
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
                case RunStateId.Expand:
                    if (_legalExpands.Count > 0)
                        TryConfirmExpand(_legalExpands[0]);
                    else
                        States.ExpandConfirmed();
                    break;
                case RunStateId.Build:
                    RequestStartWave();
                    break;
                case RunStateId.Combat:
                    States.WaveCleared(offerDraft: false);
                    break;
            }
        }
#endif
    }
}

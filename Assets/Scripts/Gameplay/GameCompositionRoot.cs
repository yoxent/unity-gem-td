using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using LitMotion;
using GemTD.Core;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Grid;
using GemTD.Gameplay.Map;
using GemTD.Gameplay.Meta;
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
        [SerializeField] BuildBarCatalog buildBarCatalog;
        [SerializeField] WaveCatalog waveCatalog;
        [SerializeField] EnemyDefinition bossEnemy;
        [SerializeField] DraftPoolCatalog draftPoolCatalog;
        [SerializeField] CodexCatalog codexCatalog;
        [SerializeField] ChunkCatalog chunkCatalog;

        [Header("Scene")]
        [SerializeField] ChunkBoardView chunkBoardView;
        [SerializeField] RunInputController inputController;
        [SerializeField] Transform poolRoot;

        [Header("Prefabs")]
        [SerializeField] EnemyView enemyPrefab;
        [SerializeField] ProjectileView projectilePrefab;
        [SerializeField] TowerView towerPrefab;
        [SerializeField] ExpandMarkerView expandMarkerPrefab;
        [SerializeField] GameObject towerRangeIndicatorPrefab;

        [Header("Tuning")]
        [SerializeField] float projectileSpeed = 20f;

        public RunClock Clock { get; private set; }
        public SpeedControl Speed { get; private set; }
        public RunStateMachine States { get; private set; }
        public RunEconomy Economy { get; private set; }
        public GemInventory Inventory { get; private set; }
        public TowerPlacementService Placement { get; private set; }
        public WaveController WaveController { get; private set; }
        public DraftService Draft { get; private set; }
        public SocketLockdown SocketLockdown { get; private set; }
        public CodexProgress Codex { get; private set; }
        public CodexCatalog CodexCatalog => codexCatalog;
        public StatusRuntime Statuses => _statuses;
        public RunStatsTracker RunStats => _runStats;
        public int CurrentWaveNumber => WaveController != null ? WaveController.CurrentWaveNumber : 0;
        public bool HasSelectedTower => Placement != null && Placement.Selected != null;
        public bool SelectedHasSocketedGems =>
            Placement?.Selected != null && Placement.Selected.HasSocketedGems;

        public bool CanSellSelected =>
            Placement != null
            && Inventory != null
            && Placement.CanSell(Placement.Selected, States != null ? States.Current : RunStateId.Boot, Inventory);

        public bool SelectedSocketOccupied(int socketIndex)
        {
            var tower = Placement?.Selected;
            if (tower?.Sockets == null || socketIndex < 0 || socketIndex >= tower.Sockets.Length)
                return false;
            var gem = tower.Sockets[socketIndex];
            return gem != null && gem.Id != GemId.None;
        }
        public bool CanStartWave =>
            States != null
            && States.Current == RunStateId.Plan
            && States.ExpandSatisfiedThisCycle
            && WaveController != null;

        public string GetPlaceTowerName(int index)
        {
            if (!TryGetBuildBarTower(index, out var def))
                return "?";
            return !string.IsNullOrEmpty(def.DisplayName) ? def.DisplayName : def.name;
        }

        public int GetPlaceTowerCost(int index)
        {
            if (!TryGetBuildBarTower(index, out var def))
                return 0;
            return ComputePlaceCost(def);
        }

        public int ComputePlaceCost(TowerDefinition def) =>
            TowerCostCalculator.ComputePlaceCost(def, _towers);

        public int BuildBarTowerCount => buildBarCatalog != null ? buildBarCatalog.Count : 0;

        public TowerDefinition[] GetBuildBarTowers() =>
            buildBarCatalog != null && buildBarCatalog.Towers != null
                ? buildBarCatalog.Towers
                : System.Array.Empty<TowerDefinition>();

        public bool HasPlaceTowerSelected => _placeDef != null;
        public float SelectedSocketLockRemaining
        {
            get
            {
                if (SocketLockdown == null || Placement?.Selected == null)
                    return 0f;
                return SocketLockdown.Remaining(Placement.Selected);
            }
        }

        public bool CanUnsocketSelected(int socketIndex)
        {
            var tower = Placement?.Selected;
            if (tower == null || tower.Sockets == null)
                return false;
            if (socketIndex < 0 || socketIndex >= tower.Sockets.Length)
                return false;
            if (tower.Sockets[socketIndex] == null)
                return false;
            if (EvolutionEvaluator.IsHydraBallista(tower))
                return false;
            if (SelectedSocketLockRemaining > 0f)
                return false;
            return true;
        }

        public string BuildSelectedTowerDetailsText()
        {
            var tower = Placement?.Selected;
            if (tower == null || tower.Def == null)
                return "No tower selected";

            var def = tower.Def;
            var sb = new System.Text.StringBuilder(128);
            sb.Append(def.DisplayName);

            if (EvolutionEvaluator.IsHydraBallista(tower))
                sb.Append(" [HYDRA]");
            sb.Append('\n');

            var spec = _pipeline != null
                ? _pipeline.Resolve(tower, _socketModScratch)
                : AttackSpec.FromBase(def.Damage, 1, def.SplashRadius);
            var dmg = spec.Damage * tower.OutgoingDamageMultiplier;
            var fireRate = spec.FireRateMultiplier > 0.01f ? spec.FireRateMultiplier : 0.01f;
            var interval = def.AttackInterval / fireRate;
            var attackRate = interval > 0.01f ? 1f / interval : 0f;
            var tags = GemTags.EffectiveTowerTags(def);
            sb.Append($"Damage {dmg:0.#}");
            if (spec.ProjectileCount > 1)
                sb.Append($" ×{spec.ProjectileCount}");
            sb.Append('\n');
            sb.Append($"Attack rate {attackRate:0.##}/s\n");
            sb.Append($"Attack range {EffectiveAttackRange(tower):0.#}\n");
            sb.Append($"Tags {GemTags.Format(tags)}");

            var lockLeft = SelectedSocketLockRemaining;
            if (lockLeft > 0f)
                sb.Append($"\nLOCK {lockLeft:0.0}s");

            return sb.ToString();
        }

        public void ToggleCodexPanel()
        {
            CodexPanelOpen = !CodexPanelOpen;
            GameEvents.RaiseCodexToggled();
        }

        public bool CodexPanelOpen { get; private set; }

        public void ClearPlaceTower()
        {
            _placeDef = null;
            _placementGhost?.Hide();
            GameEvents.RaisePlaceModeChanged();
        }

        public TargetingRecipe SelectedTargeting =>
            HasSelectedTower ? Placement.Selected.Targeting : TargetingRecipe.Default;
        public TargetingApplyScope CurrentApplyScope => _applyScope;

        TowerDefinition _placeDef;
        TargetingApplyScope _applyScope = TargetingApplyScope.ThisTower;
        readonly TargetingClipboard _targetingClipboard = new TargetingClipboard();

        GridBoard _board;
        PathGraph _path;
        ChunkExpandService _expand;
        ChunkGrid _chunkGrid;
        ChunkStampService _stamp;
        System.Random _rng;
        EnemyRegistry _registry;
        CombatDirector _combat;
        BeaconAuraSystem _beaconAura;
        GemModifierPipeline _pipeline;
        StatusRuntime _statuses;
        readonly RunStatsTracker _runStats = new RunStatsTracker();
        EnemySpawnerGate _spawnerGate;

        readonly List<TowerRuntime> _towers = new List<TowerRuntime>(16);
        readonly List<TowerView> _towerViews = new List<TowerView>(16);
        PlacementGhostView _placementGhost;
        readonly List<EnemyView> _enemyViews = new List<EnemyView>(32);
        readonly List<ProjectileView> _projectileViews = new List<ProjectileView>(32);
        readonly List<ExpandMarkerView> _markers = new List<ExpandMarkerView>(16);
        readonly List<Vector2Int> _legalChunks = new List<Vector2Int>(16);
        readonly HashSet<Vector2Int> _legalChunkSet = new HashSet<Vector2Int>();
        static readonly EdgeFlags[] MarkerDirs =
        {
            EdgeFlags.North, EdgeFlags.East, EdgeFlags.South, EdgeFlags.West
        };
        readonly List<Vector2Int> _polylineCells = new List<Vector2Int>(16);
        readonly List<Vector3> _polylineWorld = new List<Vector3>(16);
        readonly List<Vector2Int> _spawnTips = new List<Vector2Int>(8);
        readonly List<Vector2Int> _rankedTipsScratch = new List<Vector2Int>(8);
        readonly List<Vector2Int> _bossSpawnTips = new List<Vector2Int>(8);
        readonly List<IAttackModifier> _socketModScratch = new List<IAttackModifier>(4);
        readonly List<EnemyRuntime> _livingScratch = new List<EnemyRuntime>(32);

        ViewObjectPool<EnemyView> _enemyPool;
        ManualMotionDispatcher _enemyHopDispatcher;
        ViewObjectPool<ProjectileView> _projectilePool;
        ViewObjectPool<ExpandMarkerView> _markerPool;

        InputAction _debugAdvance;
        InputAction _debugFillBag;
        InputActionMap _debugMap;
        bool _loggedExpandSkip;
        int _nextTipIndex;
        int _bossSpawnCursor;
        HomeBaseView _homeMarker;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            PlayerProfile.Load();

            // NOTE: do NOT GameEvents.ClearAll() here — UI prefabs subscribe in OnEnable during
            // scene load, which runs before this Awake. Wiping here silently kills all UI event
            // subscriptions (speed labels, gold/lives/wave text). OnDestroy handles cleanup.
            _placeDef = null;

            Clock = new RunClock();
            _enemyHopDispatcher = new ManualMotionDispatcher();
            Speed = new SpeedControl(Clock);
            States = new RunStateMachine(Speed, Clock);
            States.StateChanged += OnStateChanged;

            BootstrapServices();
            SetupPools();

            if (inputController != null)
                inputController.Bind(this);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _debugMap = new InputActionMap("RunDebug");
            _debugAdvance = _debugMap.AddAction("AdvancePhase", InputActionType.Button);
            _debugAdvance.AddBinding("<Keyboard>/f5");
            _debugFillBag = _debugMap.AddAction("FillBag", InputActionType.Button);
            _debugFillBag.AddBinding("<Keyboard>/f6");
            _debugMap.Enable();
#endif
        }

        void Start()
        {
            _runStats.Reset();
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
            TryDebugFillBag();
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
                    var cellSize = chunkBoardView != null ? chunkBoardView.CellSize : 1f;
                    _beaconAura?.Tick(_towers, _registry, cellSize);
                    if (_statuses != null && _registry != null)
                    {
                        _livingScratch.Clear();
                        _registry.CopyAlive(_livingScratch);
                        _statuses.Tick(dt, _livingScratch);
                    }
                    _combat?.Tick(dt, _towers, _registry, _pipeline, _statuses);
                    SocketLockdown?.Tick(dt);
                }
            }

            ApplyEnemyHopPlaybackSpeeds();
            if (_enemyHopDispatcher != null)
                _enemyHopDispatcher.Update(dt);

            SyncProjectileViews();
            SyncEnemyViews();
            TickPlacementGhost();
        }

        void EnsurePlacementGhost()
        {
            if (_placementGhost != null)
                return;

            var go = new GameObject("PlacementGhost");
            go.transform.SetParent(transform, false);
            _placementGhost = go.AddComponent<PlacementGhostView>();
            _placementGhost.EnsureBuilt(towerPrefab, towerRangeIndicatorPrefab);
            _placementGhost.Hide();
        }

        float EffectiveAttackRange(TowerRuntime tower)
        {
            if (tower == null || tower.Def == null)
                return 0f;

            var spec = _pipeline != null
                ? _pipeline.Resolve(tower, _socketModScratch)
                : AttackSpec.FromBase(tower.Def.Damage, 1, tower.Def.SplashRadius);
            var rangeMul = spec.RangeMultiplier > 0.01f ? spec.RangeMultiplier : 1f;
            return tower.Def.Range * rangeMul;
        }

        /// <summary>Bloons-style ghost while placing, plus range disc when Tower Details is open.</summary>
        public void TickPlacementGhost()
        {
            if (chunkBoardView == null || States == null)
            {
                _placementGhost?.Hide();
                return;
            }

            var phase = States.Current;
            if (phase != RunStateId.Plan && phase != RunStateId.Combat)
            {
                _placementGhost?.Hide();
                return;
            }

            if (HasPlaceTowerSelected && Placement != null)
            {
                if (Mouse.current == null)
                {
                    _placementGhost?.Hide();
                    return;
                }

                var cam = Camera.main;
                if (cam == null)
                {
                    _placementGhost?.Hide();
                    return;
                }

                EnsurePlacementGhost();
                var ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
                var plane = new Plane(Vector3.up, Vector3.zero);
                if (!plane.Raycast(ray, out var enter))
                {
                    _placementGhost.Hide();
                    return;
                }

                var world = ray.GetPoint(enter);
                var cell = chunkBoardView.WorldToCell(world);
                var valid = Placement.CanPlace(_placeDef, cell, phase, ComputePlaceCost(_placeDef));
                var range = _placeDef != null ? _placeDef.Range : 3f;
                _placementGhost.SetRange(range);
                _placementGhost.ShowAt(chunkBoardView.CellToWorld(cell), valid);
                return;
            }

            if (HasSelectedTower && Placement.Selected != null)
            {
                EnsurePlacementGhost();
                _placementGhost.SetRange(EffectiveAttackRange(Placement.Selected));
                _placementGhost.ShowRangeOnlyAt(chunkBoardView.CellToWorld(Placement.Selected.Cell));
                return;
            }

            _placementGhost?.Hide();
        }

        void BootstrapServices()
        {
            if (runConfig == null)
                Debug.LogError("[GemTD] Assign RunConfig on GameCompositionRoot in the inspector.");

            var gold = runConfig != null ? runConfig.StartingGold : 100;
            var lives = runConfig != null ? runConfig.StartingLives : 20;
            var endWaveGold = runConfig != null ? runConfig.EndWaveGold : 50;

            var chunksW = runConfig != null && runConfig.ChunkGridWidth > 0 ? runConfig.ChunkGridWidth : 13;
            var chunksH = runConfig != null && runConfig.ChunkGridHeight > 0 ? runConfig.ChunkGridHeight : 13;
            var cellW = chunksW * ChunkMask.Size;
            var cellH = chunksH * ChunkMask.Size;
            _board = new GridBoard(cellW, cellH);
            _path = new PathGraph(cellW, cellH);
            _path.BindBoard(_board);
            _chunkGrid = new ChunkGrid(chunksW, chunksH);
            _stamp = new ChunkStampService();
            _rng = new System.Random();

            if (chunkBoardView != null)
                chunkBoardView.Bind(_chunkGrid);

            var laneCount = runConfig != null ? runConfig.LaneCount : 1;
            if (chunkCatalog == null)
                Debug.LogError("[GemTD] ChunkCatalog is not assigned on GameCompositionRoot.");
            else
                StartLayoutBuilder.Build(
                    _chunkGrid, _stamp, _path, _board, chunkCatalog, _rng, laneCount);
            EnsureHomeMarker();

            Economy = new RunEconomy(gold, lives, _runStats.RecordGoldEarned);
            GameEvents.RaiseGoldChanged(Economy.Gold);
            GameEvents.RaiseLivesChanged(Economy.Lives);

            var capacity = runConfig != null && runConfig.InventoryCapacity > 0
                ? runConfig.InventoryCapacity
                : 10;
            Inventory = new GemInventory(capacity);
            SeedHydraRecipeInventory();
            GameEvents.RaiseInventoryChanged();

            Draft = new DraftService(new System.Random());
            var lockdownSeconds = runConfig != null
                ? Mathf.Max(0f, runConfig.SocketLockdownSeconds)
                : 0f;
            SocketLockdown = new SocketLockdown(lockdownSeconds);
            Codex = new CodexProgress(new JsonFileCodexStore());
            _statuses = new StatusRuntime();

            _expand = new ChunkExpandService(_chunkGrid, _path, _board, _stamp, chunkCatalog, _rng, runConfig);
            Placement = new TowerPlacementService(_board, _path, Economy);
            _registry = new EnemyRegistry();
            var cellSize = chunkBoardView != null ? chunkBoardView.CellSize : 1f;
            _combat = new CombatDirector(cellSize, projectileSpeed, _runStats.RecordDamage);
            _beaconAura = new BeaconAuraSystem();
            _pipeline = new GemModifierPipeline();

            _spawnerGate = new EnemySpawnerGate(SpawnEnemy, () => CountLivingEnemies());

            var waveDefs = waveCatalog != null ? waveCatalog.GetWavesOrEmpty() : System.Array.Empty<WaveDefinition>();
            if (waveDefs.Length == 0)
                Debug.LogError("[GemTD] No wave definitions assigned on WaveCatalog.");
            else
            {
                var endWave = ExpandPickPolicy.EndWave(runConfig);
                WaveController = new WaveController(
                    waveDefs, States, Economy, endWaveGold, bossEnemy, endWave, CapLastTipBeforeVictory);
            }
        }

        void CapLastTipBeforeVictory()
        {
            if (_expand == null)
            {
                Debug.LogWarning("[GemTD] Wave EndVictory: expand service missing — cannot auto-DeadEnd last tip.");
                return;
            }

            SyncExpandPolicy();
            if (!_expand.TryForceDeadEndCap())
                Debug.LogWarning("[GemTD] Wave EndVictory: failed to auto-DeadEnd last tip — proceeding to Victory anyway.");
        }

        /// <summary>Victory Run Summary → Endless (Task 8). No expand; combat continues past EndWave.</summary>
        public void BeginEndless()
        {
            if (States == null || WaveController == null)
                return;
            if (States.Current != RunStateId.VictorySummary)
                return;

            WaveController.BeginEndless();
            States.EnterEndless();
        }

        void SetupPools()
        {
            var parent = poolRoot != null ? poolRoot : transform;
            if (enemyPrefab != null)
                _enemyPool = new ViewObjectPool<EnemyView>(enemyPrefab, parent);
            if (projectilePrefab != null)
                _projectilePool = new ViewObjectPool<ProjectileView>(projectilePrefab, parent);
            if (expandMarkerPrefab != null)
                _markerPool = new ViewObjectPool<ExpandMarkerView>(expandMarkerPrefab, parent);
        }

        void OnStateChanged(RunStateId prev, RunStateId next)
        {
            if (next == RunStateId.Defeat || next == RunStateId.VictorySummary)
                PlayerProfile.TryUpdateHighestWave(CurrentWaveNumber);

            GameEvents.RaiseRunStateChanged();

            if (IsCombatPhase(prev) && !IsCombatPhase(next))
            {
                _combat?.ClearProjectiles();
                SyncProjectileViews();
            }

            if (next == RunStateId.Plan)
            {
                _loggedExpandSkip = false;
                if (WaveController != null && WaveController.IsEndless && States.ExpandSatisfiedThisCycle)
                {
                    ClearExpandMarkers();
                    StartWaveAfterExpand();
                    return;
                }

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

            var pool = draftPoolCatalog != null
                ? draftPoolCatalog.GetGemsOrEmpty()
                : System.Array.Empty<GemDefinition>();
            var usable = 0;
            for (var i = 0; i < pool.Length; i++)
            {
                if (pool[i] != null)
                    usable++;
            }

            if (usable < 3)
            {
                Debug.LogError(
                    "[GemTD] DraftPoolCatalog needs at least 3 assigned gems on GameCompositionRoot " +
                    $"(found {usable}). Assign the catalog at Data/Gems/DraftPoolCatalog.");
                return;
            }

            Draft.BeginOffer(pool, allowSkip);
            GameEvents.RaiseDraftOfferChanged();
            Debug.Log(
                $"[GemTD] Draft offer ({(allowSkip ? "skip OK" : "must pick")}): " +
                $"{Draft.CurrentOffer[0].DisplayName} / {Draft.CurrentOffer[1].DisplayName} / {Draft.CurrentOffer[2].DisplayName}");
        }

        void RefreshExpandMarkers()
        {
            ClearExpandMarkers();
            if (_expand == null || _markerPool == null || chunkBoardView == null)
                return;

            if (States.Current != RunStateId.Plan || States.ExpandSatisfiedThisCycle)
                return;

            SyncExpandPolicy();
            var upcoming = WaveController != null ? WaveController.NextWaveNumber : 1;
            var endWave = ExpandPickPolicy.EndWave(runConfig);
            if ((WaveController != null && WaveController.IsEndless)
                || ExpandPickPolicy.SkipExpand(upcoming, endWave))
            {
                if (!_loggedExpandSkip)
                {
                    if (WaveController != null && WaveController.IsEndless)
                        Debug.Log("[GemTD] Endless — skip expand, start next combat.");
                    else
                        Debug.Log($"[GemTD] EndWave {endWave} — skip expand, start final combat.");
                    _loggedExpandSkip = true;
                }
                States.WaiveExpandRequirement();
                StartWaveAfterExpand();
                return;
            }

            var count = _expand.CollectLegalExpands(_legalChunks);
            _legalChunkSet.Clear();
            for (var i = 0; i < _legalChunks.Count; i++) _legalChunkSet.Add(_legalChunks[i]);

            if (count == 0)
            {
                if (!_loggedExpandSkip)
                {
                    Debug.Log("[GemTD] No legal expands — waive expand requirement.");
                    _loggedExpandSkip = true;
                }
                States.WaiveExpandRequirement();
                StartWaveAfterExpand();
                return;
            }

            // One plate per occupied open edge whose empty neighbor is a legal
            // expand slot — B and C both opening into x each get a button.
            var cellSize = chunkBoardView.CellSize;
            for (var cy = 0; cy < _chunkGrid.ChunksH; cy++)
            {
                for (var cx = 0; cx < _chunkGrid.ChunksW; cx++)
                {
                    if (!_chunkGrid.TryGet(cx, cy, out var slot)) continue;
                    var occupied = new Vector2Int(cx, cy);
                    for (var d = 0; d < MarkerDirs.Length; d++)
                    {
                        var outward = MarkerDirs[d];
                        if ((slot.Mask.OpenEdges & outward) == 0) continue;
                        var dest = _chunkGrid.NeighborCoord(occupied, outward);
                        if (!_chunkGrid.InBounds(dest.x, dest.y) || _chunkGrid.IsOccupied(dest.x, dest.y))
                            continue;
                        if (!_legalChunkSet.Contains(dest))
                            continue;

                        var portal = ChunkMask.AdjacentExpandCell(occupied, outward);
                        var marker = _markerPool.Get();
                        marker.Bind(dest, chunkBoardView.CellCenterWorld(portal.x, portal.y), cellSize, outward);
                        _markers.Add(marker);
                    }
                }
            }
        }

        void ClearExpandMarkers()
        {
            for (var i = 0; i < _markers.Count; i++)
            {
                if (_markers[i] != null && _markerPool != null)
                    _markerPool.Release(_markers[i]);
            }
            _markers.Clear();
        }

        public bool TryChunkExpandAtWorld(Vector3 world)
        {
            if (chunkBoardView == null) return false;
            var cell = chunkBoardView.WorldToCell(world);
            var coord = new Vector2Int(
                Mathf.FloorToInt(cell.x / (float)ChunkMask.Size),
                Mathf.FloorToInt(cell.y / (float)ChunkMask.Size));
            return TryConfirmChunkExpand(coord);
        }

        public bool TryConfirmChunkExpand(Vector2Int coord)
        {
            if (States.Current != RunStateId.Plan || States.ExpandSatisfiedThisCycle)
                return false;

            if (!_legalChunkSet.Contains(coord))
            {
                Debug.Log($"[GemTD] Expand rejected at chunk {coord} (not in legal set).");
                return false;
            }

            SyncExpandPolicy();
            if (!_expand.TryExpand(coord))
            {
                Debug.Log($"[GemTD] Expand rejected at chunk {coord}");
                return false;
            }

            // ChunkBoardView self-updates via ChunkPlaced event — no explicit SetPath call.
            ClearExpandMarkers();
            States.NotifyExpandDone();
            StartWaveAfterExpand();
            return true;
        }

        void SyncExpandPolicy()
        {
            if (_expand == null)
                return;
            _expand.Config = runConfig;
            _expand.UpcomingWaveNumber = WaveController != null ? WaveController.NextWaveNumber : 1;
        }

        void StartWaveAfterExpand()
        {
            if (WaveController == null || States == null)
                return;
            if (States.Current != RunStateId.Plan || !States.ExpandSatisfiedThisCycle)
                return;

            BeginWaveWithBossCadence();
            GameEvents.RaiseWaveChanged(WaveController.CurrentWaveNumber);
            GameEvents.RaiseRunStateChanged();
        }

        /// <summary>
        /// Shared by every <c>WaveController.StartWave</c> call site: collects the live spawn
        /// tip count (same combat about to start), starts the wave with it (boss cadence =
        /// min(wave/10, tipCount)), then snapshots the furthest-tip boss routing for that wave.
        /// Callers still raise their own events afterward.
        /// </summary>
        void BeginWaveWithBossCadence()
        {
            var tipCount = _path != null ? _path.CollectSpawnTips(_spawnTips) : 0;
            WaveController.StartWave(tipCount);
            PrepareBossSpawnTips();
        }

        /// <summary>
        /// Snapshot the furthest tips (hop BFS from home, coord tiebreak) for this wave's
        /// boss cadence, using the same live tip set <see cref="WaveController.StartWave"/>
        /// just used to compute <see cref="WaveController.CurrentBossCount"/>.
        /// </summary>
        void PrepareBossSpawnTips()
        {
            _bossSpawnTips.Clear();
            _bossSpawnCursor = 0;

            var bossCount = WaveController != null ? WaveController.CurrentBossCount : 0;
            if (bossCount <= 0 || _path == null)
                return;

            _path.RankTipsByHopDescending(_spawnTips, _rankedTipsScratch);
            for (var i = 0; i < bossCount && i < _rankedTipsScratch.Count; i++)
                _bossSpawnTips.Add(_rankedTipsScratch[i]);
        }

        public void TryPlaceAtWorld(Vector3 world, bool keepPlacementSelected = false)
        {
            if (chunkBoardView == null || _placeDef == null || Placement == null)
                return;

            var phase = States.Current;
            if (phase != RunStateId.Plan && phase != RunStateId.Combat)
                return;

            var cell = chunkBoardView.WorldToCell(world);
            var placeCost = ComputePlaceCost(_placeDef);
            if (!Placement.TryPlace(_placeDef, cell, phase, placeCost, out var tower))
            {
                Debug.Log($"[GemTD] Place rejected at {cell} (phase={phase}, gold={Economy.Gold}, cost={placeCost})");
                return;
            }

            _towers.Add(tower);
            _runStats.RecordTowerPlaced(tower.Def);
            if (towerPrefab != null)
            {
                var view = Instantiate(towerPrefab, transform);
                view.Bind(tower, chunkBoardView.CellToWorld(cell));
                _towerViews.Add(view);
            }

            // Classic TD: one build-bar pick = one place, unless Shift is held.
            if (!keepPlacementSelected)
                ClearPlaceTower();

            GameEvents.RaiseTowerRosterChanged();
        }

        public void ClearTowerSelection()
        {
            if (Placement != null)
                Placement.Selected = null;

            for (var i = 0; i < _towerViews.Count; i++)
            {
                if (_towerViews[i] != null)
                    _towerViews[i].SetSelected(false);
            }

            GameEvents.RaiseTowerSelectionChanged();
        }

        public void SetPlaceTower(int index)
        {
            ClearTowerSelection();
            _placeDef = null;
            if (TryGetBuildBarTower(index, out var def))
                _placeDef = def;

            if (_placeDef != null)
            {
                EnsurePlacementGhost();
                TickPlacementGhost();
            }

            GameEvents.RaisePlaceModeChanged();
        }

        bool TryGetBuildBarTower(int index, out TowerDefinition def)
        {
            def = null;
            return buildBarCatalog != null && buildBarCatalog.TryGet(index, out def);
        }

        public void CyclePriority(int slot, int delta = 1)
        {
            if (!HasSelectedTower)
                return;

            var selected = Placement.Selected;
            var next = selected.Targeting.WithCycled(slot, delta);
            TargetingService.Apply(next, TargetingApplyScope.ThisTower, selected, _towers);
            GameEvents.RaiseTargetingChanged();
        }

        public bool TryCycleApplyScope(out bool needsAllConfirm)
        {
            needsAllConfirm = false;
            if (!HasSelectedTower)
                return false;

            var next = TargetingScopeRequests.Next(_applyScope);
            if (TargetingScopeRequests.NeedsAllConfirm(_applyScope, next))
            {
                needsAllConfirm = true;
                return true;
            }

            SetApplyScope(next);
            return true;
        }

        public void SetApplyScope(TargetingApplyScope scope)
        {
            _applyScope = scope;
            if (!HasSelectedTower)
                return;

            TargetingService.Apply(Placement.Selected.Targeting, _applyScope, Placement.Selected, _towers);
            GameEvents.RaiseTargetingChanged();
        }

        public void CopySelectedTargeting()
        {
            if (!HasSelectedTower)
                return;

            _targetingClipboard.Copy(Placement.Selected.Targeting);
        }

        public void PasteSelectedTargeting()
        {
            if (!HasSelectedTower || !_targetingClipboard.TryGet(out var recipe))
                return;

            TargetingService.Apply(recipe, TargetingApplyScope.ThisTower, Placement.Selected, _towers);
            GameEvents.RaiseTargetingChanged();
        }

        public void SelectTower(TowerView view)
        {
            if (view == null || view.Runtime == null)
                return;

            ClearPlaceTower();
            Placement.Selected = view.Runtime;
            for (var i = 0; i < _towerViews.Count; i++)
            {
                var tv = _towerViews[i];
                if (tv != null)
                    tv.SetSelected(tv == view);
            }

            GameEvents.RaiseTowerSelectionChanged();
        }

        public void RequestStartWave()
        {
            if (!CanStartWave)
                return;

            BeginWaveWithBossCadence();
            GameEvents.RaiseWaveChanged(WaveController.CurrentWaveNumber);
        }

        public void RequestSellSelected()
        {
            var selected = Placement?.Selected;
            if (selected == null)
                return;

            if (!Placement.TrySell(selected, States.Current, Inventory))
            {
                var gemCount = 0;
                for (var s = 0; s < selected.Sockets.Length; s++)
                {
                    if (selected.Sockets[s] != null)
                        gemCount++;
                }

                var bagBlocked = Inventory != null
                                 && gemCount > Inventory.FreeSlotCount;
                Debug.Log(bagBlocked
                    ? "[GemTD] Sell blocked — inventory cannot fit socketed gems (discard first)."
                    : $"[GemTD] Sell rejected (phase={States.Current})");
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
            GameEvents.RaiseTowerSelectionChanged();
            GameEvents.RaiseInventoryChanged();
            GameEvents.RaiseTowerRosterChanged();
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

            if (SocketLockdown != null && !SocketLockdown.CanSocket(tower, States.Current))
            {
                Debug.Log($"[GemTD] Tower sockets locked ({SocketLockdown.Remaining(tower):0.0}s).");
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
                // Important: keep the gem in its original inventory index.
                // If we fall back to TryAdd(), GemInventory picks the first empty slot,
                // which looks like the gem "teleports" when a tower has fewer sockets.
                if (!Inventory.TryAddAt(inventoryIndex, gem))
                {
                    // Defensive fallback: should not happen because we just removed from this index.
                    Inventory.TryAdd(gem);
                }
                Debug.Log($"[GemTD] Could not socket {gem.DisplayName} (full sockets, duplicate GemId, or tag mismatch).");
                return;
            }

            NotifySocketChanged(tower, gem);
            GameEvents.RaiseInventoryChanged();
        }

        void NotifySocketChanged(TowerRuntime tower, GemDefinition socketedGem)
        {
            if (socketedGem != null)
                _runStats.RecordGemSocketed(socketedGem.Id);
            OnSocketChanged(tower);
        }

        /// <summary>
        /// Socket the gem from a specific inventory index onto a specific tower socket index.
        /// Supports swapping: if the destination socket is occupied, the existing gem is moved
        /// into inventory (if possible) and the dragged gem takes that socket.
        /// </summary>
        public void RequestSocketFromInventoryAt(int inventoryIndex, int socketIndex)
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

            if (SocketLockdown != null && !SocketLockdown.CanSocket(tower, States.Current))
            {
                Debug.Log($"[GemTD] Tower sockets locked ({SocketLockdown.Remaining(tower):0.0}s).");
                return;
            }

            if (!Inventory.TryTakeAt(inventoryIndex, out var gem))
                return;

            // If the socket is empty, just socket the dragged gem back onto the exact target.
            var socketWasOccupied = socketIndex >= 0
                                     && tower.Sockets != null
                                     && socketIndex < tower.Sockets.Length
                                     && tower.Sockets[socketIndex] != null;

            if (!socketWasOccupied)
            {
                if (tower.TrySocket(gem, socketIndex, allowSocket: true))
                {
                    NotifySocketChanged(tower, gem);
                    GameEvents.RaiseInventoryChanged();
                    return;
                }

                // Socket rejected (usually uniqueness). Put the gem back.
                Inventory.TryAdd(gem);
                GameEvents.RaiseInventoryChanged();
                return;
            }

            // Swap path: remove existing gem from the target socket, try socketing dragged gem,
            // and only then move the displaced gem into inventory.
            if (!tower.TryUnsocket(socketIndex, out var displacedGem, allowSocket: true))
            {
                Inventory.TryAdd(gem);
                GameEvents.RaiseInventoryChanged();
                return;
            }

            if (!tower.TrySocket(gem, socketIndex, allowSocket: true))
            {
                // Rejected (usually uniqueness); restore displaced gem and return dragged gem.
                tower.TrySocket(displacedGem, socketIndex, allowSocket: true);
                Inventory.TryAdd(gem);
                GameEvents.RaiseInventoryChanged();
                return;
            }

            // Dragged gem is now in the target socket; move displaced gem back into the exact
            // inventory slot the dragged gem came from (swap semantics).
            if (!Inventory.TryAddAt(inventoryIndex, displacedGem))
            {
                // Very defensive: slot should be empty because we just took from it.
                // Restore previous state.
                tower.TryUnsocket(socketIndex, out _, allowSocket: true);
                tower.TrySocket(displacedGem, socketIndex, allowSocket: true);
                Inventory.TryAddAt(inventoryIndex, gem);
                GameEvents.RaiseInventoryChanged();
                return;
            }

            NotifySocketChanged(tower, gem);
            GameEvents.RaiseInventoryChanged();
        }

        public void RequestUnsocket(int socketIndex)
        {
            if (States.Current != RunStateId.Plan && States.Current != RunStateId.Combat)
                return;

            var tower = Placement?.Selected;
            if (tower == null || Inventory == null)
                return;

            if (SocketLockdown != null && !SocketLockdown.CanSocket(tower, States.Current))
            {
                Debug.Log($"[GemTD] Tower sockets locked ({SocketLockdown.Remaining(tower):0.0}s).");
                return;
            }

            if (Inventory.FreeSlotCount <= 0)
            {
                Debug.Log("[GemTD] Unsocket blocked — inventory full (discard first).");
                return;
            }

            if (!tower.TryUnsocket(socketIndex, out var gem, allowSocket: true))
                return;

            if (!Inventory.TryAdd(gem))
            {
                tower.TrySocket(gem, socketIndex, allowSocket: true);
                return;
            }

            OnSocketChanged(tower);
            GameEvents.RaiseInventoryChanged();
        }

        /// <summary>
        /// Drag socket → inventory: unsocket and place into a specific inventory slot.
        /// If the target slot is occupied, swap: the existing inventory gem sockets into
        /// the vacated socket, and the unsocketed gem lands in the target inventory slot.
        /// </summary>
        public void RequestUnsocketToInventoryAt(int socketIndex, int inventoryIndex)
        {
            if (States.Current != RunStateId.Plan && States.Current != RunStateId.Combat)
                return;

            var tower = Placement?.Selected;
            if (tower == null || Inventory == null)
                return;

            if (SocketLockdown != null && !SocketLockdown.CanSocket(tower, States.Current))
            {
                Debug.Log($"[GemTD] Tower sockets locked ({SocketLockdown.Remaining(tower):0.0}s).");
                return;
            }

            if (inventoryIndex < 0 || inventoryIndex >= Inventory.Capacity)
                return;

            var targetInventoryGem = Inventory.Slots[inventoryIndex]; // null if empty

            // Remove gem from socket.
            if (!tower.TryUnsocket(socketIndex, out var unsocketed, allowSocket: true))
                return;

            if (targetInventoryGem == null)
            {
                // Target slot is empty — place directly.
                if (!Inventory.TryAddAt(inventoryIndex, unsocketed))
                {
                    // Defensive: restore.
                    tower.TrySocket(unsocketed, socketIndex, allowSocket: true);
                    return;
                }
            }
            else
            {
                // Target slot is occupied — swap: take the existing gem, put unsocketed gem there,
                // then try to socket the displaced inventory gem into the now-empty socket.
                Inventory.TryTakeAt(inventoryIndex, out var displaced);
                Inventory.TryAddAt(inventoryIndex, unsocketed);

                if (!tower.TrySocket(displaced, socketIndex, allowSocket: true))
                {
                    // Socket rejected the displaced gem (uniqueness). Restore everything.
                    Inventory.TryTakeAt(inventoryIndex, out _);
                    Inventory.TryAddAt(inventoryIndex, targetInventoryGem);
                    tower.TrySocket(unsocketed, socketIndex, allowSocket: true);
                    return;
                }

                NotifySocketChanged(tower, displaced);
            }

            GameEvents.RaiseInventoryChanged();
        }

        void OnSocketChanged(TowerRuntime tower)
        {
            SocketLockdown?.NotifyChanged(tower, States.Current);
            GameEvents.RaiseTowerSelectionChanged();
            if (EvolutionEvaluator.IsHydraBallista(tower) && Codex != null && codexCatalog != null)
            {
                var entry = codexCatalog.GetById("hydra-ballista");
                if (entry != null)
                {
                    Codex.Unlock(entry);
                    GameEvents.RaiseEvolutionUnlocked();
                    Debug.Log("[EvolutionUnlocked] Hydra Ballista formed.");
                }
            }
        }

        void SeedHydraRecipeInventory()
        {
            if (Inventory == null || runConfig == null || !runConfig.SeedHydraRecipeGems)
                return;

            var seeds = runConfig.SeedGems;
            if (seeds == null || seeds.Length == 0)
                return;

            for (var i = 0; i < seeds.Length; i++)
            {
                if (seeds[i] == null)
                    continue;
                if (!Inventory.TryAdd(seeds[i]))
                    Debug.LogWarning($"[GemTD] Could not seed gem {seeds[i].DisplayName} (inventory full).");
            }
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
            GameEvents.RaiseInventoryChanged();
        }

        /// <summary>
        /// Plan-only: drag/drop reorders inventory gems. If destination is empty it moves,
        /// if occupied it swaps.
        /// </summary>
        public void RequestMoveOrSwapInventoryAt(int fromIndex, int toIndex)
        {
            if (Inventory == null)
                return;
            if (States.Current != RunStateId.Plan && States.Current != RunStateId.Combat)
                return;

            if (!Inventory.TryMoveOrSwapAt(fromIndex, toIndex))
                return;

            GameEvents.RaiseInventoryChanged();
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
                GameEvents.RaiseDraftOfferChanged();
                GameEvents.RaiseInventoryChanged();
                return;
            }

            if (Draft.ReplacePhase == DraftReplacePhase.AwaitingConfirm)
                Debug.Log("[GemTD] Bag full — ConfirmReplaceYes/No, then pick inventory slot.");
            GameEvents.RaiseDraftOfferChanged();
        }

        public void RequestDraftSkip()
        {
            if (States.Current != RunStateId.Draft || Draft == null || !Draft.IsActive)
                return;

            if (!Draft.TrySkip(Economy, runConfig != null ? runConfig.DraftSkipGold : 75, out var resolved) || !resolved)
                return;

            States.DraftResolved();
            GameEvents.RaiseDraftOfferChanged();
        }

        public void RequestDraftReplaceYes()
        {
            Draft?.ConfirmReplaceYes();
            GameEvents.RaiseDraftOfferChanged();
        }

        public void RequestDraftReplaceNo()
        {
            Draft?.ConfirmReplaceNo();
            GameEvents.RaiseDraftOfferChanged();
        }

        public void RequestDraftReplaceCancel()
        {
            Draft?.CancelReplace();
            GameEvents.RaiseDraftOfferChanged();
        }

        public void RequestDraftReplaceComplete(int inventoryIndex)
        {
            if (States.Current != RunStateId.Draft || Draft == null || Inventory == null)
                return;

            if (!Draft.TryCompleteReplace(inventoryIndex, Inventory, out var resolved) || !resolved)
                return;

            States.DraftResolved();
            GameEvents.RaiseDraftOfferChanged();
            GameEvents.RaiseInventoryChanged();
        }

        void SpawnEnemy(EnemyDefinition def)
        {
            if (def == null || chunkBoardView == null || _path == null)
                return;

            Vector2Int tip;
            if (def.IsBoss && _bossSpawnCursor < _bossSpawnTips.Count)
            {
                // Bosses spawn 1-per-tip from the pre-ranked furthest tips for this wave
                // (see PrepareBossSpawnTips) — not the regular round-robin scheduler.
                tip = _bossSpawnTips[_bossSpawnCursor];
                _bossSpawnCursor++;
            }
            else
            {
                _path.CollectSpawnTips(_spawnTips);
                if (_spawnTips.Count == 0)
                {
                    Debug.LogWarning("[GemTD] No spawn tips — cannot spawn.");
                    return;
                }

                tip = SpawnTipScheduler.Next(_spawnTips, ref _nextTipIndex);
            }

            if (!_path.TryGetWaypointPolyline(tip, _polylineCells))
                return;

            _polylineWorld.Clear();
            for (var i = 0; i < _polylineCells.Count; i++)
                _polylineWorld.Add(chunkBoardView.CellToWorld(_polylineCells[i]));

            var runtime = new EnemyRuntime();
            var endless = WaveController != null && WaveController.IsEndless;
            var hpScale = WaveScaling.HpScale(
                CurrentWaveNumber > 0 ? CurrentWaveNumber : 1,
                runConfig != null ? runConfig.GetHpMultiplier() : 1f,
                endless);
            runtime.Init(def, _polylineWorld, hpScale);
            _registry.Register(runtime);

            if (_enemyPool != null)
            {
                var view = _enemyPool.Get();
                view.Bind(runtime, _enemyHopDispatcher != null ? _enemyHopDispatcher.Scheduler : null);
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
                    if (enemy.LastDamageSource != null)
                        _runStats.RecordKill(enemy.LastDamageSource);

                    var killGold = enemy.Definition != null ? enemy.Definition.KillGold : 0;
                    if (killGold > 0 && enemy.Definition != null)
                    {
                        var endless = WaveController != null && WaveController.IsEndless;
                        if (enemy.Definition.IsBoss)
                            killGold = WaveScaling.ScaleBossBounty(
                                killGold, CurrentWaveNumber > 0 ? CurrentWaveNumber : 1, endless);
                        else
                            killGold = WaveScaling.ApplyEndlessGold(killGold, endless);
                    }
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

        void ApplyEnemyHopPlaybackSpeeds()
        {
            for (var i = 0; i < _enemyViews.Count; i++)
                _enemyViews[i]?.ApplyHopPlaybackSpeed();
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
            if (chunkBoardView == null || _path == null)
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

            _homeMarker.Bind(_path.Home, chunkBoardView.CellToWorld(_path.Home));
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
                        if (_legalChunks.Count > 0)
                            TryConfirmChunkExpand(_legalChunks[0]);
                        else
                        {
                            States.WaiveExpandRequirement();
                            StartWaveAfterExpand();
                        }
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

        void TryDebugFillBag()
        {
            if (_debugFillBag == null || !_debugFillBag.WasPressedThisFrame())
                return;
            if (Inventory == null)
                return;

            var filler = ResolveDebugFillGem();
            if (filler == null)
            {
                Debug.LogWarning("[GemTD] F6 fill bag: no gem definition on SeedGems or DraftPoolCatalog.");
                return;
            }

            var added = 0;
            while (Inventory.FreeSlotCount > 0)
            {
                if (!Inventory.TryAdd(filler))
                    break;
                added++;
            }

            Debug.Log($"[GemTD] F6 filled {added} bag slot(s) with {filler.DisplayName}. Free={Inventory.FreeSlotCount}.");
            if (added > 0)
                GameEvents.RaiseInventoryChanged();
        }

        GemDefinition ResolveDebugFillGem()
        {
            if (runConfig != null && runConfig.SeedGems != null)
            {
                for (var i = 0; i < runConfig.SeedGems.Length; i++)
                {
                    if (runConfig.SeedGems[i] != null)
                        return runConfig.SeedGems[i];
                }
            }

            if (draftPoolCatalog != null && draftPoolCatalog.Gems != null)
            {
                for (var i = 0; i < draftPoolCatalog.Gems.Length; i++)
                {
                    if (draftPoolCatalog.Gems[i] != null)
                        return draftPoolCatalog.Gems[i];
                }
            }

            return null;
        }
#endif
    }
}

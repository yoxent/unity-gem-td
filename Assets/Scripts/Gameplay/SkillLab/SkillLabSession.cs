using System.Collections.Generic;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.SkillLab
{
    public sealed class SkillLabSession
    {
        public const string StatusIdle = "";
        public const string StatusNoTarget = "No target in range.";
        public const string StatusTruncated = "Trace truncated.";

        static readonly TowerDefinition[] EmptyTowers = System.Array.Empty<TowerDefinition>();
        static readonly GemId[] EmptyDraftIds = System.Array.Empty<GemId>();

        readonly AttackTracer _tracer = new AttackTracer();
        readonly GemModifierPipeline _pipeline = new GemModifierPipeline();
        readonly CombatDirector _combat = new CombatDirector();
        readonly StatusRuntime _statuses = new StatusRuntime();
        readonly List<ISkillModifier> _scratch = new List<ISkillModifier>(8);
        readonly List<EnemyRuntime> _living = new List<EnemyRuntime>(DummyField.PinCount);
        readonly List<TowerInstance> _placed = new List<TowerInstance>(1);
        GemDefinition[] _catalog;
        GemId[] _draftGemIds = EmptyDraftIds;
        TowerDefinition[] _towers = EmptyTowers;
        int _stampedPayloadCount;
        bool _curseFieldActive;

        public DummyField Dummies { get; } = new DummyField();
        public TowerInstance Tower { get; private set; }
        public TowerDefinition[] Towers => _towers;
        public int SelectedTowerIndex { get; private set; } = -1;
        public Vector3 TowerPosition { get; set; } = DummyField.DefaultTowerPosition;
        public AttackTrace LastTrace { get; private set; } = new AttackTrace();
        public string Status { get; private set; } = StatusIdle;
        public IReadOnlyList<ProjectileRuntime> Projectiles => _combat.Projectiles;
        public IReadOnlyList<EffectPayloadRuntime> EffectPayloads => _combat.EffectPayloads;
        public StatusRuntime Statuses => _statuses;
        public bool HasActiveVolley => _combat.HasActiveVolley;

        public bool IsHydra => EvolutionEvaluator.IsHydraTower(Tower);

        public float Range
        {
            get
            {
                if (Tower == null || Tower.Def == null)
                    return 0f;
                var spec = _pipeline.Resolve(Tower, _scratch);
                var mul = spec.RangeMultiplier > 0.01f ? spec.RangeMultiplier : 1f;
                return Tower.Def.GetFireTowerRadius(Tower.Level) * mul;
            }
        }

        public void BindCatalog(GemDefinition[] catalog)
        {
            _catalog = catalog;
            RebuildDraftGemIds();
        }

        public void BindTowers(TowerDefinition[] towers)
        {
            if (towers == null || towers.Length == 0)
            {
                _towers = EmptyTowers;
                return;
            }

            var count = 0;
            for (var i = 0; i < towers.Length; i++)
            {
                if (IsSkillLabTower(towers[i]))
                    count++;
            }

            if (count == 0)
            {
                _towers = EmptyTowers;
                return;
            }

            var compact = new TowerDefinition[count];
            var n = 0;
            for (var i = 0; i < towers.Length; i++)
            {
                if (IsSkillLabTower(towers[i]))
                    compact[n++] = towers[i];
            }

            System.Array.Sort(compact, CompareTowerNames);
            _towers = compact;
        }

        public void SelectTower(int index)
        {
            if (index < 0 || index >= _towers.Length)
                return;
            var def = _towers[index];
            if (def == null)
                return;
            if (Tower != null && Tower.Def == def && SelectedTowerIndex == index)
                return;
            SetTowerDef(def);
        }

        public int IndexOfDisplayName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
                return -1;
            for (var i = 0; i < _towers.Length; i++)
            {
                if (_towers[i] != null && _towers[i].DisplayName == displayName)
                    return i;
            }

            return -1;
        }

        public void SetTowerDef(TowerDefinition def)
        {
            if (!IsSkillLabTower(def))
                return;
            Tower = new TowerInstance(Vector2Int.zero, def);
            Tower.MuzzleLocalY = TowerView.DefaultMuzzleLocalY;
            SelectedTowerIndex = IndexOfDef(def);
            _placed.Clear();
            _placed.Add(Tower);
            ClearOverlay();
        }

        public static string TowerLabel(TowerDefinition def)
        {
            if (def == null)
                return "";
            return string.IsNullOrEmpty(def.DisplayName) ? def.name : def.DisplayName;
        }

        public void SetSocket(int index, GemId id)
        {
            if (Tower == null || Tower.Sockets == null || index < 0 || index >= Tower.Sockets.Length)
                return;

            var current = Tower.Sockets[index];
            if (id == GemId.None)
            {
                if (current.IsEmpty)
                    return;
                Tower.TryUnsocket(index, out _, true, ignoreHydraLock: true);
                ClearOverlay();
                return;
            }

            if (!current.IsEmpty && current.Id == id)
                return;

            var gem = FindCatalog(id);
            if (gem == null)
                return;

            var instance = GemInstance.FromDefinition(gem);
            if (!GemTags.CanSocket(Tower.Def, instance))
                return;
            if (HasOtherSocket(index, id))
                return;

            Tower.TryUnsocket(index, out _, true, ignoreHydraLock: true);
            Tower.TrySocket(instance, index, true);
            ClearOverlay();
        }

        public GemDefinition CatalogGem(GemId id)
        {
            return FindCatalog(id);
        }

        public GemId[] DraftGemIds => _draftGemIds;

        public void Fire()
        {
            _combat.ClearProjectiles(keepDelayedStationaryPulses: true);
            _stampedPayloadCount = 0;
            _curseFieldActive = false;
            _statuses.Clear();
            _living.Clear();
            Dummies.CopyLiving(_living);
            LastTrace = _tracer.Trace(
                Tower,
                CombatMuzzle(),
                _living,
                payloadRng: null,
                includeRandomPayloads: false);
            if (!LastTrace.HasTarget)
            {
                Status = StatusNoTarget;
                return;
            }

            if (!_combat.TryFireOnce(Tower, TowerPosition, _living, _pipeline, _statuses, _placed))
            {
                LastTrace = new AttackTrace();
                Status = StatusNoTarget;
                return;
            }

            if (Tower != null && Tower.Def != null && Tower.Def.HasRole<CurseRoleDefinition>())
                _curseFieldActive = true;

            StampNewPayloads();
            if (LastTrace.Truncated)
                Status = StatusTruncated;
            else
                Status = StatusIdle;
        }

        public void TickVolley(float dt)
        {
            _living.Clear();
            Dummies.CopyLiving(_living);
            if (_curseFieldActive)
            {
                _statuses.ClearCurseHexes(_living);
                _combat.TryFireOnce(Tower, TowerPosition, _living, _pipeline, _statuses, _placed);
            }

            if (!_combat.HasActiveVolley)
                return;

            _combat.TickInFlight(dt, _living);
            StampNewPayloads();
        }

        public void QueueAnimationAction(TowerInstance tower, int fireGeneration, string action)
        {
            _combat.QueueAnimationAction(tower, fireGeneration, action);
        }

        public void ResolveQueuedAnimationActions()
        {
            _living.Clear();
            Dummies.CopyLiving(_living);
            _combat.ResolveQueuedAnimationActions(_living);
            StampNewPayloads();
        }

        public void StopVolley()
        {
            _combat.ClearProjectiles();
            _stampedPayloadCount = 0;
        }

        public void ClearOverlay()
        {
            StopVolley();
            _curseFieldActive = false;
            _statuses.Clear();
            LastTrace = new AttackTrace();
            Status = StatusIdle;
        }

        public void ResetPins()
        {
            StopVolley();
            Dummies.ResetPins();
        }

        void StampNewPayloads()
        {
            var payloads = _combat.EffectPayloads;
            if (payloads == null || LastTrace == null)
                return;

            _living.Clear();
            Dummies.CopyLiving(_living);
            for (var i = _stampedPayloadCount; i < payloads.Count; i++)
            {
                var payload = payloads[i];
                if (payload == null)
                    continue;
                _tracer.AppendPayload(LastTrace, payload.Plan, _living);
            }

            _stampedPayloadCount = payloads.Count;
        }

        void RebuildDraftGemIds()
        {
            if (_catalog == null || _catalog.Length == 0)
            {
                _draftGemIds = EmptyDraftIds;
                return;
            }

            var count = 0;
            for (var i = 0; i < _catalog.Length; i++)
            {
                var gem = _catalog[i];
                if (gem != null && gem.Id != GemId.None)
                    count++;
            }

            if (count == 0)
            {
                _draftGemIds = EmptyDraftIds;
                return;
            }

            var gems = new GemDefinition[count];
            var n = 0;
            for (var i = 0; i < _catalog.Length; i++)
            {
                var gem = _catalog[i];
                if (gem != null && gem.Id != GemId.None)
                    gems[n++] = gem;
            }

            System.Array.Sort(gems, CompareGemNames);
            var ids = new GemId[gems.Length];
            for (var i = 0; i < gems.Length; i++)
                ids[i] = gems[i].Id;
            _draftGemIds = ids;
        }

        GemDefinition FindCatalog(GemId id)
        {
            if (_catalog == null)
                return null;
            for (var i = 0; i < _catalog.Length; i++)
            {
                if (_catalog[i] != null && _catalog[i].Id == id)
                    return _catalog[i];
            }

            return null;
        }

        static int CompareGemNames(GemDefinition a, GemDefinition b)
        {
            var an = a != null && !string.IsNullOrEmpty(a.DisplayName) ? a.DisplayName : "";
            var bn = b != null && !string.IsNullOrEmpty(b.DisplayName) ? b.DisplayName : "";
            return string.CompareOrdinal(an, bn);
        }

        int IndexOfDef(TowerDefinition def)
        {
            if (def == null)
                return -1;
            for (var i = 0; i < _towers.Length; i++)
            {
                if (_towers[i] == def)
                    return i;
            }

            return -1;
        }

        static int CompareTowerNames(TowerDefinition a, TowerDefinition b)
        {
            var cmp = string.Compare(TowerLabel(a), TowerLabel(b), System.StringComparison.OrdinalIgnoreCase);
            if (cmp != 0)
                return cmp;
            return string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase);
        }

        Vector3 CombatMuzzle()
        {
            var muzzle = TowerPosition;
            if (Tower != null)
                muzzle.y += Tower.MuzzleLocalY;
            return muzzle;
        }

        static bool IsSkillLabTower(TowerDefinition tower)
        {
            return tower != null && tower.HasDamageRole;
        }

        bool HasOtherSocket(int exceptIndex, GemId id)
        {
            var sockets = Tower.Sockets;
            for (var i = 0; i < sockets.Length; i++)
            {
                if (i == exceptIndex)
                    continue;
                if (!sockets[i].IsEmpty && sockets[i].Id == id)
                    return true;
            }

            return false;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
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

        static readonly GemId[] DraftOrder =
        {
            GemId.Lmp, GemId.Chain, GemId.Fork, GemId.IncreasedArea, GemId.Pierce,
            GemId.Ignite, GemId.Chill, GemId.Shock, GemId.ElementalProliferation
        };

        readonly AttackTracer _tracer = new AttackTracer();
        readonly GemModifierPipeline _pipeline = new GemModifierPipeline();
        readonly List<IAttackModifier> _scratch = new List<IAttackModifier>(8);
        readonly List<EnemyRuntime> _living = new List<EnemyRuntime>(DummyField.PinCount);
        GemDefinition[] _catalog;

        public DummyField Dummies { get; } = new DummyField();
        public TowerRuntime Tower { get; private set; }
        public Vector3 TowerPosition { get; set; } = DummyField.DefaultTowerPosition;
        public AttackTrace LastTrace { get; private set; } = new AttackTrace();
        public string Status { get; private set; } = StatusIdle;

        public bool IsHydra => EvolutionEvaluator.IsHydraBallista(Tower);

        public float Range
        {
            get
            {
                if (Tower == null || Tower.Def == null)
                    return 0f;
                var spec = _pipeline.Resolve(Tower, _scratch);
                var mul = spec.RangeMultiplier > 0.01f ? spec.RangeMultiplier : 1f;
                return Tower.Def.Range * mul;
            }
        }

        public void BindCatalog(GemDefinition[] catalog)
        {
            _catalog = catalog;
        }

        public void SetTowerDef(TowerDefinition def)
        {
            Tower = new TowerRuntime(Vector2Int.zero, def);
            ClearOverlay();
        }

        public void SetSocket(int index, GemId id)
        {
            if (Tower == null || Tower.Sockets == null || index < 0 || index >= Tower.Sockets.Length)
                return;

            var current = Tower.Sockets[index];
            if (id == GemId.None)
            {
                if (current == null)
                    return;
                Tower.TryUnsocket(index, out _, true, ignoreHydraLock: true);
                ClearOverlay();
                return;
            }

            if (current != null && current.Id == id)
                return;

            var gem = FindCatalog(id);
            if (gem == null)
                return;
            if (!AttackTags.CanSocket(Tower.Def, gem))
                return;
            if (HasOtherSocket(index, id))
                return;

            Tower.TryUnsocket(index, out _, true, ignoreHydraLock: true);
            Tower.TrySocket(gem, index, true);
            ClearOverlay();
        }

        public GemDefinition CatalogGem(GemId id)
        {
            return FindCatalog(id);
        }

        public static GemId[] DraftGemIds => DraftOrder;

        public void Fire()
        {
            _living.Clear();
            Dummies.CopyLiving(_living);
            LastTrace = _tracer.Trace(Tower, TowerPosition, _living);
            if (!LastTrace.HasTarget)
                Status = StatusNoTarget;
            else if (LastTrace.Truncated)
                Status = StatusTruncated;
            else
                Status = StatusIdle;
        }

        public void ClearOverlay()
        {
            LastTrace = new AttackTrace();
            Status = StatusIdle;
        }

        public void ResetPins()
        {
            Dummies.ResetPins();
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

        bool HasOtherSocket(int exceptIndex, GemId id)
        {
            var sockets = Tower.Sockets;
            for (var i = 0; i < sockets.Length; i++)
            {
                if (i == exceptIndex)
                    continue;
                if (sockets[i] != null && sockets[i].Id == id)
                    return true;
            }

            return false;
        }
    }
}

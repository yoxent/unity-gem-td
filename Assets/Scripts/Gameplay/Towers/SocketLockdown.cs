using System.Collections.Generic;
using GemTD.Gameplay.Run;

namespace GemTD.Gameplay.Towers
{
    /// <summary>
    /// Per-tower Combat socket lockdown. Plan is always free; firing is unaffected.
    /// </summary>
    public sealed class SocketLockdown
    {
        readonly float _duration;
        readonly Dictionary<TowerRuntime, float> _remaining = new Dictionary<TowerRuntime, float>();
        readonly List<TowerRuntime> _scratchKeys = new List<TowerRuntime>(8);

        public SocketLockdown(float duration = 3f)
        {
            _duration = duration > 0f ? duration : 3f;
        }

        public void NotifyChanged(TowerRuntime tower, RunStateId state)
        {
            if (tower == null || state != RunStateId.Combat)
                return;

            _remaining[tower] = _duration;
        }

        public void Tick(float dt)
        {
            if (dt <= 0f || _remaining.Count == 0)
                return;

            _scratchKeys.Clear();
            foreach (var kv in _remaining)
                _scratchKeys.Add(kv.Key);

            for (var i = 0; i < _scratchKeys.Count; i++)
            {
                var tower = _scratchKeys[i];
                var left = _remaining[tower] - dt;
                if (left <= 0f)
                    _remaining.Remove(tower);
                else
                    _remaining[tower] = left;
            }
        }

        public bool CanSocket(TowerRuntime tower, RunStateId state)
        {
            if (tower == null)
                return false;

            if (state == RunStateId.Plan)
                return true;

            if (state != RunStateId.Combat)
                return false;

            return Remaining(tower) <= 0f;
        }

        public float Remaining(TowerRuntime tower)
        {
            if (tower == null || !_remaining.TryGetValue(tower, out var left))
                return 0f;
            return left > 0f ? left : 0f;
        }
    }
}

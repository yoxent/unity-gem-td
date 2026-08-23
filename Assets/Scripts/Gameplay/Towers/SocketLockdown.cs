using System.Collections.Generic;
using GemTD.Gameplay.Run;

namespace GemTD.Gameplay.Towers
{
    /// <summary>
    /// Optional per-tower Combat socket lockdown. Duration 0 disables it (current default).
    /// Plan is always free; firing is unaffected.
    /// </summary>
    public sealed class SocketLockdown
    {
        readonly float _duration;
        readonly Dictionary<TowerInstance, float> _remaining = new Dictionary<TowerInstance, float>();
        readonly List<TowerInstance> _scratchKeys = new List<TowerInstance>(8);

        public SocketLockdown(float duration = 0f)
        {
            _duration = duration > 0f ? duration : 0f;
        }

        public void NotifyChanged(TowerInstance tower, RunStateId state)
        {
            if (_duration <= 0f)
                return;
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

        public bool CanSocket(TowerInstance tower, RunStateId state)
        {
            if (tower == null)
                return false;

            if (_duration <= 0f)
                return state == RunStateId.Plan || state == RunStateId.Combat;

            if (state == RunStateId.Plan)
                return true;

            if (state != RunStateId.Combat)
                return false;

            return Remaining(tower) <= 0f;
        }

        public float Remaining(TowerInstance tower)
        {
            if (tower == null || !_remaining.TryGetValue(tower, out var left))
                return 0f;
            return left > 0f ? left : 0f;
        }
    }
}

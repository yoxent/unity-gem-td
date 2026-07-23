using System.Collections.Generic;
using UnityEngine;

namespace GemTD.Gameplay.Enemies
{
    public sealed class EnemyRuntime
    {
        EnemyDefinition _def;
        Vector3[] _waypoints;
        int _segmentIndex;
        bool _alive;

        public EnemyDefinition Definition => _def;
        public float Hp { get; private set; }
        public bool IsAlive => _alive;
        public Vector3 WorldPosition { get; private set; }

        public float Progress
        {
            get
            {
                if (_waypoints == null || _waypoints.Length <= 1)
                    return 0f;

                if (_segmentIndex >= _waypoints.Length - 1)
                    return _waypoints.Length - 1;

                var from = _waypoints[_segmentIndex];
                var to = _waypoints[_segmentIndex + 1];
                var segLen = Vector3.Distance(from, to);
                if (segLen <= 0f)
                    return _segmentIndex;

                var traveled = Vector3.Distance(from, WorldPosition);
                return _segmentIndex + traveled / segLen;
            }
        }

        public void Init(EnemyDefinition def, IReadOnlyList<Vector3> worldWaypoints)
        {
            _def = def;
            _alive = true;
            Hp = def != null ? def.MaxHealth : 0f;
            _segmentIndex = 0;

            if (worldWaypoints == null || worldWaypoints.Count == 0)
            {
                _waypoints = System.Array.Empty<Vector3>();
                WorldPosition = Vector3.zero;
                return;
            }

            _waypoints = new Vector3[worldWaypoints.Count];
            for (var i = 0; i < worldWaypoints.Count; i++)
                _waypoints[i] = worldWaypoints[i];

            WorldPosition = _waypoints[0];
        }

        public bool TickMove(float dt)
        {
            if (!_alive || _waypoints == null || _waypoints.Length < 2 || dt <= 0f)
                return false;

            var speed = _def != null ? _def.MoveSpeed : 0f;
            var remaining = speed * dt;

            while (remaining > 0f && _segmentIndex < _waypoints.Length - 1)
            {
                var target = _waypoints[_segmentIndex + 1];
                var delta = target - WorldPosition;
                var dist = delta.magnitude;

                if (dist <= remaining)
                {
                    WorldPosition = target;
                    _segmentIndex++;
                    remaining -= dist;

                    if (_segmentIndex >= _waypoints.Length - 1)
                        return true;
                }
                else
                {
                    WorldPosition += delta / dist * remaining;
                    remaining = 0f;
                }
            }

            return _segmentIndex >= _waypoints.Length - 1;
        }

        public void ApplyDamage(float dmg)
        {
            if (!_alive || dmg <= 0f)
                return;

            Hp -= dmg;
            if (Hp <= 0f)
            {
                Hp = 0f;
                _alive = false;
            }
        }
    }
}

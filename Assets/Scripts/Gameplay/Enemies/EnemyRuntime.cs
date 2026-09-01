using System.Collections.Generic;
using UnityEngine;

using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Enemies
{
    public sealed class EnemyRuntime
    {
        EnemyDefinition _def;
        Vector3[] _waypoints;
        int _segmentIndex;
        bool _alive;

        float _maxHealth;
        float _shieldMax;
        int _baseArmor;
        float _packMaxHealth;
        float _packShield;
        int _packArmor;
        float _packMoveSpeed;

        public EnemyDefinition Definition => _def;
        public TowerDefinition LastDamageSource { get; set; }
        public LocomotionStyle LocomotionStyle { get; private set; }
        public float HopHeight { get; private set; }
        public float HopPeriod { get; private set; }
        public float FlyHeight { get; private set; }
        public float FlyPeriod { get; private set; }
        public float Hp { get; private set; }
        public float ShieldHp { get; private set; }
        public float SpawnMaxHealth => _maxHealth;
        public float MaxHealth => _maxHealth + _packMaxHealth;
        public int Armor => _baseArmor + _packArmor;
        public int FireResistance { get; set; }
        public int ColdResistance { get; set; }
        public int LightningResistance { get; set; }
        public int ChaosResistance { get; set; }
        public EnemyAffix[] Affixes { get; private set; }
        public float CurrentMoveSpeed
        {
            get
            {
                var baseSpeed = _def != null ? _def.MoveSpeed : 0f;
                var multiplier = MoveSpeedMultiplier < 0f ? 0f : MoveSpeedMultiplier;
                return baseSpeed * multiplier * (1f + _packMoveSpeed);
            }
        }
        public float MoveSpeedMultiplier { get; set; }
        public bool Invulnerable { get; set; }
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

        public void Init(EnemyDefinition def, IReadOnlyList<Vector3> worldWaypoints, float healthScale = 1f)
        {
            _def = def;
            _alive = true;
            Invulnerable = false;
            if (healthScale < 0f)
                healthScale = 0f;
            _maxHealth = def != null ? def.MaxHealth * healthScale : 0f;
            Hp = _maxHealth;
            _shieldMax = def != null ? def.ShieldMax : 0f;
            ShieldHp = _shieldMax;
            _baseArmor = def != null ? def.Armor : 0;
            _packMaxHealth = 0f;
            _packShield = 0f;
            _packArmor = 0;
            _packMoveSpeed = 0f;
            FireResistance = def != null ? def.FireResistance : 0;
            ColdResistance = def != null ? def.ColdResistance : 0;
            LightningResistance = def != null ? def.LightningResistance : 0;
            ChaosResistance = def != null ? def.ChaosResistance : 0;
            Affixes = CopyAffixes(def != null ? def.Affixes : null);
            MoveSpeedMultiplier = 1f;
            LastDamageSource = null;
            _segmentIndex = 0;
            
            // Snapshot locomotion parameters at spawn time so view behavior cannot change later
            // if the underlying ScriptableObject fields are modified (in-editor or by tooling).
            if (def != null)
            {
                LocomotionStyle = def.Locomotion;
                HopHeight = def.HopHeight;
                HopPeriod = def.HopPeriod;
                FlyHeight = def.FlyHeight;
                FlyPeriod = def.FlyPeriod;
            }
            else
            {
                LocomotionStyle = LocomotionStyle.Slide;
                HopHeight = 0f;
                HopPeriod = 0f;
                FlyHeight = 0f;
                FlyPeriod = 0f;
            }

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

        public void SetWorldPosition(Vector3 position)
        {
            WorldPosition = position;
        }

        public bool TryGetPositionAfter(float seconds, out Vector3 point)
        {
            point = WorldPosition;
            if (!_alive || _waypoints == null || _waypoints.Length < 2)
                return false;
            if (seconds <= 0f)
                return true;

            var remaining = CurrentMoveSpeed * seconds;
            var pos = WorldPosition;
            var seg = _segmentIndex;
            while (remaining > 0f && seg < _waypoints.Length - 1)
            {
                var target = _waypoints[seg + 1];
                var delta = target - pos;
                var dist = delta.magnitude;
                if (dist <= remaining)
                {
                    pos = target;
                    seg++;
                    remaining -= dist;
                }
                else
                {
                    pos += delta / dist * remaining;
                    remaining = 0f;
                }
            }

            point = pos;
            return true;
        }

        public bool TickMove(float dt)
        {
            if (!_alive || _waypoints == null || _waypoints.Length < 2 || dt <= 0f)
                return false;

            var remaining = CurrentMoveSpeed * dt;

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

        public bool TryGetPathTangent(out Vector3 tangent)
        {
            tangent = Vector3.zero;
            if (_waypoints == null || _waypoints.Length < 2)
                return false;

            var seg = _segmentIndex;
            if (seg >= _waypoints.Length - 1)
                seg = _waypoints.Length - 2;
            if (seg < 0)
                return false;

            var delta = _waypoints[seg + 1] - _waypoints[seg];
            delta.y = 0f;
            var mag = delta.magnitude;
            if (mag <= 1e-5f)
                return false;

            tangent = delta / mag;
            return true;
        }

        public void ApplyPackBonuses(int armor, float extraMaxHealth, float extraShield, float extraMoveSpeed)
        {
            if (extraMaxHealth < 0f)
                extraMaxHealth = 0f;
            if (extraShield < 0f)
                extraShield = 0f;
            if (extraMoveSpeed < 0f)
                extraMoveSpeed = 0f;
            if (armor < 0)
                armor = 0;

            var healthDelta = extraMaxHealth - _packMaxHealth;
            _packMaxHealth = extraMaxHealth;
            if (healthDelta > 0f)
                Hp += healthDelta;
            if (Hp > MaxHealth)
                Hp = MaxHealth;

            var shieldDelta = extraShield - _packShield;
            _packShield = extraShield;
            if (shieldDelta > 0f)
                ShieldHp += shieldDelta;
            var shieldCap = _shieldMax + _packShield;
            if (ShieldHp > shieldCap)
                ShieldHp = shieldCap;

            _packArmor = armor;
            _packMoveSpeed = extraMoveSpeed;
        }

        public void ApplyDamage(float dmg)
        {
            ApplyDamage(dmg, default, null);
        }

        public void ApplyDamage(float dmg, SkillSpec spec, StatusRuntime statuses)
        {
            if (!_alive || dmg <= 0f || Invulnerable)
                return;

            var remaining = IncomingHit.Mitigate(dmg, spec, this, statuses);

            if (ShieldHp > 0f && remaining > 0f)
            {
                if (remaining >= ShieldHp)
                {
                    remaining -= ShieldHp;
                    ShieldHp = 0f;
                }
                else
                {
                    ShieldHp -= remaining;
                    remaining = 0f;
                }
            }

            if (remaining > 0f)
            {
                Hp -= remaining;
                if (Hp <= 0f)
                {
                    Hp = 0f;
                    _alive = false;
                }
            }
        }

        public void KnockbackAlongPath(float worldDistance)
        {
            if (!_alive || _waypoints == null || _waypoints.Length < 2 || worldDistance <= 0f)
                return;

            var remaining = worldDistance;
            while (remaining > 0f)
            {
                var from = _waypoints[_segmentIndex];
                var distToFrom = Vector3.Distance(WorldPosition, from);
                if (distToFrom > 1e-5f)
                {
                    if (remaining < distToFrom)
                    {
                        WorldPosition += (from - WorldPosition) * (remaining / distToFrom);
                        return;
                    }

                    WorldPosition = from;
                    remaining -= distToFrom;
                }

                if (_segmentIndex <= 0)
                {
                    WorldPosition = _waypoints[0];
                    _segmentIndex = 0;
                    return;
                }

                _segmentIndex--;
            }
        }

        static EnemyAffix[] CopyAffixes(EnemyAffix[] source)
        {
            if (source == null || source.Length == 0)
                return System.Array.Empty<EnemyAffix>();

            var copy = new EnemyAffix[source.Length];
            for (var i = 0; i < source.Length; i++)
                copy[i] = source[i];
            return copy;
        }
    }
}

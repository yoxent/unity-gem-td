using System.Collections.Generic;
using UnityEngine;

using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Run;
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
        public EnemyTag Tags { get; private set; }
        public float BlinkDistance { get; private set; }
        public float BlinkInterval { get; private set; }
        float _blinkTimer;
        public float Hp { get; private set; }
        public float ShieldHp { get; private set; }
        public float SpawnMaxHealth => _maxHealth;
        public float MaxHealth => _maxHealth + _packMaxHealth;
        public float ShieldMax => _shieldMax + _packShield;
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

        /// <summary>
        /// Armor rating multiplier. Same three moments as health, with smaller steps.
        /// Enemies authored at 0 armor stay at 0.
        /// </summary>
        public static int ScaledArmor(int armor, float armorScale)
        {
            if (armor <= 0 || armorScale <= 0f)
                return 0;
            return Mathf.RoundToInt(armor * armorScale);
        }

        static EnemyAffix[] CopyAffixes(EnemyDefinition def)
        {
            var source = def != null ? def.Affixes : null;
            if (source == null || source.Length == 0)
                return System.Array.Empty<EnemyAffix>();

            var copy = new EnemyAffix[source.Length];
            for (var i = 0; i < source.Length; i++)
                copy[i] = source[i];
            return copy;
        }

        public static int ScaleResist(int authored, int bonus)
        {
            if (authored == 0)
                return 0;
            if (authored < 0)
                return authored;

            var value = authored + (bonus > 0 ? bonus : 0);
            if (value > WaveScaling.ResistCap)
                value = WaveScaling.ResistCap;
            return value < authored ? authored : value;
        }

        public void Init(
            EnemyDefinition def,
            IReadOnlyList<Vector3> worldWaypoints,
            float healthScale = 1f,
            float speedScale = 1f,
            float armorScale = 1f,
            int resistBonus = 0)
        {
            _def = def;
            _alive = true;
            Invulnerable = false;
            if (healthScale < 0f)
                healthScale = 0f;
            _maxHealth = def != null ? def.MaxHealth * healthScale : 0f;
            Hp = _maxHealth;
            _shieldMax = def != null ? def.ShieldMax * healthScale : 0f;
            ShieldHp = _shieldMax;
            _baseArmor = ScaledArmor(def != null ? def.Armor : 0, armorScale);
            _packMaxHealth = 0f;
            _packShield = 0f;
            _packArmor = 0;
            _packMoveSpeed = 0f;
            FireResistance = ScaleResist(def != null ? def.FireResistance : 0, resistBonus);
            ColdResistance = ScaleResist(def != null ? def.ColdResistance : 0, resistBonus);
            LightningResistance = ScaleResist(def != null ? def.LightningResistance : 0, resistBonus);
            ChaosResistance = ScaleResist(def != null ? def.ChaosResistance : 0, resistBonus);
            Affixes = CopyAffixes(def);
            MoveSpeedMultiplier = speedScale > 0f ? speedScale : 0f;
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
                Tags = def.Tags;
                BlinkDistance = def.ResolveBlinkDistance();
                BlinkInterval = def.ResolveBlinkInterval();
            }
            else
            {
                LocomotionStyle = LocomotionStyle.Slide;
                HopHeight = 0f;
                HopPeriod = 0f;
                FlyHeight = 0f;
                FlyPeriod = 0f;
                Tags = EnemyTag.None;
                BlinkDistance = 0f;
                BlinkInterval = 0f;
            }

            _blinkTimer = 0f;

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

            var pos = WorldPosition;
            var seg = _segmentIndex;
            var timer = _blinkTimer;
            var timeLeft = seconds;
            var speed = CurrentMoveSpeed;
            var blinks = BlinkReady;

            while (timeLeft > 1e-6f && seg < _waypoints.Length - 1)
            {
                var timeToBlink = float.PositiveInfinity;
                if (blinks)
                {
                    timeToBlink = BlinkInterval - timer;
                    if (timeToBlink < 0f)
                        timeToBlink = 0f;
                }

                var target = _waypoints[seg + 1];
                var delta = target - pos;
                var dist = delta.magnitude;
                var timeToNode = speed > 1e-5f ? dist / speed : float.PositiveInfinity;

                var step = timeLeft;
                if (timeToBlink < step)
                    step = timeToBlink;
                if (timeToNode < step)
                    step = timeToNode;
                if (step < 0f)
                    step = 0f;

                if (step > 0f && dist > 1e-6f && speed > 0f)
                {
                    var move = speed * step;
                    if (move >= dist)
                    {
                        pos = target;
                        seg++;
                    }
                    else
                        pos += delta / dist * move;
                }
                else if (dist <= 1e-6f)
                    seg++;

                timeLeft -= step;
                if (blinks)
                    timer += step;

                if (blinks && timer >= BlinkInterval - 1e-4f)
                {
                    timer -= BlinkInterval;
                    if (AdvanceAlong(_waypoints, ref seg, ref pos, BlinkDistance))
                    {
                        point = pos;
                        return true;
                    }
                }

                if (step <= 1e-8f && dist > 1e-6f && !(blinks && timeToBlink <= 1e-8f))
                    break;
            }

            point = pos;
            return true;
        }

        public bool TickMove(float dt)
        {
            if (!_alive || _waypoints == null || _waypoints.Length < 2 || dt <= 0f)
                return false;

            var position = WorldPosition;
            var segment = _segmentIndex;
            if (AdvanceAlong(_waypoints, ref segment, ref position, CurrentMoveSpeed * dt))
            {
                WorldPosition = position;
                _segmentIndex = segment;
                return true;
            }

            if (BlinkReady)
            {
                _blinkTimer += dt;
                while (_blinkTimer >= BlinkInterval)
                {
                    _blinkTimer -= BlinkInterval;
                    if (AdvanceAlong(_waypoints, ref segment, ref position, BlinkDistance))
                    {
                        WorldPosition = position;
                        _segmentIndex = segment;
                        return true;
                    }
                }
            }

            WorldPosition = position;
            _segmentIndex = segment;
            return segment >= _waypoints.Length - 1;
        }

        bool BlinkReady => EnemyTags.Has(Tags, EnemyTag.Blink);

        static bool AdvanceAlong(Vector3[] waypoints, ref int segmentIndex, ref Vector3 position, float distance)
        {
            var remaining = distance;
            while (remaining > 0f && segmentIndex < waypoints.Length - 1)
            {
                var target = waypoints[segmentIndex + 1];
                var delta = target - position;
                var dist = delta.magnitude;

                if (dist <= remaining)
                {
                    position = target;
                    segmentIndex++;
                    remaining -= dist;

                    if (segmentIndex >= waypoints.Length - 1)
                        return true;
                }
                else
                {
                    if (dist > 1e-8f)
                        position += delta / dist * remaining;
                    remaining = 0f;
                }
            }

            return segmentIndex >= waypoints.Length - 1;
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

    }
}

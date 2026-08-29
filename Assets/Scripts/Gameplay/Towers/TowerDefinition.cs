using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Gems;

namespace GemTD.Gameplay.Towers
{
    [CreateAssetMenu(menuName = "Gem TD/Tower Definition", fileName = "Tower_")]
    public sealed class TowerDefinition : ScriptableObject
    {
        public string DisplayName = "Tower";
        public int Cost = 50;
        [Tooltip("Added per same-type tower already on map. placeCost = Cost + BuildIncrement × countOnMap")]
        public int BuildIncrement = 25;
        public float Damage = 10f;
        [Tooltip("Skill-gem bucket payloads. Add only what this tower uses (Smite = Attack + Aura).")]
        public TowerRoleDefinition[] Roles;
        public int SocketCount = 3;
        public bool AllowsHydraEvolution;
        [Tooltip("PoE-style tags. None = infer from the assigned role types.")]
        public GemTag Tags = GemTag.None;

        public T GetRole<T>() where T : TowerRoleDefinition
        {
            if (Roles == null)
                return null;
            for (var i = 0; i < Roles.Length; i++)
            {
                if (Roles[i] is T match)
                    return match;
            }

            return null;
        }

        public bool HasRole<T>() where T : TowerRoleDefinition =>
            GetRole<T>() != null;

        public bool IsFireable => FireRole != null;

        public bool IsAuraOnly =>
            GetRole<AuraRoleDefinition>() != null && FireRole == null;

        public bool HasDamageRole => FireRole is DamageRoleDefinition;

        /// <summary>Role that drives fire cadence. Attack wins if present (Smite). Aura never fires.</summary>
        public TowerRoleDefinition FireRole
        {
            get
            {
                var attack = GetRole<AttackRoleDefinition>();
                if (attack != null)
                    return attack;
                if (Roles == null)
                    return null;
                for (var i = 0; i < Roles.Length; i++)
                {
                    var role = Roles[i];
                    if (role != null && role.BaseFireInterval > 0f)
                        return role;
                }

                return null;
            }
        }

        public float BaseFireInterval => GetBaseFireInterval(TowerInstance.DefaultLevel);

        public float GetBaseFireInterval(int sourceLevel)
        {
            var fire = FireRole;
            return fire != null ? fire.GetBaseFireInterval(sourceLevel) : 0f;
        }

        public float BaseActionsPerSecond =>
            GetBaseActionsPerSecond(TowerInstance.DefaultLevel);

        public float GetBaseActionsPerSecond(int sourceLevel)
        {
            var interval = GetBaseFireInterval(sourceLevel);
            return interval > 0f ? 1f / interval : 0f;
        }

        public float GetFireTowerRadius(int sourceLevel)
        {
            var fire = FireRole;
            return fire != null ? fire.GetTowerRadius(sourceLevel) : 0f;
        }

        public float GetAuraTowerRadius(int sourceLevel)
        {
            var aura = GetRole<AuraRoleDefinition>();
            return aura != null ? aura.GetTowerRadius(sourceLevel) : 0f;
        }

        public float GetProjectileSpeedMultiplier(int sourceLevel)
        {
            if ((Tags & GemTag.Projectile) == 0)
                return 1f;

            var fire = FireRole;
            if (fire == null)
                return 1f;

            var speed = fire.ResolveStat(RoleStat.ProjectileSpeed, sourceLevel);
            return speed > 0.01f ? speed : 1f;
        }

        public PierceMode GetProjectilePierceMode()
        {
            if ((Tags & GemTag.Projectile) == 0)
                return PierceMode.Finite;

            var damageRole = FireRole as DamageRoleDefinition;
            return damageRole != null ? damageRole.PierceBehavior : PierceMode.Finite;
        }

        public AimMode GetAimMode()
        {
            var damageRole = FireRole as DamageRoleDefinition;
            return damageRole != null ? damageRole.AimMode : AimMode.Direct;
        }

        public DeliveryPattern GetDeliveryPattern()
        {
            var damageRole = FireRole as DamageRoleDefinition;
            return damageRole != null ? damageRole.DeliveryPattern : DeliveryPattern.Straight;
        }

        public float GetPlacementTowerRadius(int sourceLevel)
        {
            if (FireRole != null)
                return GetFireTowerRadius(sourceLevel);
            return GetAuraTowerRadius(sourceLevel);
        }

        public RoleStatValue GetDamageRange(int sourceLevel)
        {
            var damageRole = FireRole as DamageRoleDefinition;
            if (damageRole != null)
            {
                var resolved = damageRole.ResolveStatValue(RoleStat.Damage, sourceLevel);
                if (resolved.Min > 0f || resolved.Max > 0f)
                    return resolved;
            }

            return RoleStatValue.FromSingle(Damage);
        }

        public float GetSplashRadius(int sourceLevel)
        {
            var damageRole = FireRole as DamageRoleDefinition;
            if (damageRole == null)
                return 0f;

            return damageRole.ResolveStat(RoleStat.SplashRadius, sourceLevel);
        }

        public int GetProjectileCount(int sourceLevel)
        {
            var fire = FireRole;
            return fire != null ? fire.GetProjectileCount(sourceLevel) : 0;
        }

        public int GetChainCount(int sourceLevel)
        {
            var fire = FireRole;
            return fire != null ? fire.GetChainCount(sourceLevel) : 0;
        }

        public EffectPayloadDefinition[] GetEffectPayloads()
        {
            var fire = FireRole;
            if (fire?.EffectPayloads == null || fire.EffectPayloads.Length == 0)
                return System.Array.Empty<EffectPayloadDefinition>();
            return fire.EffectPayloads;
        }

        public bool UsesAttackSpeed =>
            FireRole == null || FireRole.UsesAttackSpeed;

        public float FireInterval(in SkillSpec spec)
        {
            return FireInterval(spec, TowerInstance.DefaultLevel);
        }

        public float FireInterval(in SkillSpec spec, int sourceLevel)
        {
            var gemMul = UsesAttackSpeed ? spec.AttackSpeedMultiplier : spec.CastSpeedMultiplier;
            gemMul *= spec.FireRateMultiplier > 0.01f ? spec.FireRateMultiplier : 1f;
            gemMul = Mathf.Max(0.01f, gemMul);
            var baseline = GetBaseFireInterval(sourceLevel);
            if (baseline <= 0f)
                return 0f;
            return baseline / gemMul;
        }
    }
}

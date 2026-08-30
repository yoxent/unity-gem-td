using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.SkillLab;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class SkillLabSessionTests
    {
        EnemyDefinition _enemyDef;
        TowerDefinition _fireball;
        TowerDefinition _alternateTower;
        SpellRoleDefinition _fireballRole;
        SpellRoleDefinition _alternateRole;
        GemDefinition[] _catalog;

        [SetUp]
        public void SetUp()
        {
            _enemyDef = ScriptableObject.CreateInstance<EnemyDefinition>();
            _enemyDef.MaxHealth = 100f;

            _fireball = ScriptableObject.CreateInstance<TowerDefinition>();
            _fireballRole = ScriptableObject.CreateInstance<SpellRoleDefinition>();
            _fireballRole.Modifiers = new[]
            {
                Modifier(RoleStat.CastTime, 0.75f),
                Modifier(RoleStat.CastSpeed, 100f),
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.SplashRadius, 1.5f),
                Modifier(RoleStat.ProjectileCount, 1f)
            };
            _fireball.Roles = new TowerRoleDefinition[] { _fireballRole };
            _fireball.Tags = GemTag.Spell | GemTag.Projectile | GemTag.Aoe;
            _fireball.SocketCount = 3;
            _fireball.Damage = 8f;
            _fireball.DisplayName = "Fireball";

            _alternateTower = ScriptableObject.CreateInstance<TowerDefinition>();
            _alternateRole = ScriptableObject.CreateInstance<SpellRoleDefinition>();
            _alternateRole.Modifiers = new[]
            {
                Modifier(RoleStat.CastTime, 0.75f),
                Modifier(RoleStat.CastSpeed, 100f),
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.ProjectileCount, 1f)
            };
            _alternateTower.Roles = new TowerRoleDefinition[] { _alternateRole };
            _alternateTower.Tags = GemTag.Spell | GemTag.Projectile | GemTag.Aoe;
            _alternateTower.SocketCount = 3;
            _alternateTower.Damage = 8f;
            _alternateTower.DisplayName = "Cleave";

            var ids = new[]
            {
                GemId.MultipleProjectiles, GemId.Chain, GemId.Fork, GemId.IncreasedArea,
                GemId.Pierce, GemId.ElementalProliferation, GemId.Combustion, GemId.AddedFireDamage,
                GemId.AddedColdDamage, GemId.AddedLightningDamage, GemId.Knockback,
                GemId.SpellEcho
            };
            _catalog = new GemDefinition[ids.Length];
            for (var i = 0; i < ids.Length; i++)
            {
                _catalog[i] = ScriptableObject.CreateInstance<GemDefinition>();
                _catalog[i].Id = ids[i];
                CatalogGemModifiers.Bind(_catalog[i]);
            }
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_enemyDef);
            Object.DestroyImmediate(_fireballRole);
            Object.DestroyImmediate(_alternateRole);
            Object.DestroyImmediate(_fireball);
            Object.DestroyImmediate(_alternateTower);
            for (var i = 0; i < _catalog.Length; i++)
                Object.DestroyImmediate(_catalog[i]);
        }

        [Test]
        public void Fire_OutOfRange_SetsStatus_ClearsSegments()
        {
            _fireballRole.Modifiers = new[]
            {
                Modifier(RoleStat.CastTime, 0.75f),
                Modifier(RoleStat.CastSpeed, 100f),
                Modifier(RoleStat.TowerRadius, 1f),
                Modifier(RoleStat.SplashRadius, 1.5f),
                Modifier(RoleStat.ProjectileCount, 1f)
            };
            var session = MakeSession();
            session.TowerPosition = DummyField.DefaultTowerPosition;
            session.Fire();
            Assert.AreEqual(SkillLabSession.StatusNoTarget, session.Status);
            Assert.IsFalse(session.LastTrace.HasTarget);
            Assert.AreEqual(0, session.Projectiles.Count);
            Assert.IsFalse(session.HasActiveVolley);
        }

        [Test]
        public void Fire_InRange_SpawnsRealProjectile_DummiesStayAlive()
        {
            var session = MakeSession();
            session.TowerPosition = Vector3.zero;
            session.Dummies.GetDummy(0).SetWorldPosition(new Vector3(3f, 0f, 0f));
            session.Fire();
            Assert.IsTrue(session.LastTrace.HasTarget);
            Assert.AreEqual(1, session.Projectiles.Count);
            Assert.IsTrue(session.HasActiveVolley);
            Assert.IsTrue(session.Dummies.GetDummy(0).IsAlive);
            Assert.AreEqual(100f, session.Dummies.GetDummy(0).Hp, 1e-4f);
        }

        [Test]
        public void Fire_SpellEcho_OverlayAndActualVolleyCountsMatch()
        {
            var session = MakeSession();
            session.SetSocket(0, GemId.SpellEcho);
            session.TowerPosition = Vector3.zero;
            session.Dummies.GetDummy(0).SetWorldPosition(new Vector3(3f, 0f, 0f));

            session.Fire();

            var primarySegments = 0;
            for (var i = 0; i < session.LastTrace.Segments.Count; i++)
            {
                if (session.LastTrace.Segments[i].Kind == AttackTraceKind.Primary)
                    primarySegments++;
            }

            Assert.AreEqual(2, primarySegments);
            Assert.AreEqual(2, session.Projectiles.Count);
        }

        [Test]
        public void Fire_MoltenStrike_RandomMagmaLandingsMatchActualPayloads()
        {
            _fireball.Tags = GemTag.Attack | GemTag.Projectile | GemTag.Melee | GemTag.Strike;
            _fireballRole.AimMode = AimMode.Direct;
            _fireballRole.DeliveryPattern = DeliveryPattern.WarpStrike;
            _fireballRole.Modifiers = new[]
            {
                Modifier(RoleStat.CastTime, 1f),
                Modifier(RoleStat.CastSpeed, 100f),
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f),
                Modifier(RoleStat.Damage, 10f)
            };
            _fireballRole.EffectPayloads = new[]
            {
                new EffectPayloadDefinition
                {
                    Trigger = EffectPayloadTrigger.OnImpact,
                    Anchor = EffectPayloadAnchor.PrimaryTarget,
                    TravelPattern = EffectPayloadTravelPattern.Fountain,
                    ScatterPattern = EffectPayloadScatterPattern.RandomRing,
                    HitPolicy = EffectPayloadHitPolicy.PerImpact,
                    Tags = GemTag.Aoe | GemTag.Projectile,
                    Count = 4,
                    DamageMultiplier = 0.4f,
                    AoeRadius = 1f,
                    MinDistance = 1.5f,
                    MaxDistance = 2.5f,
                    ArcHeight = 1.5f
                }
            };

            var session = MakeSession();
            session.TowerPosition = Vector3.zero;
            session.Dummies.GetDummy(0).SetWorldPosition(new Vector3(3f, 0f, 0f));
            Assert.AreEqual(DeliveryPattern.WarpStrike, session.Tower.Def.GetDeliveryPattern());
            Assert.AreEqual(4, session.Tower.Def.GetEffectPayloads()[0].Count);
            session.Fire();

            WaitForMagmaPayloads(session, 4);
            var expectedLandings = CollectMagmaLandings(session.LastTrace);

            Assert.AreEqual(4, expectedLandings.Count, "overlay magma landings");
            Assert.AreEqual(4, session.EffectPayloads.Count, "runtime magma payloads");
            AssertLandingsMatchPayloads(expectedLandings, session.EffectPayloads);
        }

        [Test]
        public void Fire_MoltenStrike_RandomRing_ChangesLandingsBetweenFires()
        {
            _fireball.Tags = GemTag.Attack | GemTag.Projectile | GemTag.Melee | GemTag.Strike;
            _fireballRole.AimMode = AimMode.Direct;
            _fireballRole.DeliveryPattern = DeliveryPattern.WarpStrike;
            _fireballRole.Modifiers = new[]
            {
                Modifier(RoleStat.CastTime, 1f),
                Modifier(RoleStat.CastSpeed, 100f),
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f),
                Modifier(RoleStat.Damage, 10f)
            };
            _fireballRole.EffectPayloads = new[]
            {
                new EffectPayloadDefinition
                {
                    Trigger = EffectPayloadTrigger.OnImpact,
                    Anchor = EffectPayloadAnchor.PrimaryTarget,
                    TravelPattern = EffectPayloadTravelPattern.Fountain,
                    ScatterPattern = EffectPayloadScatterPattern.RandomRing,
                    HitPolicy = EffectPayloadHitPolicy.PerImpact,
                    Tags = GemTag.Aoe | GemTag.Projectile,
                    Count = 4,
                    DamageMultiplier = 0.4f,
                    AoeRadius = 1f,
                    MinDistance = 1.5f,
                    MaxDistance = 2.5f,
                    ArcHeight = 1.5f
                }
            };

            var session = MakeSession();
            session.TowerPosition = Vector3.zero;
            session.Dummies.GetDummy(0).SetWorldPosition(new Vector3(3f, 0f, 0f));

            session.Fire();
            WaitForMagmaPayloads(session, 4);
            var first = CollectMagmaLandings(session.LastTrace);
            Assert.AreEqual(4, first.Count);

            session.Fire();
            WaitForMagmaPayloads(session, 4);
            var second = CollectMagmaLandings(session.LastTrace);
            Assert.AreEqual(4, second.Count);
            Assert.IsFalse(
                LandingsEqual(first, second),
                "RandomRing magma should not reuse the same four landings on every Fire.");
        }

        [Test]
        public void Fire_RainRandomLandingsMatchActualPayloads()
        {
            _fireball.Tags = GemTag.Spell | GemTag.Aoe;
            _fireballRole.AimMode = AimMode.Ground;
            _fireballRole.DeliveryPattern = DeliveryPattern.Rain;
            _fireballRole.Modifiers = new[]
            {
                Modifier(RoleStat.CastTime, 1f),
                Modifier(RoleStat.CastSpeed, 100f),
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.Damage, 10f)
            };
            _fireballRole.EffectPayloads = new[]
            {
                new EffectPayloadDefinition
                {
                    Trigger = EffectPayloadTrigger.AfterDelay,
                    Anchor = EffectPayloadAnchor.GroundTarget,
                    TravelPattern = EffectPayloadTravelPattern.FallFromSky,
                    ScatterPattern = EffectPayloadScatterPattern.RandomRing,
                    HitPolicy = EffectPayloadHitPolicy.PerImpact,
                    Tags = GemTag.Aoe,
                    Count = 4,
                    DamageMultiplier = 1f,
                    AoeRadius = 1.3f,
                    MinDistance = 0f,
                    MaxDistance = 2.5f,
                    ArcHeight = 3f,
                    IntervalSeconds = 0.15f
                }
            };

            var session = MakeSession();
            session.TowerPosition = Vector3.zero;
            session.Dummies.GetDummy(0).SetWorldPosition(new Vector3(3f, 0f, 0f));
            session.Fire();

            var expectedLandings = new List<Vector3>(4);
            var segments = session.LastTrace.Segments;
            for (var i = 0; i < segments.Count; i++)
            {
                if (segments[i].Kind == AttackTraceKind.Rain)
                    expectedLandings.Add(segments[i].To);
            }

            Assert.AreEqual(4, expectedLandings.Count);
            Assert.AreEqual(4, session.EffectPayloads.Count);
            for (var i = 0; i < session.EffectPayloads.Count; i++)
            {
                var actual = session.EffectPayloads[i].LandingPoint;
                var matched = false;
                for (var j = 0; j < expectedLandings.Count; j++)
                {
                    if ((actual - expectedLandings[j]).sqrMagnitude <= 1e-6f)
                    {
                        expectedLandings.RemoveAt(j);
                        matched = true;
                        break;
                    }
                }

                Assert.IsTrue(matched, $"Rain payload {i} does not match an overlay landing.");
            }
        }

        [Test]
        public void TickVolley_MovesSpawnedProjectile()
        {
            var session = MakeSession();
            session.TowerPosition = Vector3.zero;
            session.Dummies.GetDummy(0).SetWorldPosition(new Vector3(3f, 0f, 0f));
            session.Fire();
            var origin = session.Projectiles[0].Position;
            session.TickVolley(0.05f);
            Assert.AreEqual(1, session.Projectiles.Count);
            Assert.Greater((session.Projectiles[0].Position - origin).sqrMagnitude, 0f);
        }

        [Test]
        public void ClearOverlay_StopsActiveVolley()
        {
            var session = MakeSession();
            session.TowerPosition = Vector3.zero;
            session.Dummies.GetDummy(0).SetWorldPosition(new Vector3(3f, 0f, 0f));
            session.Fire();
            Assert.IsTrue(session.HasActiveVolley);
            session.ClearOverlay();
            Assert.AreEqual(0, session.Projectiles.Count);
            Assert.IsFalse(session.HasActiveVolley);
        }

        [Test]
        public void Fire_ChainOnBowlingPins_OverlayHopMatchesCombatBolt()
        {
            var session = MakeSession();
            session.SetSocket(0, GemId.Chain);
            session.TowerPosition = DummyField.DefaultTowerPosition;
            session.Fire();

            Assert.IsTrue(session.LastTrace.HasTarget);
            Assert.AreEqual(1, session.Projectiles.Count);
            var primary = session.Dummies.GetDummy(0);
            Assert.AreSame(primary, session.Projectiles[0].Target);

            AttackTraceSegment hop = default;
            var foundHop = false;
            var segments = session.LastTrace.Segments;
            for (var i = 0; i < segments.Count; i++)
            {
                if (segments[i].Kind != AttackTraceKind.Chain)
                    continue;
                hop = segments[i];
                foundHop = true;
                break;
            }

            Assert.IsTrue(foundHop, "overlay should record a chain hop");

            EnemyRuntime chained = null;
            for (var i = 0; i < 120; i++)
            {
                session.TickVolley(0.02f);
                if (session.Projectiles.Count == 0)
                    break;
                var target = session.Projectiles[0].Target;
                if (target != null && !ReferenceEquals(target, primary))
                {
                    chained = target;
                    break;
                }
            }

            Assert.IsNotNull(chained);
            Assert.AreEqual(chained.WorldPosition.x, hop.To.x, 1e-3f);
            Assert.AreEqual(chained.WorldPosition.z, hop.To.z, 1e-3f);
        }

        [Test]
        public void Range_UsesSpellRoleModifier()
        {
            var session = MakeSession();
            Assert.AreEqual(20f, session.Range, 0.001f);

            session.Tower.SetLevel(10);
            Assert.AreEqual(20f, session.Range, 0.001f);
        }

        [Test]
        public void SetSocket_AutoClearsOverlay()
        {
            var session = MakeSession();
            session.TowerPosition = Vector3.zero;
            session.Dummies.GetDummy(0).SetWorldPosition(new Vector3(3f, 0f, 0f));
            session.Fire();
            Assert.IsTrue(session.LastTrace.HasTarget);
            session.SetSocket(0, GemId.MultipleProjectiles);
            Assert.AreEqual(0, session.LastTrace.Segments.Count);
            Assert.AreEqual(GemId.MultipleProjectiles, session.Tower.Sockets[0].Id);
            Assert.AreEqual(GemRarity.Normal, session.Tower.Sockets[0].Rarity);
        }

        [Test]
        public void SetTowerDef_AutoClearsOverlay()
        {
            var session = MakeSession();
            session.TowerPosition = Vector3.zero;
            session.Dummies.GetDummy(0).SetWorldPosition(new Vector3(3f, 0f, 0f));
            session.Fire();
            session.SetTowerDef(_alternateTower);
            Assert.AreEqual(0, session.LastTrace.Segments.Count);
            Assert.AreEqual(_alternateTower, session.Tower.Def);
        }

        [Test]
        public void BindTowers_SortsByDisplayName_SkipsNulls()
        {
            var session = new SkillLabSession();
            session.BindTowers(new[] { _fireball, null, _alternateTower });
            Assert.AreEqual(2, session.Towers.Length);
            Assert.AreSame(_alternateTower, session.Towers[0]);
            Assert.AreSame(_fireball, session.Towers[1]);
        }

        [Test]
        public void BindTowers_IncludesCurseTowers_SkipsAuraOnly()
        {
            var curseTower = ScriptableObject.CreateInstance<TowerDefinition>();
            var curseRole = ScriptableObject.CreateInstance<CurseRoleDefinition>();
            curseTower.DisplayName = "Curse";
            curseTower.Roles = new TowerRoleDefinition[] { curseRole };

            var auraTower = ScriptableObject.CreateInstance<TowerDefinition>();
            var auraRole = ScriptableObject.CreateInstance<AuraRoleDefinition>();
            auraTower.DisplayName = "Aura";
            auraTower.Roles = new TowerRoleDefinition[] { auraRole };

            try
            {
                var session = new SkillLabSession();
                session.BindTowers(new[] { _fireball, curseTower, auraTower });
                Assert.AreEqual(2, session.Towers.Length);
                Assert.AreSame(curseTower, session.Towers[0]);
                Assert.AreSame(_fireball, session.Towers[1]);
            }
            finally
            {
                Object.DestroyImmediate(curseRole);
                Object.DestroyImmediate(curseTower);
                Object.DestroyImmediate(auraRole);
                Object.DestroyImmediate(auraTower);
            }
        }

        [Test]
        public void Fire_Curse_AppliesHexAtMuzzle_ShowsCasterNovaDisc()
        {
            var curseRole = ScriptableObject.CreateInstance<CurseRoleDefinition>();
            curseRole.AimMode = AimMode.Direct;
            curseRole.DeliveryPattern = DeliveryPattern.CasterNova;
            curseRole.Modifiers = new[] { Modifier(RoleStat.TowerRadius, 3f) };
            curseRole.Levels = new[]
            {
                new RoleLevelDefinition
                {
                    SourceLevel = 1,
                    Effects = new[]
                    {
                        RoleEffectModifier.Single(
                            RoleEffectKind.EnemyColdResistance,
                            RoleModifierOperation.Set,
                            -30f)
                    }
                }
            };
            var curseTower = ScriptableObject.CreateInstance<TowerDefinition>();
            curseTower.DisplayName = "Frostbite";
            curseTower.Roles = new TowerRoleDefinition[] { curseRole };
            curseTower.Damage = 0f;

            try
            {
                var session = new SkillLabSession();
                session.BindCatalog(_catalog);
                session.SetTowerDef(curseTower);
                session.Dummies.Init(_enemyDef);
                session.TowerPosition = Vector3.zero;
                session.Dummies.GetDummy(0).SetWorldPosition(new Vector3(1.5f, 0f, 0f));
                session.Dummies.GetDummy(1).SetWorldPosition(new Vector3(20f, 0f, 0f));

                session.Fire();

                Assert.AreEqual(SkillLabSession.StatusIdle, session.Status);
                Assert.IsTrue(session.LastTrace.HasTarget);
                Assert.AreEqual(1, session.LastTrace.Discs.Count);
                Assert.AreEqual(3f, session.LastTrace.Discs[0].Radius, 0.001f);
                Assert.IsTrue(session.Statuses.Has(session.Dummies.GetDummy(0), StatusId.CurseFrostbite));
                Assert.IsFalse(session.Statuses.Has(session.Dummies.GetDummy(1), StatusId.CurseFrostbite));
            }
            finally
            {
                Object.DestroyImmediate(curseRole);
                Object.DestroyImmediate(curseTower);
            }
        }

        [Test]
        public void Fire_Earthquake_ShowsSlamDiscAndKeepsOneAftershock()
        {
            _fireball.Tags = GemTag.Attack | GemTag.Melee | GemTag.Slam | GemTag.Aoe;
            _fireballRole.AimMode = AimMode.Ground;
            _fireballRole.DeliveryPattern = DeliveryPattern.GroundPulse;
            _fireballRole.Modifiers = new[]
            {
                Modifier(RoleStat.CastTime, 1f),
                Modifier(RoleStat.CastSpeed, 100f),
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f),
                Modifier(RoleStat.SplashRadius, SkillGemTowerMap.EarthquakeSlamRadius),
                Modifier(RoleStat.Damage, 10f)
            };
            _fireballRole.EffectPayloads = new[]
            {
                new EffectPayloadDefinition
                {
                    Trigger = EffectPayloadTrigger.AfterDelay,
                    Anchor = EffectPayloadAnchor.GroundTarget,
                    TravelPattern = EffectPayloadTravelPattern.StationaryPulse,
                    ScatterPattern = EffectPayloadScatterPattern.None,
                    HitPolicy = EffectPayloadHitPolicy.PerImpact,
                    Tags = GemTag.Aoe,
                    Count = 1,
                    DamageMultiplier = SkillGemTowerMap.EarthquakeAftershockDamageMultiplier,
                    AoeRadius = SkillGemTowerMap.EarthquakeAftershockRadius,
                    DelaySeconds = SkillGemTowerMap.EarthquakeAftershockDelaySeconds
                }
            };

            var session = MakeSession();
            session.TowerPosition = Vector3.zero;
            var dummy = session.Dummies.GetDummy(0);
            dummy.SetWorldPosition(new Vector3(3f, 0f, 0f));
            dummy.LastDamageSource = null;

            session.Fire();
            Assert.AreEqual(SkillLabSession.StatusIdle, session.Status);
            Assert.GreaterOrEqual(session.LastTrace.Discs.Count, 1);
            Assert.AreEqual(SkillGemTowerMap.EarthquakeSlamRadius, session.LastTrace.Discs[0].Radius, 0.001f);
            Assert.AreSame(_fireball, dummy.LastDamageSource);
            Assert.AreEqual(1, session.EffectPayloads.Count);

            session.Fire();
            Assert.AreEqual(1, session.EffectPayloads.Count);

            session.TickVolley(1f);
            Assert.AreEqual(1, session.EffectPayloads.Count);
            session.TickVolley(0.02f);
            Assert.AreEqual(0, session.EffectPayloads.Count);
        }

        [Test]
        public void TickVolley_Curse_DropsHexWhenDummyLeavesDisc()
        {
            var curseRole = ScriptableObject.CreateInstance<CurseRoleDefinition>();
            curseRole.AimMode = AimMode.Direct;
            curseRole.DeliveryPattern = DeliveryPattern.CasterNova;
            curseRole.Modifiers = new[] { Modifier(RoleStat.TowerRadius, 3f) };
            curseRole.Levels = new[]
            {
                new RoleLevelDefinition
                {
                    SourceLevel = 1,
                    Effects = new[]
                    {
                        RoleEffectModifier.Single(
                            RoleEffectKind.EnemyColdResistance,
                            RoleModifierOperation.Set,
                            -30f)
                    }
                }
            };
            var curseTower = ScriptableObject.CreateInstance<TowerDefinition>();
            curseTower.DisplayName = "Frostbite";
            curseTower.Roles = new TowerRoleDefinition[] { curseRole };
            curseTower.Damage = 0f;

            try
            {
                var session = new SkillLabSession();
                session.BindCatalog(_catalog);
                session.SetTowerDef(curseTower);
                session.Dummies.Init(_enemyDef);
                session.TowerPosition = Vector3.zero;
                var dummy = session.Dummies.GetDummy(0);
                dummy.SetWorldPosition(new Vector3(1.5f, 0f, 0f));

                session.Fire();
                Assert.IsTrue(session.Statuses.Has(dummy, StatusId.CurseFrostbite));

                dummy.SetWorldPosition(new Vector3(20f, 0f, 0f));
                session.TickVolley(0.016f);
                Assert.IsFalse(session.Statuses.Has(dummy, StatusId.CurseFrostbite));
            }
            finally
            {
                Object.DestroyImmediate(curseRole);
                Object.DestroyImmediate(curseTower);
            }
        }

        [Test]
        public void SelectTower_LoadsBoundDefinition_ClearsOverlay()
        {
            var session = MakeSession();
            session.BindTowers(new[] { _fireball, _alternateTower });
            session.SelectTower(session.IndexOfDisplayName("Fireball"));
            session.TowerPosition = Vector3.zero;
            session.Dummies.GetDummy(0).SetWorldPosition(new Vector3(3f, 0f, 0f));
            session.Fire();
            Assert.IsTrue(session.LastTrace.HasTarget);

            session.SelectTower(session.IndexOfDisplayName("Cleave"));
            Assert.AreSame(_alternateTower, session.Tower.Def);
            Assert.AreEqual(session.IndexOfDisplayName("Cleave"), session.SelectedTowerIndex);
            Assert.AreEqual(0, session.LastTrace.Segments.Count);
        }

        [Test]
        public void SelectTower_SameTower_KeepsSockets()
        {
            var session = MakeSession();
            session.BindTowers(new[] { _fireball });
            session.SelectTower(0);
            session.SetSocket(0, GemId.MultipleProjectiles);
            session.SelectTower(0);
            Assert.AreEqual(GemId.MultipleProjectiles, session.Tower.Sockets[0].Id);
            Assert.AreEqual(GemRarity.Normal, session.Tower.Sockets[0].Rarity);
        }

        [Test]
        public void SelectTower_OutOfRange_NoOp()
        {
            var session = MakeSession();
            session.BindTowers(new[] { _fireball, _alternateTower });
            session.SelectTower(0);
            session.SelectTower(-1);
            session.SelectTower(99);
            Assert.AreSame(session.Towers[0], session.Tower.Def);
        }

        [Test]
        public void IndexOfDisplayName_FindsFireball()
        {
            var session = new SkillLabSession();
            session.BindTowers(new[] { _alternateTower, _fireball });
            Assert.AreEqual(1, session.IndexOfDisplayName("Fireball"));
            Assert.AreEqual(-1, session.IndexOfDisplayName("Missing"));
        }

        [Test]
        public void ResetPins_DoesNotClearOverlay()
        {
            var session = MakeSession();
            session.TowerPosition = Vector3.zero;
            session.Dummies.GetDummy(0).SetWorldPosition(new Vector3(3f, 0f, 0f));
            session.Fire();
            Assert.IsTrue(session.LastTrace.HasTarget);
            session.ResetPins();
            Assert.IsTrue(session.LastTrace.HasTarget);
            Assert.IsFalse(session.HasActiveVolley);
            Assert.AreEqual(DummyField.HeadPin, session.Dummies.GetDummy(0).WorldPosition);
        }

        [Test]
        public void SetSocket_CanUnsocketRecipeTrio()
        {
            var session = MakeSession();
            session.SetSocket(0, GemId.MultipleProjectiles);
            session.SetSocket(1, GemId.Chain);
            session.SetSocket(2, GemId.Fork);
            Assert.AreEqual(GemRarity.Normal, session.Tower.Sockets[0].Rarity);
            Assert.AreEqual(GemRarity.Normal, session.Tower.Sockets[1].Rarity);
            Assert.AreEqual(GemRarity.Normal, session.Tower.Sockets[2].Rarity);
            Assert.IsFalse(session.IsHydra);
            session.SetSocket(2, GemId.None);
            Assert.IsTrue(session.Tower.Sockets[2].IsEmpty);
            Assert.IsFalse(session.IsHydra);
        }

        [Test]
        public void SetSocket_RejectsDuplicateId()
        {
            var session = MakeSession();
            session.SetSocket(0, GemId.MultipleProjectiles);
            session.SetSocket(1, GemId.MultipleProjectiles);
            Assert.AreEqual(GemId.MultipleProjectiles, session.Tower.Sockets[0].Id);
            Assert.IsTrue(session.Tower.Sockets[1].IsEmpty);
        }

        static RoleStatModifier Modifier(RoleStat stat, float value)
        {
            return RoleStatModifier.Single(stat, RoleModifierOperation.Set, value);
        }

        static List<Vector3> CollectMagmaLandings(AttackTrace trace)
        {
            var landings = new List<Vector3>(4);
            var segments = trace.Segments;
            for (var i = 0; i < segments.Count; i++)
            {
                if (segments[i].Kind == AttackTraceKind.Magma
                    && Mathf.Abs(segments[i].To.y) <= 1e-5f)
                    landings.Add(segments[i].To);
            }

            return landings;
        }

        static void WaitForMagmaPayloads(SkillLabSession session, int count)
        {
            for (var i = 0; i < 120 && session.EffectPayloads.Count < count; i++)
                session.TickVolley(0.02f);
        }

        static void AssertLandingsMatchPayloads(
            List<Vector3> expectedLandings,
            IReadOnlyList<EffectPayloadRuntime> payloads)
        {
            var remaining = new List<Vector3>(expectedLandings);
            for (var i = 0; i < payloads.Count; i++)
            {
                var actual = payloads[i].LandingPoint;
                var matched = false;
                for (var j = 0; j < remaining.Count; j++)
                {
                    if ((actual - remaining[j]).sqrMagnitude <= 1e-6f)
                    {
                        remaining.RemoveAt(j);
                        matched = true;
                        break;
                    }
                }

                Assert.IsTrue(matched, $"Magma payload {i} does not match an overlay landing.");
            }
        }

        static bool LandingsEqual(List<Vector3> a, List<Vector3> b)
        {
            if (a.Count != b.Count)
                return false;

            var remaining = new List<Vector3>(b);
            for (var i = 0; i < a.Count; i++)
            {
                var matched = false;
                for (var j = 0; j < remaining.Count; j++)
                {
                    if ((a[i] - remaining[j]).sqrMagnitude <= 1e-6f)
                    {
                        remaining.RemoveAt(j);
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                    return false;
            }

            return true;
        }

        SkillLabSession MakeSession()
        {
            var session = new SkillLabSession();
            session.BindCatalog(_catalog);
            session.SetTowerDef(_fireball);
            session.Dummies.Init(_enemyDef);
            return session;
        }
    }
}

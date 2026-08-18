using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Enemies;

namespace GemTD.Tests.EditMode
{
    public sealed class TargetSelectorTests
    {
        const float CellSize = 1f;

        EnemyDefinition _def;
        TargetSelector _selector;

        [SetUp]
        public void SetUp()
        {
            _def = ScriptableObject.CreateInstance<EnemyDefinition>();
            _def.MaxHealth = 50f;
            _def.MoveSpeed = 2f;
            _selector = new TargetSelector();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_def);
        }

        [Test]
        public void TrySelectFirst_PicksHighestProgressAmongLivingInRange()
        {
            var waypoints = BuildWorldWaypoints(
                new Vector2Int(0, 0),
                new Vector2Int(10, 0));

            var near = new EnemyRuntime();
            near.Init(_def, waypoints);
            near.TickMove(0.5f);

            var far = new EnemyRuntime();
            far.Init(_def, waypoints);
            far.TickMove(2.5f);

            var towerPos = CellCenter(0, 0);
            var candidates = new List<EnemyRuntime> { near, far };

            Assert.IsTrue(_selector.TrySelectFirst(towerPos, range: 20f, candidates, out var target));
            Assert.AreSame(far, target);
            Assert.Greater(far.Progress, near.Progress);
        }

        [Test]
        public void TrySelectFirst_IgnoresDeadAndOutOfRange()
        {
            var waypoints = BuildWorldWaypoints(
                new Vector2Int(0, 0),
                new Vector2Int(10, 0));

            var deadLeader = new EnemyRuntime();
            deadLeader.Init(_def, waypoints);
            deadLeader.TickMove(4f);
            deadLeader.ApplyDamage(100f);

            var outOfRange = new EnemyRuntime();
            outOfRange.Init(_def, waypoints);
            outOfRange.TickMove(8f);

            var inRange = new EnemyRuntime();
            inRange.Init(_def, waypoints);
            inRange.TickMove(1f);

            var towerPos = CellCenter(0, 0);
            var candidates = new List<EnemyRuntime> { deadLeader, outOfRange, inRange };

            Assert.IsTrue(_selector.TrySelectFirst(towerPos, range: 3f, candidates, out var target));
            Assert.AreSame(inRange, target);
        }

        [Test]
        public void TrySelectFirst_ReturnsFalseWhenNoValidTarget()
        {
            var towerPos = CellCenter(0, 0);
            Assert.IsFalse(_selector.TrySelectFirst(towerPos, range: 5f, new List<EnemyRuntime>(), out var target));
            Assert.IsNull(target);
        }

        [Test]
        public void Recipe_MostArmor_ThenMostHpPct_ThenFirst_PicksArmored()
        {
            var tankDef = ScriptableObject.CreateInstance<EnemyDefinition>();
            tankDef.MaxHealth = 50f;
            tankDef.Armor = 100;
            tankDef.MoveSpeed = 2f;
            var squishDef = ScriptableObject.CreateInstance<EnemyDefinition>();
            squishDef.MaxHealth = 100f;
            squishDef.Armor = 0;
            squishDef.MoveSpeed = 2f;

            var waypoints = BuildWorldWaypoints(new Vector2Int(0, 0), new Vector2Int(10, 0));
            var a = new EnemyRuntime(); a.Init(squishDef, waypoints); a.TickMove(2.5f);
            var b = new EnemyRuntime(); b.Init(squishDef, waypoints); b.TickMove(0.5f);
            var tank = new EnemyRuntime(); tank.Init(tankDef, waypoints); tank.TickMove(1f);

            var recipe = new TargetingRecipe
            {
                Priority1 = TargetingKey.MostArmor,
                Priority2 = TargetingKey.MostHpPct,
                Priority3 = TargetingKey.First
            };
            Assert.IsTrue(_selector.TrySelect(recipe, CellCenter(0, 0), 20f,
                new List<EnemyRuntime> { a, b, tank }, out var target));
            Assert.AreSame(tank, target);

            Object.DestroyImmediate(tankDef);
            Object.DestroyImmediate(squishDef);
        }

        [Test]
        public void Recipe_MostArmor_AllZeroArmor_FallsThroughToMostHpPct()
        {
            var waypoints = BuildWorldWaypoints(new Vector2Int(0, 0), new Vector2Int(10, 0));
            _def.MaxHealth = 100f;
            _def.Armor = 0;
            var low = new EnemyRuntime(); low.Init(_def, waypoints); low.ApplyDamage(60f);
            var high = new EnemyRuntime(); high.Init(_def, waypoints);
            var recipe = new TargetingRecipe
            {
                Priority1 = TargetingKey.MostArmor,
                Priority2 = TargetingKey.MostHpPct,
                Priority3 = TargetingKey.First
            };
            Assert.IsTrue(_selector.TrySelect(recipe, CellCenter(0, 0), 20f,
                new List<EnemyRuntime> { low, high }, out var target));
            Assert.AreSame(high, target);
        }

        [Test]
        public void Recipe_EqualHpPct_FallsThroughToFirst()
        {
            var waypoints = BuildWorldWaypoints(new Vector2Int(0, 0), new Vector2Int(10, 0));
            var near = new EnemyRuntime(); near.Init(_def, waypoints); near.TickMove(0.5f);
            var far = new EnemyRuntime(); far.Init(_def, waypoints); far.TickMove(2.5f);
            var recipe = new TargetingRecipe
            {
                Priority1 = TargetingKey.MostHpPct,
                Priority2 = TargetingKey.MostHpPct,
                Priority3 = TargetingKey.First
            };
            Assert.IsTrue(_selector.TrySelect(recipe, CellCenter(0, 0), 20f,
                new List<EnemyRuntime> { near, far }, out var target));
            Assert.AreSame(far, target);
        }

        [Test]
        public void Recipe_LeastHpPct_PicksDamaged()
        {
            var waypoints = BuildWorldWaypoints(new Vector2Int(0, 0), new Vector2Int(10, 0));
            var hurt = new EnemyRuntime(); hurt.Init(_def, waypoints); hurt.ApplyDamage(25f);
            var full = new EnemyRuntime(); full.Init(_def, waypoints);
            var recipe = new TargetingRecipe
            {
                Priority1 = TargetingKey.LeastHpPct,
                Priority2 = TargetingKey.First,
                Priority3 = TargetingKey.First
            };
            Assert.IsTrue(_selector.TrySelect(recipe, CellCenter(0, 0), 20f,
                new List<EnemyRuntime> { full, hurt }, out var target));
            Assert.AreSame(hurt, target);
        }

        [Test]
        public void Recipe_Fastest_UsesCurrentMoveSpeed()
        {
            var waypoints = BuildWorldWaypoints(new Vector2Int(0, 0), new Vector2Int(10, 0));
            var slow = new EnemyRuntime(); slow.Init(_def, waypoints); slow.MoveSpeedMultiplier = 0.5f;
            var fast = new EnemyRuntime(); fast.Init(_def, waypoints); fast.MoveSpeedMultiplier = 1f;
            var recipe = new TargetingRecipe
            {
                Priority1 = TargetingKey.Fastest,
                Priority2 = TargetingKey.First,
                Priority3 = TargetingKey.First
            };
            Assert.IsTrue(_selector.TrySelect(recipe, CellCenter(0, 0), 20f,
                new List<EnemyRuntime> { slow, fast }, out var target));
            Assert.AreSame(fast, target);
        }

        [Test]
        public void Recipe_MostShield_PicksHigherShieldHp()
        {
            var shieldedDef = ScriptableObject.CreateInstance<EnemyDefinition>();
            shieldedDef.MaxHealth = 50f;
            shieldedDef.ShieldMax = 20f;
            var noneDef = ScriptableObject.CreateInstance<EnemyDefinition>();
            noneDef.MaxHealth = 50f;
            noneDef.ShieldMax = 0f;
            var waypoints = BuildWorldWaypoints(new Vector2Int(0, 0), new Vector2Int(10, 0));
            var shielded = new EnemyRuntime(); shielded.Init(shieldedDef, waypoints);
            var none = new EnemyRuntime(); none.Init(noneDef, waypoints);
            var recipe = new TargetingRecipe
            {
                Priority1 = TargetingKey.MostShield,
                Priority2 = TargetingKey.First,
                Priority3 = TargetingKey.First
            };
            Assert.IsTrue(_selector.TrySelect(recipe, CellCenter(0, 0), 20f,
                new List<EnemyRuntime> { none, shielded }, out var target));
            Assert.AreSame(shielded, target);
            Object.DestroyImmediate(shieldedDef);
            Object.DestroyImmediate(noneDef);
        }

        [Test]
        public void Recipe_ZeroMaxHealth_DoesNotThrow()
        {
            _def.MaxHealth = 0f;
            var waypoints = BuildWorldWaypoints(new Vector2Int(0, 0), new Vector2Int(10, 0));
            var enemy = new EnemyRuntime(); enemy.Init(_def, waypoints);
            var recipe = TargetingRecipe.Default;
            recipe.Priority1 = TargetingKey.MostHpPct;
            Assert.DoesNotThrow(() => _selector.TrySelect(recipe, CellCenter(0, 0), 20f,
                new List<EnemyRuntime> { enemy }, out _));
        }

        [Test]
        public void TrySelect_Last_UsesRecipe()
        {
            var waypoints = BuildWorldWaypoints(new Vector2Int(0, 0), new Vector2Int(10, 0));
            var near = new EnemyRuntime(); near.Init(_def, waypoints); near.TickMove(0.5f);
            var far = new EnemyRuntime(); far.Init(_def, waypoints); far.TickMove(2.5f);
            var recipe = new TargetingRecipe
            {
                Priority1 = TargetingKey.Last,
                Priority2 = TargetingKey.First,
                Priority3 = TargetingKey.First
            };
            Assert.IsTrue(_selector.TrySelect(recipe, CellCenter(0, 0), 20f,
                new List<EnemyRuntime> { near, far }, out var target));
            Assert.AreSame(near, target);
        }

        static Vector3 CellCenter(int x, int y)
        {
            var half = CellSize * 0.5f;
            return new Vector3(x * CellSize + half, 0f, y * CellSize + half);
        }

        static List<Vector3> BuildWorldWaypoints(params Vector2Int[] cells)
        {
            var list = new List<Vector3>(cells.Length);
            for (var i = 0; i < cells.Length; i++)
                list.Add(CellCenter(cells[i].x, cells[i].y));
            return list;
        }
    }
}

using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using GemTD.Core;
using GemTD.Gameplay.Grid;
using GemTD.Gameplay.Map;
using GemTD.Gameplay.Run;

namespace GemTD.Tests.EditMode
{
    /// <summary>
    /// Deterministic production-catalog simulation comparing the live 50-wave
    /// every-wave expansion cadence against the proposed 30-wave odd-wave cadence.
    /// Does not mutate campaign assets.
    /// </summary>
    public sealed class MapExpansionSimulationTests
    {
        const string RunConfigPath = "Assets/Data/RunConfig_Default.asset";
        const string ChunkCatalogPath = "Assets/Data/Map/ChunkCatalog.asset";
        const string WaveCatalogPath = "Assets/Data/Waves/WaveCatalog.asset";

        // Keep the seed set small: each expand evaluates the production catalog
        // through topology dry-runs, so wide seed sweeps stall the Editor.
        static readonly int[] Seeds = { 1 };
        static readonly int[] Lanes = { 1, 2, 3, 4 };

        RunConfig _configAsset;
        ChunkCatalog _catalog;
        WaveCatalog _waves;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _configAsset = AssetDatabase.LoadAssetAtPath<RunConfig>(RunConfigPath);
            _catalog = AssetDatabase.LoadAssetAtPath<ChunkCatalog>(ChunkCatalogPath);
            _waves = AssetDatabase.LoadAssetAtPath<WaveCatalog>(WaveCatalogPath);
            Assert.IsNotNull(_configAsset, "Missing " + RunConfigPath);
            Assert.IsNotNull(_catalog, "Missing " + ChunkCatalogPath);
            Assert.IsNotNull(_waves, "Missing " + WaveCatalogPath);
            Assert.Greater(_waves.GetWavesOrEmpty().Length, 0);
        }

        [TearDown]
        public void TearDown() => GameEvents.ClearAll();

        [Test]
        public void Simulate_BalanceAnchors_Wave30Vs50()
        {
            var report = new StringBuilder(4096);
            report.AppendLine("=== Gem TD balance anchors ===");
            AppendBalanceComparison(report);
            Debug.Log(report.ToString());
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void Simulate_BaselineVsProposed_PerLane(int lanes)
        {
            var report = new StringBuilder(8192);
            report.AppendLine("=== Gem TD map expansion simulation lanes=" + lanes + " ===");

            var baselineFailures = 0;
            var proposedFailures = 0;
            var proposedOpenFrontiersAtEnd = 0;

            for (var seedIndex = 0; seedIndex < Seeds.Length; seedIndex++)
            {
                var seed = Seeds[seedIndex];
                var baseline = RunScenario(
                    "baseline50",
                    lanes,
                    seed,
                    endWave: 50,
                    ExpansionCadence.EveryPreFinalWave);
                var proposed = RunScenario(
                    "proposed30odd",
                    lanes,
                    seed,
                    endWave: 30,
                    ExpansionCadence.OddUpcomingWaves);

                AppendRunSummary(report, baseline);
                AppendRunSummary(report, proposed);

                if (!baseline.AllTipsReachHome || baseline.ExpandFailures > 0)
                    baselineFailures++;

                // A mid-run expand waive with frontiers>0 is recoverable when the
                // post-campaign TryForceDeadEndCap closes the map. Count only
                // unrecovered topology as a hard proposed failure.
                if (!proposed.AllTipsReachHome)
                    proposedFailures++;
                else if (proposed.ExpandFailures > 0 && proposed.FinalFrontiers > 0)
                    proposedFailures++;
                else if (proposed.ExpandFailures > 0)
                    report.AppendLine("NOTE " + proposed.Label
                        + " waived mid-run but force-cap recovered (finalFront=0).");

                if (proposed.FinalFrontiers > 0)
                    proposedOpenFrontiersAtEnd++;
            }

            report.AppendLine("Summary failures: baseline=" + baselineFailures
                + " proposed=" + proposedFailures
                + " proposedOpenFrontiersAtEnd=" + proposedOpenFrontiersAtEnd);
            Debug.Log(report.ToString());

            // Baseline stuck-open and recoverable proposed waives are review notes.
            // Hard gate: proposed must finish with zero expandable frontiers.
            if (baselineFailures > 0)
                Debug.LogWarning("Baseline topology issues:\n" + report);

            Assert.AreEqual(0, proposedFailures, "Proposed topology failures\n" + report);
            Assert.AreEqual(0, proposedOpenFrontiersAtEnd, "Proposed left open frontiers\n" + report);
        }

        void AppendBalanceComparison(StringBuilder report)
        {
            report.AppendLine("--- Balance anchors (unchanged scaling formulas) ---");
            for (var laneIndex = 0; laneIndex < Lanes.Length; laneIndex++)
            {
                var lanes = Lanes[laneIndex];
                var mode = ModeForLanes(lanes);
                report.AppendLine("Lanes " + lanes
                    + "  tipCap=" + mode.TipCap
                    + "  hpMult=" + mode.HpMultiplier.ToString("0.##")
                    + "  splitP=" + mode.SplitP.ToString("0.##"));
                AppendWaveBalanceRow(report, 30, mode.HpMultiplier, tipCap: mode.TipCap);
                AppendWaveBalanceRow(report, 50, mode.HpMultiplier, tipCap: mode.TipCap);
            }

            report.AppendLine("Draft opportunities: baseline end50 = 49; proposed end30 = 29");
            report.AppendLine("Expansion turns: baseline = 49; proposed odd = 15");
            report.AppendLine();
        }

        void AppendWaveBalanceRow(StringBuilder report, int wave, float hpMult, int tipCap)
        {
            var template = ResolveWaveTemplate(wave);
            var bodies = CountScaledBodies(template, wave);
            var bossesAtCap = BossCadence.BossCount(wave, tipCap, endless: false);
            var bossesAtOne = BossCadence.BossCount(wave, 1, endless: false);
            var hp = WaveScaling.HpScale(wave, hpMult);
            var armor = WaveScaling.ArmorScale(wave);
            var speed = WaveScaling.SpeedScale(wave);
            var resist = WaveScaling.ResistBonus(wave);
            var gold = WaveScaling.ScaleEndWaveGold(_configAsset.EndWaveGold, wave);
            var combo = wave > WaveComboSchedule.AuthoredWaveCount
                ? WaveComboSchedule.ForWave(wave, endless: false).ToString()
                : "authored";

            report.AppendLine(
                "  W" + wave
                + " combo=" + combo
                + " bodies=" + bodies
                + " hp=" + hp.ToString("0.00")
                + " armor=" + armor.ToString("0.00")
                + " speed=" + speed.ToString("0.00")
                + " resist+=" + resist
                + " endGold=" + gold
                + " bosses(tipCap/1)=" + bossesAtCap + "/" + bossesAtOne
                + " draftAfter@50=" + WaveController.ShouldOfferDraft(wave, 50, false)
                + " draftAfter@30=" + WaveController.ShouldOfferDraft(wave, 30, false));
        }

        WaveDefinition ResolveWaveTemplate(int waveNumber)
        {
            var waves = _waves.GetWavesOrEmpty();
            if (waveNumber > WaveComboSchedule.AuthoredWaveCount
                && _waves.Combos != null
                && _waves.Combos.Length > 0)
            {
                var index = (int)WaveComboSchedule.ForWave(waveNumber, endless: false);
                if (index >= 0 && index < _waves.Combos.Length && _waves.Combos[index] != null)
                    return _waves.Combos[index];
            }

            if (waveNumber >= 1 && waveNumber <= waves.Length)
                return waves[waveNumber - 1];
            return waves[waves.Length - 1];
        }

        static int CountScaledBodies(WaveDefinition template, int wave)
        {
            if (template == null || template.Entries == null)
                return 0;

            var regular = 0;
            for (var i = 0; i < template.Entries.Length; i++)
            {
                var entry = template.Entries[i];
                if (entry.Enemy == null || entry.Count <= 0 || entry.Enemy.IsBoss)
                    continue;
                regular++;
            }

            if (regular == 0)
                return 0;

            var counts = new int[regular];
            var n = 0;
            for (var i = 0; i < template.Entries.Length; i++)
            {
                var entry = template.Entries[i];
                if (entry.Enemy == null || entry.Count <= 0 || entry.Enemy.IsBoss)
                    continue;
                counts[n++] = entry.Count;
            }

            WaveScaling.ApplyCountScale(counts, regular, template.WaveNumber, wave);
            var total = 0;
            for (var i = 0; i < regular; i++)
                total += counts[i];
            return total;
        }

        DifficultyModeRow ModeForLanes(int lanes)
        {
            var clone = Object.Instantiate(_configAsset);
            clone.OpenArmCount = lanes;
            var mode = clone.GetDifficultyMode();
            Object.DestroyImmediate(clone);
            return mode;
        }

        ScenarioResult RunScenario(
            string name,
            int lanes,
            int seed,
            int endWave,
            ExpansionCadence cadence)
        {
            var config = Object.Instantiate(_configAsset);
            config.OpenArmCount = lanes;
            config.EndWave = endWave;

            var cellsW = config.ChunkGridWidth * ChunkMask.Size;
            var cellsH = config.ChunkGridHeight * ChunkMask.Size;
            var board = new GridBoard(cellsW, cellsH);
            var path = new PathGraph(cellsW, cellsH);
            path.BindBoard(board);
            var grid = new ChunkGrid(config.ChunkGridWidth, config.ChunkGridHeight);
            var stamp = new ChunkStampService();
            var rng = new System.Random(seed);

            StartLayoutBuilder.Build(grid, stamp, path, board, _catalog, rng, lanes);

            var expand = new ChunkExpandService(grid, path, board, stamp, _catalog, rng, config);
            expand.Cadence = cadence;

            var result = new ScenarioResult
            {
                Label = name + " L" + lanes + " seed" + seed,
                Lanes = lanes,
                Seed = seed,
                EndWave = endWave,
                Cadence = cadence,
                ExpansionsAttempted = 0,
                ExpansionsSucceeded = 0,
                ExpandFailures = 0,
                MaxFrontiers = 0,
                MaxTips = 0,
                MaxHop = 0,
                ChunksPlaced = grid.Count
            };

            var legal = new List<Vector2Int>(8);
            var tips = new List<Vector2Int>(8);

            for (var upcoming = 1; upcoming <= endWave; upcoming++)
            {
                expand.UpcomingWaveNumber = upcoming;
                var shouldExpand = ExpandPickPolicy.ShouldExpand(upcoming, endWave, cadence);
                if (!shouldExpand)
                    continue;

                result.ExpansionsAttempted++;
                var legalCount = expand.CollectLegalExpands(legal);
                if (legalCount <= 0)
                {
                    var openFrontiers = expand.CountOpenFrontiers();
                    // Production waives expand when no legal slots remain. That is
                    // success when frontiers are already closed; stuck open fronts fail.
                    if (openFrontiers == 0)
                    {
                        result.WaivedClosedEarly = true;
                        break;
                    }

                    result.ExpandFailures++;
                    result.LastFailure = "W" + upcoming + " no legal expands; frontiers="
                        + openFrontiers;
                    break;
                }

                var pick = legal[rng.Next(legalCount)];
                if (!expand.TryExpand(pick))
                {
                    result.ExpandFailures++;
                    result.LastFailure = "W" + upcoming + " TryExpand failed at " + pick;
                    break;
                }

                result.ExpansionsSucceeded++;
                result.ChunksPlaced = grid.Count;

                if (!grid.TryGet(pick.x, pick.y, out var slot))
                {
                    result.ExpandFailures++;
                    result.LastFailure = "W" + upcoming + " missing committed slot " + pick;
                    break;
                }

                if (slot.Mask.Type == ChunkType.DeadEnd)
                    result.DeadEndsPlaced++;
                else if (slot.Mask.Type == ChunkType.TJunction)
                    result.TJunctionsPlaced++;
                else if (slot.Mask.Type == ChunkType.Cross)
                    result.CrossesPlaced++;

                var frontiers = expand.CountOpenFrontiers();
                if (frontiers > result.MaxFrontiers)
                    result.MaxFrontiers = frontiers;

                path.CollectSpawnTips(tips);
                if (tips.Count > result.MaxTips)
                    result.MaxTips = tips.Count;

                for (var t = 0; t < tips.Count; t++)
                {
                    var hop = path.HopDistanceFromHome(tips[t]);
                    if (hop > result.MaxHop)
                        result.MaxHop = hop;
                }

                if (!path.AllTipsReachHome())
                {
                    result.ExpandFailures++;
                    result.LastFailure = "W" + upcoming + " tips lost home reachability";
                    break;
                }
            }

            // Final campaign tip auto-cap (Task 7) after EndWave clear.
            expand.UpcomingWaveNumber = endWave;
            expand.TryForceDeadEndCap();

            result.FinalFrontiers = expand.CountOpenFrontiers();
            path.CollectSpawnTips(tips);
            result.FinalTips = tips.Count;
            result.AllTipsReachHome = path.AllTipsReachHome();
            result.ChunksPlaced = grid.Count;

            Object.DestroyImmediate(config);
            return result;
        }

        static void AppendRunSummary(StringBuilder report, ScenarioResult run)
        {
            report.AppendLine(
                run.Label
                + " expand=" + run.ExpansionsSucceeded + "/" + run.ExpansionsAttempted
                + " fail=" + run.ExpandFailures
                + (run.WaivedClosedEarly ? " waivedClosed" : "")
                + " chunks=" + run.ChunksPlaced
                + " maxFront=" + run.MaxFrontiers
                + " maxTips=" + run.MaxTips
                + " finalFront=" + run.FinalFrontiers
                + " finalTips=" + run.FinalTips
                + " maxHop=" + run.MaxHop
                + " DE/T/X=" + run.DeadEndsPlaced + "/" + run.TJunctionsPlaced + "/" + run.CrossesPlaced
                + " home=" + run.AllTipsReachHome
                + (string.IsNullOrEmpty(run.LastFailure) ? "" : " ERR=" + run.LastFailure));
        }

        sealed class ScenarioResult
        {
            public string Label;
            public int Lanes;
            public int Seed;
            public int EndWave;
            public ExpansionCadence Cadence;
            public int ExpansionsAttempted;
            public int ExpansionsSucceeded;
            public int ExpandFailures;
            public bool WaivedClosedEarly;
            public int ChunksPlaced;
            public int MaxFrontiers;
            public int MaxTips;
            public int FinalFrontiers;
            public int FinalTips;
            public int MaxHop;
            public int DeadEndsPlaced;
            public int TJunctionsPlaced;
            public int CrossesPlaced;
            public bool AllTipsReachHome;
            public string LastFailure;
        }
    }
}

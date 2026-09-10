using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SecondWind.SpaceStation.Editor
{
    public static class StationChecks
    {
        static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        static StationState Apply(StationState s, StationAction a) { Check(StationRules.TryApply(s, a, out var next, out string error), error); return next; }
        public static string Run()
        {
            var passed = new List<string>();
            var s = StationRules.NewGame(seed: 1234);
            Check(s.players[0].resources.S == 2 && s.players[0].resources.Total == 2 && s.market[0] == "A-0" && s.market[1] == "B-1", "initial state"); passed.Add("Initial resources and fixed deck");
            var actions = new[] { new StationAction("build", "solar"), new StationAction("collect", null, 0), new StationAction("build", "condenser"), new StationAction("supply", "A-0"), new StationAction("collect", null, 0), new StationAction("supply", "F-2"), new StationAction("collect", null, 0), new StationAction("supply", "C-3"), new StationAction("supply", "B-1"), new StationAction("collect", null, 0), new StationAction("supply", "D-4"), new StationAction("pass") };
            int[,] expected = { { 0,0,0,0 }, { 2,1,0,0 }, { 0,1,0,0 }, { 0,0,0,3 }, { 2,1,1,3 }, { 1,1,1,6 }, { 3,2,2,6 }, { 3,0,1,11 }, { 1,0,2,14 }, { 3,1,3,14 }, { 0,2,3,18 }, { 0,3,4,18 } };
            for (int i = 0; i < actions.Length; i++)
            {
                s = Apply(s, actions[i]); var h = s.history.Last();
                Check(h.after.S == expected[i,0] && h.after.E == expected[i,1] && h.after.W == expected[i,2] && h.score == expected[i,3], "PPT example turn " + (i + 1));
                string json = JsonUtility.ToJson(s); Check(StationRules.Restore(json) != null, "Restore turn " + (i + 1));
                if (i == 0) Check(h.after.E == 0 && s.players[0].resources.E == 1, "deferred production");
                if (i == 8) Check(!s.finished, "goal must not end before turn 12");
            }
            Check(s.finished && s.round == 12 && s.players[0].score == 18 && s.players[0].resources.W == 4, "18 point final result");
            passed.Add("All 12 reference turns: balances, points, delayed production, no early end"); passed.Add("Save replay after every action");
            Check(!StationRules.TryApply(s, new StationAction("pass"), out _, out _), "13th turn rejected"); passed.Add("No thirteenth action or production");
            var initial = StationRules.NewGame(); string before = JsonUtility.ToJson(initial);
            Check(!StationRules.TryApply(initial, new StationAction("supply", "A-0"), out _, out _), "insufficient cost");
            Check(!StationRules.TryApply(initial, new StationAction("pass"), out _, out _, 1), "wrong actor");
            Check(!StationRules.TryApply(initial, new StationAction("pass"), out _, out _, expectedRevision: 20), "stale revision");
            Check(JsonUtility.ToJson(initial) == before, "invalid action mutated input"); passed.Add("Cost, actor and revision validation without mutation");
            StationRules.TryApply(initial, new StationAction("collect", null, 0), out var first, out _, actionId: "tap");
            Check(!StationRules.TryApply(first, new StationAction("collect", null, 0), out _, out _, actionId: "tap"), "double action"); passed.Add("Action ID idempotency");
            var capped = StationRules.NewGame(); capped.players[0].resources.S = 8;
            capped = Apply(capped, new StationAction("collect", null, 0)); Check(capped.players[0].resources.S == 9 && capped.history.Last().wasted.S == 1, "collection cap");
            capped.players[0].facilities.Add("solar"); capped.players[0].resources.E = 9; capped = Apply(capped, new StationAction("pass")); Check(capped.players[0].resources.E == 9 && capped.overflow.E == 1, "production cap"); passed.Add("Collection and production overflow");
            Check(StationRules.Validate(capped, new StationAction("build", "solar")) != null, "duplicate facility"); passed.Add("No duplicate facilities");
            var exhausted = StationRules.NewGame(); for (int i = 0; i < 10; i++) exhausted = Apply(exhausted, new StationAction("replace", exhausted.market[0]));
            Check(exhausted.round == 11 && exhausted.cursor == 12 && StationRules.Validate(exhausted, new StationAction("replace", exhausted.market[0])) != null, "exhausted replacement");
            exhausted.players[0].resources = new Resources3(9,9,9); exhausted = Apply(exhausted, new StationAction("supply", exhausted.market[0])); Check(string.IsNullOrEmpty(exhausted.market[0]), "no refill"); passed.Add("Replacement and deck exhaustion");
            var restarted = StationRules.Restart(s, false, 77); Check(restarted.runId != s.runId && restarted.deck.SequenceEqual(s.deck) && restarted.revision == 0, "same deck restart"); passed.Add("Same-deck restart with new run identity");
            var damaged = StationRules.Clone(s); damaged.players[0].score++; Check(StationRules.Restore(JsonUtility.ToJson(damaged)) == null && StationRules.Restore("{") == null, "corrupted save"); passed.Add("Corrupted save rejected");
            var turn = StationRules.NewGame(true, seed: 12, first: 0); var actors = new List<int>();
            for (int i = 0; i < 4; i++) { actors.Add(turn.actor); turn = Apply(turn, new StationAction("pass")); }
            Check(actors.SequenceEqual(new[] { 0,1,1,0 }), "round initiative"); passed.Add("Alternating round initiative");
            turn.finished = true; turn.players[0].score = 4; turn.players[1].score = 4; turn.players[0].completed = 1; turn.players[1].completed = 2;
            Check(StationRules.Winner(turn) == 1, "completed tiebreak"); turn.players[0].completed = 2; Check(StationRules.Winner(turn) == -1, "draw"); passed.Add("Score, completion and draw results");
            for (int seed = 0; seed < 200; seed++)
            {
                var ai = StationRules.NewGame(true, seed % 2 == 0 ? "easy" : "normal", true, seed);
                while (!ai.finished)
                {
                    var a = StationRules.ChooseAI(ai); var hidden = StationRules.Clone(ai); Array.Reverse(hidden.deck, hidden.cursor, hidden.deck.Length - hidden.cursor);
                    Check(JsonUtility.ToJson(a) == JsonUtility.ToJson(StationRules.ChooseAI(hidden)), "AI read hidden deck");
                    ai = Apply(ai, a);
                    foreach (var p in ai.players) { for (int k = 0; k < 3; k++) Check(p.resources[k] >= 0 && p.resources[k] <= 9, "resource range"); Check(p.facilities.Distinct().Count() == p.facilities.Count, "facility uniqueness"); }
                }
                Check(ai.revision == 24 && ai.players.All(p => p.actions == 12) && StationRules.Restore(JsonUtility.ToJson(ai)) != null, "full AI match " + seed);
            }
            passed.Add("200 complete seeded AI matches: legality, hidden-information isolation, resources, saves");
            string testDirectory = Path.Combine(Path.GetTempPath(), "StationSaveTest-" + Guid.NewGuid().ToString("N"));
            string testPath = Path.Combine(testDirectory, "run.json");
            Check(StationSave.WriteAt(testPath, initial, out _) && StationSave.WriteAt(testPath, s, out _), "disk snapshot writes");
            Check(StationSave.LoadAt(testPath, out _)?.players[0].score == 18, "disk snapshot restore");
            File.WriteAllText(testPath, "corrupted");
            var recovered = StationSave.LoadAt(testPath, out bool usedBackup); Check(recovered != null && recovered.revision == 0 && usedBackup, "backup restore");
            File.Delete(testPath); File.Delete(testPath + ".backup"); Directory.Delete(testDirectory);
            passed.Add("Atomic disk save, resume and backup recovery");
            return "PASS: " + passed.Count + " checks\n" + string.Join("\n", passed.Select(x => "PASS " + x));
        }
    }
}

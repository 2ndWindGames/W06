using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SecondWind.SpaceStation
{
    [Serializable] public struct Resources3
    {
        public int S, E, W;
        public Resources3(int s, int e, int w) { S = s; E = e; W = w; }
        public int this[int i] { get { return i == 0 ? S : i == 1 ? E : W; } set { if (i == 0) S = value; else if (i == 1) E = value; else W = value; } }
        public int Total { get { return S + E + W; } }
        public override string ToString() { return $"고철 {S} · 전력 {E} · 물 {W}"; }
    }
    [Serializable] public class StationAction
    {
        public string type, target;
        public int resource;
        public StationAction(string kind, string id = null, int res = 0) { type = kind; target = id; resource = res; }
    }
    [Serializable] public class StationPlayer
    {
        public Resources3 resources = new Resources3(2, 0, 0);
        public List<string> facilities = new List<string>();
        public int score, completed, actions, collections, replacements;
    }
    [Serializable] public class StationTurn
    {
        public int round, actor, points, score;
        public string label;
        public StationAction action;
        public Resources3 production, overflow, cost, gained, wasted, after;
    }
    [Serializable] public class StationState
    {
        public string rulesVersion = StationRules.Version, runId, lastActionId, difficulty;
        public bool versusAI, shuffled, finished;
        public int seed, revision, round = 1, slot, first, actor, cursor;
        public StationPlayer[] players;
        public string[] deck, market = new string[2];
        public Resources3 production, overflow;
        public List<StationTurn> history = new List<StationTurn>();
    }
    public class OrderDefinition
    {
        public string id, name, description;
        public Resources3 cost;
        public int score;
        public OrderDefinition(string id, string name, string description, int s, int e, int w, int score)
        { this.id = id; this.name = name; this.description = description; cost = new Resources3(s, e, w); this.score = score; }
    }
    public class FacilityDefinition
    {
        public string id, name, shortName;
        public int resource;
        public Resources3 cost;
        public FacilityDefinition(string id, string name, string shortName, int s, int e, int w, int resource)
        { this.id = id; this.name = name; this.shortName = shortName; cost = new Resources3(s, e, w); this.resource = resource; }
    }
    /// <summary>Deterministic rules. Every action returns a new snapshot; rendering and persistence are separate.</summary>
    public static class StationRules
    {
        public const string Version = "unity-0.3.0";
        public static readonly string[] ResourceNames = { "고철", "전력", "물" };
        public static readonly string[] ResourceIds = { "S", "E", "W" };
        public static readonly OrderDefinition[] Orders = {
            new OrderDefinition("A", "탐사선", "미지의 궤도를 향해", 0, 2, 1, 3),
            new OrderDefinition("B", "화물선", "다음 정거장까지 안전하게", 2, 1, 0, 3),
            new OrderDefinition("C", "장거리 조사선", "먼 은하의 신호를 찾아", 0, 3, 2, 5),
            new OrderDefinition("D", "수리 요청선", "다시 항해할 수 있도록", 3, 0, 1, 4),
            new OrderDefinition("E", "개척선", "새로운 터전을 위한 출발", 2, 2, 2, 6),
            new OrderDefinition("F", "소형 연락선", "작은 배에 담긴 큰 소식", 1, 1, 1, 3)
        };
        public static readonly FacilityDefinition[] Facilities = {
            new FacilityDefinition("solar", "태양광 패널", "태양광", 2, 0, 0, 1),
            new FacilityDefinition("condenser", "수분 응축기", "응축기", 2, 1, 0, 2),
            new FacilityDefinition("drone", "회수 드론", "회수 드론", 0, 2, 1, 0)
        };
        public static OrderDefinition Order(string instance) { return string.IsNullOrEmpty(instance) ? null : Orders.FirstOrDefault(o => o.id == instance.Substring(0, 1)); }
        public static FacilityDefinition Facility(string id) { return Facilities.FirstOrDefault(f => f.id == id); }
        public static StationState Clone(StationState s) { return JsonUtility.FromJson<StationState>(JsonUtility.ToJson(s)); }
        public static StationState NewGame(bool ai = false, string difficulty = "normal", bool shuffle = false, int seed = 0, string[] types = null, int first = -1)
        {
            var random = new System.Random(seed);
            var s = new StationState { runId = Guid.NewGuid().ToString("N"), versusAI = ai, difficulty = difficulty == "easy" ? "easy" : "normal", shuffled = ai || shuffle, seed = seed,
                first = ai ? (first < 0 ? random.Next(2) : first) : 0, players = ai ? new[] { new StationPlayer(), new StationPlayer() } : new[] { new StationPlayer() } };
            var list = types == null ? Enumerable.Range(0, ai ? 4 : 2).SelectMany(_ => new[] { "A", "B", "F", "C", "D", "E" }).ToArray() : (string[])types.Clone();
            if (list.Length != (ai ? 24 : 12) || Orders.Any(o => list.Count(x => x == o.id) != (ai ? 4 : 2))) throw new ArgumentException("Invalid order deck");
            if (types == null && (ai || shuffle)) for (int i = list.Length - 1; i > 0; i--) { int j = random.Next(i + 1); string t = list[i]; list[i] = list[j]; list[j] = t; }
            s.deck = list.Select((type, i) => type + "-" + i).ToArray();
            Refill(s); BeginTurn(s); return s;
        }
        public static StationState Restart(StationState s, bool fresh, int seed)
        { return NewGame(s.versusAI, s.difficulty, fresh || s.shuffled, seed, fresh ? null : s.deck.Select(c => c.Substring(0, 1)).ToArray(), fresh ? -1 : s.first); }
        static void Refill(StationState s) { for (int i = 0; i < 2; i++) if (string.IsNullOrEmpty(s.market[i]) && s.cursor < s.deck.Length) s.market[i] = s.deck[s.cursor++]; }
        public static Resources3 Production(StationPlayer p)
        { var r = new Resources3(); foreach (string id in p.facilities) { int k = Facility(id).resource; r[k]++; } return r; }
        static Resources3 Add(StationPlayer p, Resources3 amount, out Resources3 overflow)
        { var added = new Resources3(); overflow = new Resources3(); for (int i = 0; i < 3; i++) { added[i] = Math.Min(9 - p.resources[i], amount[i]); p.resources[i] += added[i]; overflow[i] = amount[i] - added[i]; } return added; }
        static void BeginTurn(StationState s)
        { s.actor = s.versusAI ? (s.first + s.round - 1 + s.slot) % 2 : 0; s.production = Add(s.players[s.actor], Production(s.players[s.actor]), out s.overflow); }
        public static Resources3 Cost(StationState s, StationAction a)
        { if (a.type == "build") return Facility(a.target)?.cost ?? new Resources3(); if (a.type == "supply") return Order(a.target)?.cost ?? new Resources3(); return new Resources3(); }
        public static string Missing(Resources3 have, Resources3 cost)
        { var parts = new List<string>(); for (int i = 0; i < 3; i++) if (have[i] < cost[i]) parts.Add(ResourceNames[i] + " " + (cost[i] - have[i]) + "개"); return string.Join(", ", parts); }
        public static string Validate(StationState s, StationAction a, int actor = -1)
        {
            if (s == null || s.finished) return "이미 종료된 게임입니다.";
            if (actor >= 0 && actor != s.actor) return "아직 내 차례가 아닙니다.";
            if (a == null) return "행동을 선택하세요.";
            var p = s.players[s.actor];
            switch (a.type)
            {
                case "collect": if (a.resource < 0 || a.resource > 2) return "자원을 선택하세요."; if (p.resources[a.resource] == 9) return "보유 상한 9개입니다."; break;
                case "build": if (Facility(a.target) == null) return "시설을 선택하세요."; if (p.facilities.Contains(a.target)) return "이미 설치한 시설입니다."; if (p.facilities.Count >= 3) return "시설 공간이 가득 찼습니다."; break;
                case "supply": case "replace": if (string.IsNullOrEmpty(a.target) || !s.market.Contains(a.target)) return "현재 공개된 주문을 선택하세요."; if (a.type == "replace" && s.cursor >= s.deck.Length) return "덱이 비어 교체할 수 없습니다."; break;
                case "pass": break;
                default: return "알 수 없는 행동입니다.";
            }
            string missing = Missing(p.resources, Cost(s, a)); return missing.Length == 0 ? null : missing + " 부족";
        }
        public static string Label(StationAction a)
        { if (a == null) return "행동을 선택하세요"; if (a.type == "collect") return ResourceNames[a.resource] + " 2개 수집"; if (a.type == "build") return Facility(a.target).name + " 설치"; if (a.type == "pass") return "턴 넘기기"; return Order(a.target).name + (a.type == "supply" ? " 보급" : " 교체"); }
        public static bool TryApply(StationState current, StationAction a, out StationState next, out string error, int actor = -1, int expectedRevision = -1, string actionId = null)
        {
            next = current;
            if (current != null && actionId != null && actionId == current.lastActionId) { error = "이미 처리한 행동입니다."; return false; }
            if (current != null && expectedRevision >= 0 && expectedRevision != current.revision) { error = "상태가 바뀌었습니다. 다시 선택하세요."; return false; }
            error = Validate(current, a, actor); if (error != null) return false;
            var s = Clone(current); var p = s.players[s.actor]; var cost = Cost(s, a);
            var entry = new StationTurn { round = s.round, actor = s.actor, action = new StationAction(a.type, a.target, a.resource), label = Label(a), cost = cost, production = s.production, overflow = s.overflow };
            for (int i = 0; i < 3; i++) p.resources[i] -= cost[i];
            if (a.type == "collect") { var gain = new Resources3(); gain[a.resource] = 2; entry.gained = Add(p, gain, out entry.wasted); p.collections++; }
            if (a.type == "build") p.facilities.Add(a.target);
            if (a.type == "supply" || a.type == "replace")
            {
                if (a.type == "supply") { entry.points = Order(a.target).score; p.score += entry.points; p.completed++; } else p.replacements++;
                s.market[Array.IndexOf(s.market, a.target)] = null; Refill(s);
            }
            p.actions++; entry.after = p.resources; entry.score = p.score; s.history.Add(entry); s.revision++; s.lastActionId = actionId ?? s.runId + ":" + s.revision;
            if (!s.versusAI || s.slot == 1) { if (s.round == 12) s.finished = true; else { s.round++; s.slot = 0; BeginTurn(s); } }
            else { s.slot = 1; BeginTurn(s); }
            next = s; return true;
        }
        public static List<StationAction> LegalActions(StationState s)
        {
            var actions = new List<StationAction>();
            foreach (string id in s.market.Where(c => !string.IsNullOrEmpty(c))) actions.Add(new StationAction("supply", id));
            foreach (var f in Facilities) actions.Add(new StationAction("build", f.id));
            for (int k = 0; k < 3; k++) actions.Add(new StationAction("collect", null, k));
            foreach (string id in s.market.Where(c => !string.IsNullOrEmpty(c))) actions.Add(new StationAction("replace", id));
            actions.Add(new StationAction("pass")); return actions.Where(a => Validate(s, a) == null).ToList();
        }
        static int Deficit(StationState s, Resources3 r)
        { int min = 8; foreach (string id in s.market) { var o = Order(id); if (o == null) continue; int gap = 0; for (int i = 0; i < 3; i++) gap += Math.Max(0, o.cost[i] - r[i]); min = Math.Min(min, gap); } return min; }
        /// <summary>Evaluates only public market/player state and deck count. Future deck contents are never read.</summary>
        public static float Evaluate(StationState s, StationAction a)
        {
            var p = s.players[s.actor]; int remaining = 12 - p.actions; var r = p.resources; var cost = Cost(s, a); var prod = Production(p);
            for (int k = 0; k < 3; k++) r[k] -= cost[k];
            if (a.type == "supply") { var o = Order(a.target); return o.score * 2.6f - cost.Total * .28f + (remaining <= 3 ? 6 : 0) + (s.versusAI && Missing(s.players[1 - s.actor].resources, o.cost).Length == 0 ? 1.1f : 0); }
            if (a.type == "build") { int key = Facility(a.target).resource; return Math.Min(remaining - 1, Math.Max(0, 9 - r[key]) + remaining / 2f) * 1.38f - cost.Total * .5f + 1.4f - (remaining <= 3 ? 8 : 0); }
            if (a.type == "collect") { r[a.resource] = Math.Min(9, r[a.resource] + 2); bool help = remaining > 6 && Facilities.Any(f => !p.facilities.Contains(f.id) && Missing(p.resources, f.cost).Length > 0 && Missing(r, f.cost).Length == 0); return 1.2f + (Deficit(s, p.resources) - Deficit(s, r)) * 2 + (help ? 2.8f : 0) + (prod[a.resource] == 0 ? .8f : -.3f) - p.resources[a.resource] * .22f - (remaining == 1 ? 8 : 0); }
            if (a.type == "replace") return Deficit(s, p.resources) > 4 ? 1.5f : -2;
            return -5;
        }
        public static StationAction ChooseAI(StationState s)
        {
            if (s.finished) return null; var actions = LegalActions(s); var random = new System.Random(s.seed ^ (s.revision + 1) * 65537);
            if (s.difficulty == "easy" && random.NextDouble() < .5) return actions[random.Next(actions.Count)];
            return actions.OrderByDescending(a => Evaluate(s, a)).First();
        }
        public static int Winner(StationState s)
        { if (!s.finished) return -1; if (!s.versusAI) return s.players[0].score >= 14 ? 0 : 1; int diff = s.players[0].score - s.players[1].score; if (diff == 0) diff = s.players[0].completed - s.players[1].completed; return diff == 0 ? -1 : diff > 0 ? 0 : 1; }
        public static StationState Restore(string json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json)) return null;
                var s = JsonUtility.FromJson<StationState>(json);
                if (s == null || s.rulesVersion != Version || s.history == null || s.history.Count > 24 || string.IsNullOrEmpty(s.runId)) return null;
                var replay = NewGame(s.versusAI, s.difficulty, s.shuffled, s.seed, s.deck.Select(c => c.Substring(0, 1)).ToArray(), s.first); replay.runId = s.runId;
                foreach (var h in s.history) { if (replay.actor != h.actor || replay.round != h.round || !TryApply(replay, h.action, out replay, out _)) return null; }
                replay.lastActionId = s.lastActionId;
                return JsonUtility.ToJson(s) == JsonUtility.ToJson(replay) ? s : null;
            }
            catch { return null; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace SecondWind.SpaceStation
{
    public sealed class StationApp : MonoBehaviour
    {
        VisualElement root, page, overlays;
        UIDocument document;
        StationState state;
        StationAction selection;
        string screen = "home", notice;
        bool hasModal, backgroundPaused, sound;
        float aiAt = -1, lockedUntil;
        int lastWidth, lastHeight;
        AudioSource audioSource;
        readonly Dictionary<string, Texture2D> art = new Dictionary<string, Texture2D>();
        public StationState State { get { return state; } }
        public UIDocument Document { get { return document; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<StationApp>() != null) return;
            var go = new GameObject("Space Station Game"); DontDestroyOnLoad(go); go.AddComponent<StationApp>();
        }
        void Awake()
        {
            Application.targetFrameRate = 60; Application.runInBackground = true;
            if (Camera.main == null) { var camera = new GameObject("Station Camera").AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color32(12, 34, 53, 255); camera.orthographic = true; camera.tag = "MainCamera"; DontDestroyOnLoad(camera.gameObject); }
            var settings = Resources.Load<PanelSettings>("Station/StationPanel");
            if (settings == null) { settings = ScriptableObject.CreateInstance<PanelSettings>(); settings.scaleMode = PanelScaleMode.ScaleWithScreenSize; settings.referenceResolution = new Vector2Int(390, 844); settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight; settings.match = 0; }
            document = gameObject.AddComponent<UIDocument>(); document.panelSettings = settings;
            root = document.rootVisualElement; root.AddToClassList("station-root");
            var stylesheet = Resources.Load<StyleSheet>("Station/Station"); if (stylesheet != null) root.styleSheets.Add(stylesheet);
            var font = Resources.Load<Font>("Station/Fonts/NanumGothic-Regular"); if (font != null) root.style.unityFontDefinition = FontDefinition.FromFont(font);
            page = Box("page"); overlays = Box("overlays"); overlays.pickingMode = PickingMode.Ignore;
            root.Add(page); root.Add(overlays);
            sound = PlayerPrefs.GetInt("Station.Sound", 1) == 1;
            audioSource = gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake = false;
            if (FindFirstObjectByType<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
            state = StationSave.Load(out bool recovered); if (recovered) notice = "직전 행동의 저장을 복구했습니다.";
            SafeArea(); Render();
        }
        void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight) SafeArea();
            if (Input.GetKeyDown(KeyCode.Escape)) { if (hasModal) CloseSheet(); else if (screen == "game") Menu(); else if (screen == "result") Home(); else Application.Quit(); }
            if (lockedUntil > 0 && Time.unscaledTime >= lockedUntil) { lockedUntil = 0; Render(); }
            if (!backgroundPaused && !hasModal && screen == "game" && state != null && state.versusAI && !state.finished && state.actor == 1 && aiAt > 0 && Time.unscaledTime >= aiAt)
            { aiAt = -1; Commit(StationRules.ChooseAI(state), 1, state.revision); }
        }
        void OnApplicationPause(bool paused) { backgroundPaused = paused; aiAt = paused ? -1 : Time.unscaledTime + 1; }
        void SafeArea()
        {
            lastWidth = Screen.width; lastHeight = Screen.height;
            float scale = 390f / Mathf.Max(1, Screen.width); Rect safe = Screen.safeArea;
            root.style.paddingTop = (Screen.height - safe.yMax) * scale; root.style.paddingBottom = safe.y * scale;
            root.style.paddingLeft = safe.x * scale; root.style.paddingRight = (Screen.width - safe.xMax) * scale;
        }
        static VisualElement Box(string classes)
        { var v = new VisualElement(); foreach (string c in classes.Split(' ')) if (c.Length > 0) v.AddToClassList(c); return v; }
        static Label Text(string value, string classes = "")
        { var l = new Label(value); foreach (string c in classes.Split(' ')) if (c.Length > 0) l.AddToClassList(c); return l; }
        Button Button(string text, Action action, string classes = "secondary", bool enabled = true)
        {
            var b = new Button(() => { Beep(0); action(); }) { text = text }; b.AddToClassList("button");
            foreach (string c in classes.Split(' ')) if (c.Length > 0) b.AddToClassList(c); b.SetEnabled(enabled); return b;
        }
        Image Image(string id, string cls = "")
        {
            if (!art.TryGetValue(id, out Texture2D texture)) { texture = Resources.Load<Texture2D>("Station/Art/" + id); art[id] = texture; }
            var img = new Image { image = texture, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            foreach (string c in cls.Split(' ')) if (c.Length > 0) img.AddToClassList(c); return img;
        }
        void Beep(int kind)
        {
            if (!sound || audioSource == null) return;
            const int rate = 22050; int size = kind == 1 ? 6000 : 1800; var samples = new float[size];
            for (int i = 0; i < size; i++) { float freq = kind == 1 ? (i < 2000 ? 523 : i < 4000 ? 659 : 784) : 440; samples[i] = Mathf.Sin(2 * Mathf.PI * freq * i / rate) * .065f * (1f - (float)i / size); }
            var clip = AudioClip.Create("Station UI", size, 1, rate, false); clip.SetData(samples, 0); audioSource.PlayOneShot(clip); Destroy(clip, 1);
        }
        void Render()
        {
            if (state != null && state.finished && screen == "game") screen = "result";
            page.Clear(); if (screen == "home") RenderHome(); else if (screen == "result") RenderResult(); else RenderGame();
            if (screen == "game" && state.actor == 1 && !hasModal && aiAt < 0) aiAt = Time.unscaledTime + 1;
        }
        void Home() { screen = "home"; selection = null; aiAt = -1; CloseSheet(false); Render(); }
        void StartGame(StationState next)
        {
            if (!StationSave.Write(next, out notice)) { ShowMessage("저장 확인", notice); return; }
            state = next; selection = null; screen = "game"; lockedUntil = 0; aiAt = -1; CloseSheet(false); Render();
        }
        int Seed() { return unchecked(Environment.TickCount ^ (int)DateTime.UtcNow.Ticks); }
        void RequestNew(bool ai, string difficulty, bool shuffle)
        {
            Action start = () => StartGame(StationRules.NewGame(ai, difficulty, shuffle, Seed()));
            if (state != null && !state.finished && state.revision > 0) { var sheet = Sheet("새 항해를 시작할까요?"); sheet.Add(Text("현재 진행 중인 게임이 새 게임으로 바뀝니다.", "body")); sheet.Add(Button("새 게임 시작", start, "primary")); }
            else start();
        }
        void RenderHome()
        {
            var scroll = new ScrollView { verticalScrollerVisibility = ScrollerVisibility.Hidden }; scroll.AddToClassList("home"); page.Add(scroll);
            var top = Box("row spread"); top.Add(Text("SECOND WIND / B-01", "eyebrow")); top.Add(Button(sound ? "소리 켜짐" : "소리 꺼짐", ToggleSound, "text-button")); scroll.Add(top);
            scroll.Add(Text("SPACE STATION SUPPLY", "eyebrow home-eyebrow")); scroll.Add(Text("우주 정거장", "home-title")); scroll.Add(Text("보급소", "home-title mint"));
            scroll.Add(Text("작은 정거장에서 시작하는 큰 항해.\n우주선의 다음 여정을 도와주세요.", "body")); scroll.Add(Image("station", "hero"));
            if (!string.IsNullOrEmpty(notice)) scroll.Add(Text(notice, "notice"));
            if (state != null) scroll.Add(Button((state.finished ? "지난 항해 결과" : "항해 이어하기") + $"   ·   {state.players[0].score}점", () => { screen = state.finished ? "result" : "game"; Render(); }, "resume"));
            var label = Box("row spread mode-label"); label.Add(Text("오늘의 항해", "heading")); label.Add(Text("한 판 12턴", "small")); scroll.Add(label);
            var practice = Button("", () => Modes(false), "mode cream"); practice.Add(Image("icon-star", "mode-icon")); var pt = Box("grow"); pt.Add(Text("연습 모드", "mode-title")); pt.Add(Text("나만의 속도로, 신뢰도 14점에 도전", "mode-sub")); practice.Add(pt); practice.Add(Text("›", "arrow")); scroll.Add(practice);
            var ai = Button("", () => Modes(true), "mode"); ai.Add(Image("icon-moon", "mode-icon")); var at = Box("grow"); at.Add(Text("루미 AI와 대전", "mode-title")); at.Add(Text("같은 주문을 두고 펼치는 보급 경쟁", "mode-sub")); ai.Add(at); ai.Add(Text("›", "arrow")); scroll.Add(ai);
            var foot = Box("row spread"); foot.Add(Text("OFFLINE PLAY · UNITY v0.3", "eyebrow")); foot.Add(Button("플레이 방법", Help, "text-button")); scroll.Add(foot);
            int best = PlayerPrefs.GetInt("Station.Best", 0); if (best > 0) scroll.Add(Text("연습 최고 기록 · " + best + "점", "small centered"));
        }
        void Modes(bool ai)
        {
            var sheet = Sheet(ai ? "루미 AI와 대전" : "연습 모드");
            sheet.Add(Text(ai ? "각자 12턴, 공유 주문 2장. 매 라운드 선공이 교대합니다. 루미는 공개된 정보만으로 행동합니다." : "12턴 종료 시 신뢰도 14점을 얻으세요. 첫 항해는 기본 덱으로 시작해 보세요.", "body"));
            sheet.Add(Button(ai ? "쉬움\n가끔 무작위 행동을 선택하는 상대" : "기본 덱\n정해진 순서와 첫 4턴 안내", () => RequestNew(ai, "easy", ai), "choice"));
            sheet.Add(Button(ai ? "보통\n시설과 보급 기회를 비교하는 상대" : "새 덱\n주문 12장을 새로 섞어 도전", () => RequestNew(ai, "normal", true), "choice"));
        }
        void Heading(VisualElement parent, string title, string right)
        { var row = Box("row spread section-heading"); row.Add(Text(title, "heading")); row.Add(Text(right, "small")); parent.Add(row); }
        VisualElement Costs(Resources3 cost, Resources3 have)
        {
            var row = Box("row costs"); for (int k = 0; k < 3; k++) if (cost[k] > 0) { var cell = Box("row cost"); cell.Add(Image("icon-" + StationRules.ResourceIds[k], "resource-icon-small")); cell.Add(Text(cost[k].ToString(), have[k] < cost[k] ? "cost-number missing" : "cost-number")); row.Add(cell); } return row;
        }
        void RenderGame()
        {
            var p = state.players[0]; var game = Box("game grow"); page.Add(game);
            var top = Box("row spread game-header"); var brand = Box("grow"); brand.Add(Text("보급소", "brand")); brand.Add(Text("STATION B-01", "eyebrow")); top.Add(brand);
            top.Add(Text(state.round.ToString("00") + " / 12", "turn")); top.Add(Button("☰", Menu, "menu-button")); game.Add(top);
            if (state.versusAI)
            {
                var rival = state.players[1]; var strip = Button("", Rival, "rival cream row"); var desc = Box("grow"); desc.Add(Text("루미 AI · " + (state.difficulty == "easy" ? "쉬움" : "보통"), "heading")); desc.Add(Text(rival.resources.ToString(), "rival-resources")); strip.Add(desc); strip.Add(Text(rival.score + "점", "rival-score")); game.Add(strip);
            }
            else { var strip = Box("status row spread"); var task = Box("grow"); task.Add(Text("오늘의 보급 임무", "small")); task.Add(Text("신뢰도 14점 달성", "heading")); strip.Add(task); strip.Add(Text(p.score + " / 14", "score mint")); game.Add(strip); }
            var turn = Box("row spread turn-strip"); turn.Add(Text(state.actor == 0 ? "● 내 차례" : "● 루미 AI의 차례", "small mint")); turn.Add(Text(state.versusAI ? "내 신뢰도 " + p.score + "점 · " + p.actions + "/12 행동" : "혼자서 운영하는 정거장", "small")); game.Add(turn);
            var board = new ScrollView { verticalScrollerVisibility = ScrollerVisibility.Hidden }; board.AddToClassList("board"); game.Add(board);
            Heading(board, state.versusAI ? "01  공유 주문" : "01  도착한 주문", "남은 덱 " + (state.deck.Length - state.cursor) + "장");
            var cards = Box("row orders"); for (int i = 0; i < 2; i++) cards.Add(OrderCard(state.market[i], i)); board.Add(cards);
            var resources = Box("row resource-bar cream"); var production = StationRules.Production(p);
            for (int i = 0; i < 3; i++) { var cell = Box("row resource-cell"); cell.Add(Image("icon-" + StationRules.ResourceIds[i], "resource-icon")); var amount = Box(""); amount.Add(Text(StationRules.ResourceNames[i], "resource-name")); amount.Add(Text(p.resources[i] + "/9", "resource-number")); amount.Add(Text(production[i] > 0 ? "매 턴 +" + production[i] : "자동 생산 없음", "resource-production")); cell.Add(amount); resources.Add(cell); }
            board.Add(resources); Heading(board, "02  내 정거장", "시설 " + p.facilities.Count + "/3");
            var facilities = Box("row facilities"); for (int i = 0; i < 3; i++) { string id = i < p.facilities.Count ? p.facilities[i] : null; var b = Button("", () => { if (id == null) BuildSheet(); else FacilityDetail(id); }, "facility " + (id == null ? "empty" : "installed")); if (id == null) { b.Add(Text("+", "plus")); b.Add(Text("시설 설치", "small")); } else { var f = StationRules.Facility(id); b.Add(Image(id, "facility-art")); b.Add(Text(f.shortName, "facility-name")); b.Add(Text(StationRules.ResourceNames[f.resource] + " +1 / 턴", "small mint")); } facilities.Add(b); }
            board.Add(facilities); board.Add(Text(Hint(), "hint"));
            if (state.history.Count > 0) { var h = state.history.Last(); string message = (h.actor == 1 ? "루미: " : "") + h.label + (h.points > 0 ? " · 신뢰도 +" + h.points : ""); if (state.actor == 0 && state.production.Total > 0) message += "\n이번 턴 생산: " + Compact(state.production); if (state.overflow.Total > 0) message += "\n보유 상한 초과: " + Compact(state.overflow); if (h.wasted.Total > 0) message += "\n수집 초과: " + Compact(h.wasted); board.Add(Text(message, "notice")); }
            if (!string.IsNullOrEmpty(notice)) board.Add(Text(notice, "notice"));
            var dock = Box("dock"); var tabs = Box("row tabs"); tabs.Add(Button("수집", CollectSheet, "tab")); tabs.Add(Button("시설", BuildSheet, "tab")); tabs.Add(Button("교체", ReplaceSheet, "tab")); dock.Add(tabs);
            var summary = Box("row spread selection"); summary.Add(Text(StationRules.Label(selection), "selection-name")); string error = state.actor != 0 ? "루미가 생각하고 있어요" : StationRules.Validate(state, selection, 0); summary.Add(Text(error ?? (selection.type == "supply" ? "신뢰도 +" + StationRules.Order(selection.target).score : "1턴 사용"), "small " + (error == null ? "mint" : "coral"))); dock.Add(summary);
            var chosen = selection; int revision = state.revision; string title = state.actor != 0 ? "루미의 행동을 기다리는 중" : selection == null ? "행동을 선택하세요" : selection.type == "supply" ? "보급 실행" : selection.type == "build" ? "시설 설치" : selection.type == "replace" ? "주문 교체" : selection.type == "collect" ? "수집 실행" : "턴 넘기기";
            var execute = Button(title, () => Commit(chosen, 0, revision), "primary", error == null && Time.unscaledTime >= lockedUntil); execute.name = "execute"; dock.Add(execute);
            dock.Add(Text(!state.versusAI && !state.shuffled && state.round <= 4 ? new[] { "", "첫 항해: 시설에서 태양광 패널을 선택하세요", "수집으로 고철 2개를 모아 보세요", "시설에서 수분 응축기를 준비하세요", "탐사선을 선택하고 보급을 실행하세요" }[state.round] : "행동마다 자동 저장 · " + (state.versusAI ? "각자 12턴" : "12턴 후 최종 정산"), "dock-note")); game.Add(dock);
        }
        Button OrderCard(string id, int slot)
        {
            if (string.IsNullOrEmpty(id)) return Button("모든 주문 보급 완료", () => { }, "order empty", false);
            var o = StationRules.Order(id); string missing = StationRules.Missing(state.players[0].resources, o.cost);
            var b = Button("", () => OrderDetail(id), "order" + (selection != null && selection.target == id ? " selected" : ""));
            var header = Box("row spread card-header"); var left = Box("grow"); left.Add(Text("DOCK 0" + (slot + 1) + " / " + o.id, "card-id")); left.Add(Text(o.name, "card-name")); header.Add(left); header.Add(Text(o.score + "\n점", "card-points")); b.Add(header);
            var scene = Box("card-scene"); scene.Add(Image("ship-" + o.id, "ship")); b.Add(scene); b.Add(Costs(o.cost, state.players[0].resources)); b.Add(Text(missing.Length == 0 ? "보급할 수 있어요" : "자원을 준비해 주세요", "card-state")); return b;
        }
        string Hint()
        { if (state.versusAI) return "공개 주문 2장을 함께 사용합니다. 상대가 보급하면 새로운 주문이 도착합니다."; if (state.round >= 10) return "항해가 곧 끝납니다. 남은 자원은 점수가 되지 않으니 보급할 주문을 비교하세요."; return "시설은 다음 내 차례부터 생산합니다. 카드를 탭해 비용과 효과를 확인하세요."; }
        string Compact(Resources3 r)
        { var parts = new List<string>(); for (int i = 0; i < 3; i++) if (r[i] > 0) parts.Add(StationRules.ResourceNames[i] + " " + r[i]); return parts.Count > 0 ? string.Join(" · ", parts) : "없음"; }
        void Select(StationAction action) { selection = action; CloseSheet(false); Render(); }
        void OrderDetail(string id)
        {
            var o = StationRules.Order(id); selection = new StationAction("supply", id); Render(); var sheet = Sheet(o.name);
            sheet.Add(Image("ship-" + o.id, "detail-art")); sheet.Add(Text(o.description, "body")); sheet.Add(Text("보급 비용: " + Compact(o.cost), "body")); sheet.Add(Text("보급 완료 시 신뢰도 +" + o.score, "heading mint"));
            string missing = StationRules.Missing(state.players[0].resources, o.cost); sheet.Add(Text(missing.Length > 0 ? missing + " 부족" : "지금 보급할 수 있습니다.", "body")); sheet.Add(Button("선택하고 돌아가기", () => CloseSheet(), "primary")); sheet.Add(Text("돌아간 뒤 하단 실행 버튼으로 확정합니다.", "small centered"));
        }
        void CollectSheet()
        {
            var sheet = Sheet("어떤 자원을 모을까요?"); sheet.Add(Text("한 종류를 2개 얻습니다. 행동 1회를 사용하며 종류별 보유 상한은 9개입니다.", "body"));
            for (int i = 0; i < 3; i++) { int k = i; int have = state.players[0].resources[k]; var b = Button("", () => Select(new StationAction("collect", null, k)), "choice row", have < 9); b.Add(Image("icon-" + StationRules.ResourceIds[k], "resource-icon")); b.Add(Text(StationRules.ResourceNames[k] + " 수집\n현재 " + have + " → " + Math.Min(9, have + 2) + "개", "body")); sheet.Add(b); }
        }
        void BuildSheet()
        {
            var sheet = Sheet("정거장에 시설 설치"); sheet.Add(Text("다음 내 차례부터 자원을 1개씩 생산합니다. 각 종류를 한 개씩 설치할 수 있어요.", "body"));
            foreach (var f in StationRules.Facilities.Where(f => !state.players[0].facilities.Contains(f.id))) { var b = Button("", () => Select(new StationAction("build", f.id)), "choice row"); b.Add(Image(f.id, "choice-art")); var desc = Box("grow"); desc.Add(Text(f.name, "heading")); desc.Add(Text(Compact(f.cost), "small")); string missing = StationRules.Missing(state.players[0].resources, f.cost); desc.Add(Text(missing.Length == 0 ? StationRules.ResourceNames[f.resource] + " +1 / 턴" : missing + " 부족", "small mint")); b.Add(desc); sheet.Add(b); }
            if (state.players[0].facilities.Count == 3) sheet.Add(Text("모든 시설을 설치했습니다.", "notice"));
        }
        void FacilityDetail(string id)
        { var f = StationRules.Facility(id); var sheet = Sheet(f.name); sheet.Add(Image(id, "detail-art")); sheet.Add(Text("설치 완료\n다음 내 차례부터 " + StationRules.ResourceNames[f.resource] + " +1\n보유 상한은 9개입니다.", "body")); sheet.Add(Button("확인", () => CloseSheet(), "primary")); }
        void ReplaceSheet()
        {
            var sheet = Sheet("공개 주문 교체"); sheet.Add(Text("주문 한 장을 포기하고 다음 주문을 받습니다. 자원 비용은 없지만 1턴을 사용합니다.", "body"));
            if (state.cursor >= state.deck.Length) { sheet.Add(Text("덱이 비어 주문을 교체할 수 없습니다.", "notice")); return; }
            foreach (string id in state.market.Where(x => !string.IsNullOrEmpty(x))) sheet.Add(Button(StationRules.Order(id).name + " 교체", () => Select(new StationAction("replace", id)), "choice"));
        }
        void Rival()
        { var p = state.players[1]; var sheet = Sheet("루미 AI의 정거장"); sheet.Add(Text("신뢰도 " + p.score + "점 · 보급 " + p.completed + "회\n" + p.resources + "\n매 턴 생산: " + Compact(StationRules.Production(p)), "body")); foreach (string id in p.facilities) sheet.Add(Text(StationRules.Facility(id).name, "notice")); }
        void ToggleSound() { sound = !sound; PlayerPrefs.SetInt("Station.Sound", sound ? 1 : 0); PlayerPrefs.Save(); CloseSheet(false); Render(); }
        void Menu()
        {
            var sheet = Sheet("항해 잠시 멈춤"); sheet.Add(Text(state.round + "턴 상태를 저장했습니다. 메뉴를 닫으면 이어집니다.", "body"));
            sheet.Add(Button("계속하기", () => CloseSheet(), "choice")); sheet.Add(Button("턴별 운영 기록", History, "choice")); sheet.Add(Button("플레이 방법", Help, "choice")); sheet.Add(Button("효과음 " + (sound ? "끄기" : "켜기"), ToggleSound, "choice"));
            sheet.Add(Button("이번 턴 넘기기", () => Select(new StationAction("pass")), "choice", state.actor == 0)); sheet.Add(Button("저장하고 시작 화면으로", Home, "choice"));
        }
        void Help()
        {
            var sheet = Sheet("보급소 운영 안내"); sheet.Add(Text("한 턴에 한 번 행동합니다.\n\n수집  ·  자원 한 종류를 2개 획득\n시설  ·  다음 내 차례부터 자동 생산\n보급  ·  주문 비용을 내고 신뢰도 획득\n교체  ·  주문을 버리고 다음 주문 공개\n\n주문을 탭하면 비용과 보상을 확인할 수 있습니다. 하단 실행 버튼으로 행동을 확정하세요. 상세를 닫아도 턴을 사용하지 않습니다.\n\n연습은 12턴 종료 시 14점 이상이면 성공합니다.\n\nAI 대전은 각자 12회 행동합니다. 매 라운드 선공이 바뀌며, 점수와 완료 주문 수 순으로 승패를 가립니다.", "body")); sheet.Add(Button("알겠어요", () => CloseSheet(), "primary"));
        }
        VisualElement Sheet(string title)
        {
            hasModal = true; aiAt = -1; overlays.Clear(); overlays.pickingMode = PickingMode.Position;
            var background = Box("modal"); background.RegisterCallback<ClickEvent>(e => { if (e.target == background) CloseSheet(); }); overlays.Add(background);
            var frame = Box("sheet"); background.Add(frame); var header = Box("row spread"); header.Add(Text(title, "sheet-title")); header.Add(Button("×", () => CloseSheet(), "menu-button")); frame.Add(header);
            var content = new ScrollView { verticalScrollerVisibility = ScrollerVisibility.Hidden }; content.AddToClassList("sheet-content"); frame.Add(content); return content;
        }
        void CloseSheet(bool render = true) { hasModal = false; overlays.Clear(); overlays.pickingMode = PickingMode.Ignore; aiAt = -1; if (render) Render(); }
        void ShowMessage(string title, string message) { var sheet = Sheet(title); sheet.Add(Text(message, "body")); sheet.Add(Button("확인", () => CloseSheet(), "primary")); }
        void Commit(StationAction action, int actor, int revision)
        {
            if (Time.unscaledTime < lockedUntil || screen != "game") return;
            if (!StationRules.TryApply(state, action, out var next, out var error, actor, revision, state.runId + ":" + (revision + 1))) { notice = error; Render(); return; }
            if (!StationSave.Write(next, out notice)) { Render(); return; }
            state = next; selection = null; aiAt = -1; lockedUntil = Time.unscaledTime + .35f;
            if (action.type == "supply") Beep(1);
            if (state.finished) { screen = "result"; if (!state.versusAI && state.players[0].score > PlayerPrefs.GetInt("Station.Best", 0)) { PlayerPrefs.SetInt("Station.Best", state.players[0].score); PlayerPrefs.Save(); } }
            Render();
        }
        void RenderResult()
        {
            var p = state.players[0]; int winner = StationRules.Winner(state); var scroll = new ScrollView { verticalScrollerVisibility = ScrollerVisibility.Hidden }; scroll.AddToClassList("result"); page.Add(scroll);
            scroll.Add(Text("MISSION REPORT / B-01", "eyebrow")); scroll.Add(Image(winner == 0 ? "icon-star" : "icon-moon", "result-medal")); scroll.Add(Text("12 " + (state.versusAI ? "ROUNDS" : "TURNS") + " COMPLETE", "eyebrow centered"));
            string title = state.versusAI ? winner == 0 ? "대전 승리" : winner == 1 ? "루미의 승리" : "멋진 무승부" : p.score >= 20 ? "우수 운영" : p.score >= 14 ? "보급 임무 성공" : "다음 항해를 준비해요";
            scroll.Add(Text(title, "result-title")); scroll.Add(Text(state.versusAI ? "나  " + p.score + "  :  " + state.players[1].score + "  루미" : p.score + "점", "final-score"));
            scroll.Add(Text(state.versusAI ? "신뢰도와 보급 완료 수 순으로 결정" : "목표 신뢰도 14점", "small centered")); var stats = Box("row stats cream");
            foreach (var pair in new[] { ("보급", p.completed), ("시설", p.facilities.Count), ("수집", p.collections), ("교체", p.replacements) }) { var cell = Box("stat"); cell.Add(Text(pair.Item2.ToString(), "stat-number")); cell.Add(Text(pair.Item1, "stat-label")); stats.Add(cell); } scroll.Add(stats);
            int total = state.history.Where(h => h.actor == 0).Sum(h => h.production.Total); scroll.Add(Text($"시설이 자원 {total}개를 생산했습니다.\n{p.completed}번의 보급으로 신뢰도 {p.score}점을 얻었어요.", "body"));
            scroll.Add(Button("턴별 운영 기록", History, "choice")); scroll.Add(Button("같은 덱으로 다시하기", () => StartGame(StationRules.Restart(state, false, Seed())), "primary")); scroll.Add(Button("새 덱으로 출발", () => StartGame(StationRules.Restart(state, true, Seed())), "secondary")); scroll.Add(Button("시작 화면으로", Home, "text-button"));
        }
        void History()
        {
            var sheet = Sheet("턴별 운영 기록"); if (state.history.Count == 0) sheet.Add(Text("첫 행동을 실행하면 기록이 쌓입니다.", "body"));
            foreach (var h in state.history) { var item = Box("history-item"); item.Add(Text(h.round.ToString("00") + "턴 · " + (h.actor == 0 ? "나" : "루미 AI") + (h.points > 0 ? "   +" + h.points + "점" : ""), "small mint")); item.Add(Text(h.label, "heading")); item.Add(Text("생산 " + Compact(h.production) + "\n사용 " + Compact(h.cost) + (h.gained.Total > 0 ? " / 수집 " + Compact(h.gained) : "") + "\n행동 후 " + h.after + " / " + h.score + "점", "small")); sheet.Add(item); }
        }
#if UNITY_EDITOR
        public void SetEditorPreview(int phase)
        {
            state = null; selection = null; notice = null; screen = "home";
            if (phase > 0)
            {
                state = StationRules.NewGame(seed: 1234);
                var actions = new[] { new StationAction("build", "solar"), new StationAction("collect", null, 0), new StationAction("build", "condenser"), new StationAction("supply", "A-0"), new StationAction("collect", null, 0), new StationAction("supply", "F-2"), new StationAction("collect", null, 0), new StationAction("supply", "C-3"), new StationAction("supply", "B-1"), new StationAction("collect", null, 0), new StationAction("supply", "D-4"), new StationAction("pass") };
                for (int i = 0; i < (phase == 1 ? 3 : actions.Length); i++) StationRules.TryApply(state, actions[i], out state, out _);
                screen = phase == 1 ? "game" : "result";
            }
            Render();
        }
#endif
    }
}

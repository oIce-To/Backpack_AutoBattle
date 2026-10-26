using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Enemies;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Prototype;
using BackpackAutoBattle.Shop;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BackpackAutoBattle.Tests.Prototype
{
    public sealed class PrototypeCombatStatisticsTests
    {
        private BackpackPrototypeController _controller;
        private readonly List<Object> _temporary = new();
        [TearDown] public void Cleanup() { foreach (var item in _temporary) if (item != null) Object.Destroy(item); _temporary.Clear(); }
        private IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single); yield return null;
            _controller = Object.FindFirstObjectByType<BackpackPrototypeController>(); _controller.BattleController.AutoTick = false;
        }
        [UnityTest]
        public IEnumerator VictoryStatistics_OpenSwitchSidesAndClose_WithoutDoubleCountingOrAdvancingRound()
        {
            yield return Setup(); _controller.TryStartBattle();
            Assert.That(_controller.BattleController.ShowStatistics(), Is.False);
            Assert.That(Find("Battle Statistics Button").gameObject.activeSelf, Is.False);
            Win();
            var snapshot = _controller.ActiveCombat.CaptureStatistics(); int gold = _controller.Gold;
            Click("Battle Statistics Button"); yield return null;
            Assert.That(Find("Combat Statistics Screen").gameObject.activeInHierarchy, Is.True);
            Assert.That(Rows(), Has.Length.EqualTo(3));
            var sword = Rows().Single(r => r.name == "Statistics Row starter_training_blade");
            Assert.That(sword.GetComponentsInChildren<Text>().Single(t => t.name == "Statistics Value").text, Is.EqualTo("30"));
            Assert.That(sword.GetComponentsInChildren<Text>().Single(t => t.name == "Statistics Item Name").text, Is.EqualTo("훈련용 검"));
            Assert.That(Text("Statistics Totals"), Does.Contain("공격 30")); Capture("PlayerVictoryStatistics.png");
            Click("Enemy Statistics Tab"); yield return null;
            Assert.That(Rows(), Has.Length.EqualTo(1)); Assert.That(Text("Statistics Totals"), Does.Contain("공격 0"));
            Click("Close Statistics Button"); Assert.That(Find("Combat Statistics Screen").gameObject.activeSelf, Is.False);
            Assert.That(_controller.BattleController.ShowStatistics(), Is.True); yield return null;
            Assert.That(Text("Statistics Totals"), Does.Contain("공격 30"));
            Assert.That(_controller.Gold, Is.EqualTo(gold)); Assert.That(_controller.CurrentRound, Is.EqualTo(1));
            Assert.That(_controller.Lives, Is.EqualTo(3));
            _controller.AdvanceRound(); yield return null;
            Assert.That(Find("Combat Statistics Screen").gameObject.activeSelf, Is.False);
            _controller.TryStartBattle();
            Assert.That(_controller.ActiveCombat.CaptureStatistics().Items.Sum(i => i.DamageDealt), Is.Zero);
            Assert.That(snapshot.Items.Sum(i => i.DamageDealt), Is.EqualTo(30));
        }
        [UnityTest]
        public IEnumerator DefeatAndGameOver_BothAllowStatistics_AndRestartDiscardsOnlyOldBattleRecords()
        {
            yield return Setup();
            for (int i = 0; i < 3; i++)
            {
                _controller.TryStartBattle();
                _controller.ActiveCombat.PlayerWeapons.Single().Stats.AttackDamage = 0;
                _controller.ActiveCombat.EnemyWeapons.Single().Stats.AttackDamage = 1000;
                _controller.ActiveCombat.EnemyWeapons.Single().Stats.AccuracyPercent = 100;
                _controller.TickCombat(2);
                Click(i == 2 ? "Run Statistics Button" : "Battle Statistics Button"); yield return null;
                Click("Enemy Statistics Tab"); yield return null;
                Assert.That(Text("Statistics Totals"), Does.Contain("공격 52.5"));
                if (i == 2)
                {
                    Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.GameOver));
                    Assert.That(Find("Run Result Screen").gameObject.activeInHierarchy, Is.True);
                    Assert.That(Find("Combat Statistics Screen").GetSiblingIndex(), Is.GreaterThan(Find("Run Result Screen").GetSiblingIndex()));
                    Capture("GameOverEnemyStatistics.png");
                }
                Click("Close Statistics Button");
                if (i < 2) _controller.ReturnToPreparation();
            }
            Click("Restart Run Button"); yield return null;
            Assert.That(Find("Combat Statistics Screen").gameObject.activeSelf, Is.False);
            Assert.That(_controller.ActiveCombat, Is.Null); Assert.That(_controller.Lives, Is.EqualTo(3));
            _controller.TryStartBattle(); Assert.That(_controller.ActiveCombat.CaptureStatistics().Items.Sum(i => i.DamageDealt), Is.Zero);
        }
        [UnityTest]
        public IEnumerator FinalClearStatistics_OverlayResultWithoutReplacingIt()
        {
            yield return Setup(); for (int i = 0; i < 11; i++) _controller.AdvanceRound();
            _controller.TryStartBattle(); Win();
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Completed));
            Click("Run Statistics Button"); yield return null;
            Assert.That(Text("Statistics Totals"), Does.Contain("공격 85"));
            Assert.That(Find("Run Result Screen").gameObject.activeInHierarchy, Is.True);
            Capture("ClearedBattleStatistics.png");
            Click("Close Statistics Button"); Assert.That(Find("Run Result Screen").gameObject.activeInHierarchy, Is.True);
        }
        [UnityTest]
        public IEnumerator ManyEnemyItems_StaySeparateAndScrollable_WithZeroContributionRowsVisible()
        {
            yield return Setup(); var enemy = Object.Instantiate(_controller.CurrentEnemyEncounter);
            try
            {
                var items = new List<EnemyItemPlacement> { new("club", enemy.Items[0].Item, new Vector2Int(1, 0)) };
                for (int y = 0; y < 4; y++)
                    for (int x = 0; x < 6; x++)
                        if (!(x == 1 && y < 2)) items.Add(new EnemyItemPlacement("potion_" + x + "_" + y, Item("healing_potion"), new Vector2Int(x, y)));
                Set(enemy, "_items", items); Set(_controller, "_debugEnemyOverride", enemy);
                Assert.That(_controller.RefreshEncounterPreview(), Is.True);
                _controller.TryStartBattle(); Win(); Click("Battle Statistics Button"); Click("Enemy Statistics Tab"); yield return null;
                Assert.That(Rows(), Has.Length.EqualTo(items.Count));
                Assert.That(Rows().Select(r => r.name).Distinct().Count(), Is.EqualTo(items.Count));
                Assert.That(Find("Statistics Rows").GetComponent<RectTransform>().rect.height, Is.GreaterThan(252));
                var scroll = Find("Statistics Viewport").GetComponent<ScrollRect>(); scroll.verticalNormalizedPosition = 0;
                Canvas.ForceUpdateCanvases(); Capture("EnemyStatisticsScrollableItems.png");
            }
            finally { Object.Destroy(enemy); }
        }
        [UnityTest]
        public IEnumerator RealFireStaffAndPoisonVial_KeepTheirOwnDotStatisticsAfterVictory()
        {
            yield return Setup(); var enemy = Object.Instantiate(_controller.CurrentEnemyEncounter);
            try
            {
                Set(enemy, "_maximumHealth", 300); Set(_controller, "_debugEnemyOverride", enemy); _controller.RefreshEncounterPreview();
                Buy("fire_staff", 3, 0); Buy("poison_vial", 4, 4); yield return null;
                _controller.TryStartBattle();
                _controller.ActiveCombat.PlayerWeapons.Single(w => w.Snapshot.ItemId == "fire_staff").Stats.AccuracyPercent = 100;
                _controller.TickCombat(4.3f);
                var data = _controller.ActiveCombat.CaptureStatistics();
                Assert.That(data.Items.Single(r => r.Side == CombatSide.Player && r.DisplayName == "화염 지팡이").StatusDamage, Is.EqualTo(2.4).Within(1e-5));
                Assert.That(data.Items.Single(r => r.Side == CombatSide.Player && r.DisplayName == "맹독 약병").StatusDamage, Is.EqualTo(2).Within(1e-5));
                Win(); Click("Battle Statistics Button"); yield return null;
                Assert.That(Rows().Any(r => r.GetComponentsInChildren<Text>().Any(t => t.text.Contains("화염 지팡이"))), Is.True);
                Capture("FireAndPoisonItemStatistics.png");
            }
            finally { Object.Destroy(enemy); }
        }
        [UnityTest]
        public IEnumerator ArmorExample_Displays333And267_WithoutCountingRemainingArmor()
        {
            yield return Setup(); var a = Object.Instantiate(Item("wooden_shield")); var b = Object.Instantiate(a);
            try
            {
                Set(a, "_displayName", "방어도 A"); Set(b, "_displayName", "방어도 B");
                Set(a, "_activation", new ItemActivationData(1, new ItemActivationEffect(ItemEffectKind.GainArmor, 50)));
                Set(b, "_activation", new ItemActivationData(1, new ItemActivationEffect(ItemEffectKind.GainArmor, 40)));
                BuyData(a, 3, 0); BuyData(b, 0, 3); yield return null;
                _controller.TryStartBattle(); _controller.TickCombat(.91f);
                foreach (var item in _controller.ActiveCombat.PlayerItems.Where(i => i.DisplayName is "방어도 A" or "방어도 B")) item.Cycle.SetBaseInterval(100);
                _controller.ActiveCombat.ApplyNonAttackDamage(CombatSide.Enemy, CombatSide.Player, 60, CombatDamageKind.Other);
                Assert.That(_controller.ActiveCombat.PlayerArmor, Is.EqualTo(30)); Win();
                Click("Battle Statistics Button"); Click("Defense Statistics Tab"); yield return null;
                Assert.That(ValueFor("방어도 A"), Is.EqualTo("33.3"));
                Assert.That(ValueFor("방어도 B"), Is.EqualTo("26.7"));
                Assert.That(Text("Statistics Totals"), Does.Contain("방어 60")); Capture("ArmorProportionalStatistics.png");
            }
            finally { Object.Destroy(a); Object.Destroy(b); }
        }
        [UnityTest]
        public IEnumerator ActualReactionItems_DisplayHealingReflectionAndAbsorptionOnTheirOwnRows()
        {
            yield return Setup(); var enemy = Object.Instantiate(_controller.CurrentEnemyEncounter);
            try
            {
                Set(enemy, "_maximumHealth", 300); Set(_controller, "_debugEnemyOverride", enemy); _controller.RefreshEncounterPreview();
                Buy("thorn_armor", 3, 0); Buy("vampiric_charm", 5, 0); yield return null;
                _controller.TryStartBattle();
                _controller.ActiveCombat.PlayerWeapons.Single().Stats.AccuracyPercent = 100;
                _controller.ActiveCombat.ApplyNonAttackDamage(CombatSide.Enemy, CombatSide.Player, 1, CombatDamageKind.Other);
                _controller.TickCombat(4.1f); Win();
                Click("Battle Statistics Button"); yield return null;
                Assert.That(ValueFor("가시 갑옷"), Is.EqualTo("6"));
                Click("Defense Statistics Tab"); yield return null;
                Assert.That(ValueFor("흡혈의 부적"), Is.EqualTo("6"));
                Assert.That(ValueFor("나무 방패"), Is.EqualTo("5"));
                Assert.That(ValueFor("훈련용 검"), Is.EqualTo("0"));
                Assert.That(Text("Statistics Totals"), Does.Contain("방어 11").And.Not.Contain("회복"));
                Capture("ThornsLifestealAndArmorStatistics.png");
            }
            finally { Object.Destroy(enemy); }
        }
        [UnityTest]
        public IEnumerator StackedAttackBars_MatchTheSketch_SortNormalizeColorAndShowHoverBreakdown()
        {
            yield return Setup();
            var a = FixtureWeapon("A", "아이템 A", 70); var b = FixtureWeapon("B", "아이템 B", 50);
            var c = Object.Instantiate(Item("healing_potion")); _temporary.Add(c);
            Set(c, "_itemId", "C"); Set(c, "_displayName", "아이템 C");
            Set(c, "_battleStartStatuses", new List<StatusApplication> { new(StatusEffectKind.Thorns, 50) });
            var enemy = FixtureWeapon("E", "적 무기", 1, 1.5f);
            _controller.TryStartBattle();
            _controller.BattleController.BeginBattle(FixtureUnit("플레이어", 100, a, b, c), FixtureUnit("적", 219.999995, enemy), 1, 5);
            _controller.TickCombat(1);
            foreach (var item in _controller.ActiveCombat.PlayerItems) item.Cycle.SetBaseInterval(100);
            _controller.ActiveCombat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Burn, 25, CombatSide.Player, "A");
            _controller.ActiveCombat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Poison, 20, CombatSide.Player, "B");
            _controller.TickCombat(1); Click("Battle Statistics Button"); yield return null; Canvas.ForceUpdateCanvases();
            Assert.That(Rows().Select(r => r.GetComponentsInChildren<Text>().Single(t => t.name == "Statistics Item Name").text),
                Is.EqualTo(new[] { "아이템 A", "아이템 B", "아이템 C" }));
            Assert.That(ValueFor("아이템 A"), Is.EqualTo("100")); Assert.That(ValueFor("아이템 B"), Is.EqualTo("70")); Assert.That(ValueFor("아이템 C"), Is.EqualTo("50"));
            float width = BarFor("아이템 A").GetComponent<RectTransform>().rect.width;
            Assert.That(width, Is.EqualTo(480).Within(.001));
            Assert.That(BarFor("아이템 B").GetComponent<RectTransform>().rect.width, Is.EqualTo(width * .7).Within(.001));
            Assert.That(BarFor("아이템 C").GetComponent<RectTransform>().rect.width, Is.EqualTo(width * .5).Within(.001));
            Assert.That(SegmentFor("아이템 A", "Direct").color, Is.EqualTo(Color.white));
            Assert.That(SegmentFor("아이템 A", "Burn").color, Is.EqualTo(PrototypeStatusGlyph.ColorFor(StatusEffectKind.Burn)));
            Assert.That(SegmentFor("아이템 B", "Poison").color, Is.EqualTo(PrototypeStatusGlyph.ColorFor(StatusEffectKind.Poison)));
            Assert.That(SegmentFor("아이템 C", "Thorns").color, Is.EqualTo(PrototypeStatusGlyph.ColorFor(StatusEffectKind.Thorns)));
            AssertBarGeometry("아이템 A"); AssertBarGeometry("아이템 B"); AssertBarGeometry("아이템 C");
            Assert.That(_controller.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Statistics Explanation"), Is.False);
            Assert.That(Rows().SelectMany(r => r.GetComponentsInChildren<Text>()).Any(t => t.text.Contains("위치")), Is.False);
            Capture("StackedAttackStatistics.png");
            var pointer = Pointer(BarFor("아이템 A").transform);
            BarFor("아이템 A").OnPointerEnter(pointer);
            Assert.That(Find("Statistics Breakdown Tooltip").gameObject.activeSelf, Is.True);
            Assert.That(Text("Statistics Breakdown Body"), Does.Contain("직접 공격 피해  70").And.Contain("화상 피해  30").And.Contain("합계  100"));
            AssertTooltipTextFits();
            Capture("StackedAttackTooltip.png");
            pointer.position = new Vector2(Screen.width - 1, 1); BarFor("아이템 A").OnPointerMove(pointer); Canvas.ForceUpdateCanvases();
            var tooltipCorners = new Vector3[4]; var screenCorners = new Vector3[4];
            Find("Statistics Breakdown Tooltip").GetComponent<RectTransform>().GetWorldCorners(tooltipCorners);
            Find("Combat Statistics Screen").GetComponent<RectTransform>().GetWorldCorners(screenCorners);
            Assert.That(tooltipCorners[0].x, Is.GreaterThanOrEqualTo(screenCorners[0].x));
            Assert.That(tooltipCorners[0].y, Is.GreaterThanOrEqualTo(screenCorners[0].y));
            Assert.That(tooltipCorners[2].x, Is.LessThanOrEqualTo(screenCorners[2].x));
            Click("Defense Statistics Tab"); yield return null;
            Assert.That(Find("Statistics Breakdown Tooltip").gameObject.activeSelf, Is.False);
            Click("Enemy Statistics Tab"); Assert.That(Find("Defense Statistics Tab").GetComponent<Button>().interactable, Is.False);
            Click("Player Statistics Tab"); Click("Attack Statistics Tab"); yield return null; Canvas.ForceUpdateCanvases();
            Assert.That(_controller.GetComponentsInChildren<PrototypeStatisticsTabGlyph>().All(g => g.color == Color.white && g.canvasRenderer.GetMesh().vertexCount > 0), Is.True);
            Assert.That(Text("Statistics Totals"), Does.Contain("공격 220").And.Contain("방어 0").And.Not.Contain("회복"));
        }
        [UnityTest]
        public IEnumerator DefenseBar_CombinesGreenHealingBlueArmorAndWhiteReduction_WithOneTotal()
        {
            yield return Setup(); _controller.TryStartBattle();
            var simulation = _controller.ActiveCombat;
            simulation.PlayerWeapons.Single().Stats.AccuracyPercent = 100;
            simulation.ApplyStatus(CombatSide.Player, StatusEffectKind.Lifesteal, 3, CombatSide.Player, "starter_wooden_shield");
            simulation.ApplyNonAttackDamage(CombatSide.Enemy, CombatSide.Player, 5, CombatDamageKind.Other);
            _controller.TickCombat(4.1f);
            // Simulate a future resolver reporting damage already prevented; no new reduction item is shipped.
            simulation.RecordDamageReduction(CombatSide.Player, "starter_wooden_shield", 4);
            Win(); Click("Battle Statistics Button"); Click("Defense Statistics Tab"); yield return null; Canvas.ForceUpdateCanvases();
            Assert.That(Rows().First().GetComponentsInChildren<Text>().Single(t => t.name == "Statistics Item Name").text, Is.EqualTo("나무 방패"));
            Assert.That(ValueFor("나무 방패"), Is.EqualTo("18"));
            Assert.That(SegmentFor("나무 방패", "Healing").color, Is.EqualTo(new Color(.6f, .92f, .34f)));
            Assert.That(SegmentFor("나무 방패", "Armor").color, Is.EqualTo(new Color(.4f, .8f, 1)));
            Assert.That(SegmentFor("나무 방패", "Reduction").color, Is.EqualTo(Color.white));
            Capture("StackedDefenseStatistics.png");
            var hover = BarFor("나무 방패"); hover.OnPointerEnter(Pointer(hover.transform));
            Assert.That(Text("Statistics Breakdown Body"), Does.Contain("체력 회복  9").And.Contain("방어도 흡수  5").And.Contain("피해량 감소  4").And.Contain("합계  18"));
            AssertBarGeometry("나무 방패"); AssertTooltipTextFits();
            Capture("StackedDefenseTooltip.png");
            hover.OnPointerExit(Pointer(hover.transform)); Assert.That(Find("Statistics Breakdown Tooltip").gameObject.activeSelf, Is.False);
            Assert.That(Text("Statistics Totals"), Does.Contain("방어 18").And.Not.Contain("회복"));
        }
        private void AssertTooltipTextFits()
        {
            Canvas.ForceUpdateCanvases(); var body = Find("Statistics Breakdown Body").GetComponent<Text>();
            Assert.That(body.preferredHeight, Is.LessThanOrEqualTo(body.rectTransform.rect.height), "The final total line must not be clipped.");
        }
        private void AssertBarGeometry(string name)
        {
            Canvas.ForceUpdateCanvases(); var bar = BarFor(name).GetComponent<RectTransform>();
            var title = bar.parent.GetComponentsInChildren<Text>().Single(t => t.name == "Statistics Item Name").rectTransform;
            Vector3[] Corners(RectTransform rect) { var result = new Vector3[4]; rect.GetWorldCorners(result); return result; }
            var bounds = Corners(bar); Assert.That(bounds[0].x, Is.GreaterThanOrEqualTo(Corners(title)[2].x), "Bars cannot cover item names.");
            var segments = bar.GetComponentsInChildren<Image>().Where(i => i.name.StartsWith("Statistics Segment ")).ToArray();
            float previous = bounds[0].x;
            foreach (var segment in segments)
            {
                var edges = Corners(segment.rectTransform);
                Assert.That(edges[0].x, Is.EqualTo(previous).Within(.1), "Colored segments must meet without gaps or overlaps.");
                previous = edges[2].x;
            }
            Assert.That(previous, Is.EqualTo(bounds[2].x).Within(.1));
        }
        private PrototypeStatisticsBarHover BarFor(string name) => Rows().Single(r => r.GetComponentsInChildren<Text>()
            .Any(t => t.name == "Statistics Item Name" && t.text == name)).GetComponentInChildren<PrototypeStatisticsBarHover>();
        private Image SegmentFor(string name, string metric) => BarFor(name).GetComponentsInChildren<Image>().Single(i => i.name == "Statistics Segment " + metric);
        private static PointerEventData Pointer(Transform target) => new(EventSystem.current)
        { position = RectTransformUtility.WorldToScreenPoint(null, target.GetComponent<RectTransform>().TransformPoint(target.GetComponent<RectTransform>().rect.center)) };
        private ItemData FixtureWeapon(string id, string name, float damage, float interval = 1)
        {
            var item = Object.Instantiate(Item("training_blade")); _temporary.Add(item);
            Set(item, "_itemId", id); Set(item, "_displayName", name); Set(item, "_attackScaling", new StatScaling());
            Set(item, "_weaponStats", new WeaponStats(damage, interval, 100, 0, 200));
            Set(item, "_activation", new ItemActivationData(interval, new ItemActivationEffect(ItemEffectKind.Attack))); return item;
        }
        private static CombatantSnapshot FixtureUnit(string name, double health, params ItemData[] items) => new(name, health,
            new CombatLoadoutSnapshot(items.Select((item, i) => new CombatItemSnapshot(item.ItemId, item, new Vector2Int(i, 0), ItemRotation.None, item.OccupiedCells))));
        private string ValueFor(string name) => Rows().Single(r => r.GetComponentsInChildren<Text>()
            .Any(t => t.name == "Statistics Item Name" && t.text == name)).GetComponentsInChildren<Text>().Single(t => t.name == "Statistics Value").text;
        private void Win()
        {
            var weapon = _controller.ActiveCombat.PlayerWeapons.First(w => w.Snapshot.ItemId == "training_blade");
            weapon.Stats.AccuracyPercent = 100; weapon.Stats.AttackDamage = 1000; _controller.TickCombat(2);
        }
        private void Buy(string id, int x, int y)
            => BuyData(Item(id), x, y);
        private void BuyData(ItemData data, int x, int y)
        {
            var slot = _controller.GetComponentsInChildren<PrototypeShopSlotView>().First(); slot.Bind(new ShopOffer(data, false));
            var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, slot.transform.position) };
            slot.OnBeginDrag(pointer); pointer.position = RectTransformUtility.WorldToScreenPoint(null, Find($"Grid Cell {x},{y}").position);
            slot.OnDrag(pointer); slot.OnEndDrag(pointer);
        }
        private Transform Find(string name) => _controller.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);
        private string Text(string name) => Find(name).GetComponent<Text>().text;
        private void Click(string name) => Find(name).GetComponent<Button>().onClick.Invoke();
        private Transform[] Rows() => Find("Statistics Rows").Cast<Transform>().Where(t => t.gameObject.activeSelf && t.name.StartsWith("Statistics Row ")).ToArray();
        private ItemData Item(string id) => _controller.ItemCatalog.Single(i => i.ItemId == id);
        private void Capture(string name) => PrototypeScreenCapture.CaptureIfRequested(_controller.GetComponentInChildren<Canvas>(), name);
        private static void Set(object obj, string field, object value) => obj.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj, value);
    }
}

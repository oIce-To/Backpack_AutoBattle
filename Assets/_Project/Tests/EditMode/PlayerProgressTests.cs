using System;
using System.IO;
using BackpackAutoBattle.Editor;
using BackpackAutoBattle.Progression;
using NUnit.Framework;

namespace BackpackAutoBattle.Tests.Progression
{
    public sealed class PlayerProgressTests
    {
        private string _directory;
        private string _path;
        [SetUp] public void Setup()
        {
            _directory = Path.Combine(Path.GetTempPath(), "BackpackProgressTest_" + Guid.NewGuid().ToString("N"));
            _path = Path.Combine(_directory, "profile.json");
        }
        [TearDown] public void Cleanup()
        {
            foreach (string suffix in new[] { "", ".bak", ".tmp" }) if (File.Exists(_path + suffix)) File.Delete(_path + suffix);
            if (Directory.Exists(_directory)) Directory.Delete(_directory); // Empty, uniquely owned directory; no recursive deletion.
        }

        [Test]
        public void EditorReset_ArchivesProfileAndRecoveryFiles_AndKeepsSettings()
        {
            var store = new PlayerProgressStore(_path);
            var progress = new PlayerProgress();
            progress.TryAddExperience(125);
            Assert.That(store.TrySave(progress, out _), Is.True);
            string original = File.ReadAllText(_path);
            File.Copy(_path, _path + ".bak");
            File.WriteAllText(_path + ".tmp", "temporary save");
            string settings = Path.Combine(_directory, "settings.json");
            File.WriteAllText(settings, "settings");

            string backup = null;
            try
            {
                backup = PlayerProgressResetTool.ArchiveProgress(store);
                string archived = Path.Combine(backup, Path.GetFileName(_path));
                Assert.That(File.ReadAllText(archived), Is.EqualTo(original));
                Assert.That(File.ReadAllText(archived + ".bak"), Is.EqualTo(original));
                Assert.That(File.ReadAllText(archived + ".tmp"), Is.EqualTo("temporary save"));
                Assert.That(File.Exists(_path), Is.False);
                Assert.That(File.Exists(_path + ".bak"), Is.False);
                Assert.That(File.Exists(_path + ".tmp"), Is.False);
                Assert.That(File.ReadAllText(settings), Is.EqualTo("settings"));
                Assert.That(store.Load(out var warning).SharedExperience, Is.Zero);
                Assert.That(warning, Is.Null);
                Assert.That(PlayerProgressResetTool.ArchiveProgress(store), Is.Null);
            }
            finally
            {
                File.Delete(settings);
                if (backup != null)
                {
                    foreach (string file in Directory.GetFiles(backup)) File.Delete(file);
                    Directory.Delete(backup);
                }
            }
        }

        [Test]
        public void EditorTicketRefill_KeepsCurrencyAndStageRecords_AndResetsRechargeTime()
        {
            var progress = new PlayerProgress();
            var now = DateTimeOffset.UtcNow;
            Assert.That(progress.TryClaimReward("mine", new StageReward(25, 500, new[] { "knight_sword" }), true, now), Is.True);
            progress.RecordRound("origin_mine_01", 12, 12, true);
            var store = new PlayerProgressStore(_path);
            Assert.That(store.TrySave(progress, out _), Is.True);
            Assert.That(MineTicketRefillTool.RefillSavedTickets(store, out _), Is.True);
            var loaded = store.Load(out _);
            Assert.That(loaded.MineTickets, Is.EqualTo(5));
            Assert.That(loaded.UntilNextMineTicket(now.AddDays(1)), Is.EqualTo(TimeSpan.Zero));
            Assert.That(loaded.SharedExperience, Is.EqualTo(25));
            Assert.That(loaded.OriginShards, Is.EqualTo(500));
            Assert.That(loaded.GetBlueprints("knight_sword"), Is.EqualTo(1));
            Assert.That(loaded.GetStage("origin_mine_01").Cleared, Is.True);
        }

        [Test]
        public void StageUnlock_RequiresPreviousClear_NotJustVisitOrRoundTwelve_AndSurvivesSave()
        {
            string[] order = { "forest", "cave", "tower" };
            var progress = new PlayerProgress();
            Assert.That(progress.IsStageUnlocked("forest", order), Is.True);
            Assert.That(progress.IsStageUnlocked("cave", order), Is.False);
            Assert.That(progress.IsStageUnlocked("missing", order), Is.False);
            Assert.That(progress.IsStageUnlocked(null, order), Is.False);
            progress.RecordRound("forest", 12, 12);
            progress.RecordRound("cave", 3, 12);
            Assert.That(progress.IsStageUnlocked("cave", order), Is.False);
            Assert.That(progress.IsStageUnlocked("tower", order), Is.False);
            progress.RecordRound("forest", 12, 12, true);
            Assert.That(progress.IsStageUnlocked("cave", order), Is.True);
            Assert.That(progress.IsStageUnlocked("tower", order), Is.False);
            progress.RecordRound("cave", 12, 12, true);
            Assert.That(new PlayerProgressStore(_path).TrySave(progress, out _), Is.True);
            var loaded = new PlayerProgressStore(_path).Load(out _);
            Assert.That(loaded.IsStageUnlocked("tower", order), Is.True);
            Assert.That(loaded.IsStageUnlocked("forest", order), Is.True);
        }

        [Test]
        public void ProgressIsMonotonicAndIndependentByStage_AndExportIsDetached()
        {
            var progress = new PlayerProgress();
            Assert.That(progress.GetStage("stage_01").HighestRound, Is.Zero);
            progress.RecordRound("stage_01", 7, 12); progress.RecordRound("stage_05", 2, 12);
            Assert.That(progress.RecordRound("stage_01", 3, 12), Is.False);
            progress.RecordRound("stage_01", 12, 12, true); progress.RecordRound("stage_01", 1, 12);
            Assert.That(progress.GetStage("stage_01").Cleared, Is.True);
            Assert.That(progress.GetStage("stage_01").HighestRound, Is.EqualTo(12));
            Assert.That(progress.GetStage("stage_05").HighestRound, Is.EqualTo(2));
            Assert.That(progress.GetStage("stage_05").Cleared, Is.False);
            var data = progress.Export(); data.stages.Clear();
            Assert.That(progress.GetStage("stage_01").Cleared, Is.True);
        }

        [Test]
        public void SaveAndReload_PreservesSelectionsAndPerStageProgress_WithoutMutatingSnapshot()
        {
            var store = new PlayerProgressStore(_path); var progress = store.Load(out string notice);
            Assert.That(notice, Is.Null); Assert.That(File.Exists(_path), Is.False);
            progress.SelectCharacter("serena"); progress.SelectStage("stage_05");
            progress.RecordRound("stage_01", 12, 12, true); progress.RecordRound("stage_05", 4, 12);
            Assert.That(store.TrySave(progress, out string error), Is.True, error);
            var loaded = new PlayerProgressStore(_path).Load(out notice);
            Assert.That(notice, Is.Null); Assert.That(loaded.SelectedCharacterId, Is.EqualTo("serena"));
            Assert.That(loaded.SelectedStageId, Is.EqualTo("stage_05"));
            Assert.That(loaded.GetStage("stage_01").Cleared, Is.True);
            Assert.That(loaded.GetStage("stage_05").HighestRound, Is.EqualTo(4));
            loaded.RecordRound("stage_05", 8, 12);
            Assert.That(progress.GetStage("stage_05").HighestRound, Is.EqualTo(4));
            Assert.That(File.ReadAllText(_path), Does.Contain($"\"version\": {PlayerProgress.SaveVersion}"));
        }

        [Test]
        public void CorruptedPrimary_RecoversBackup_AndNextSaveKeepsLastGoodBackup()
        {
            var store = new PlayerProgressStore(_path); var progress = new PlayerProgress();
            progress.RecordRound("stage_01", 2, 12); Assert.That(store.TrySave(progress, out _), Is.True);
            progress.RecordRound("stage_01", 3, 12); Assert.That(store.TrySave(progress, out _), Is.True);
            string backup = File.ReadAllText(_path + ".bak"); File.WriteAllText(_path, "{invalid json");
            var loaded = store.Load(out string warning);
            Assert.That(warning, Is.Not.Empty); Assert.That(loaded.GetStage("stage_01").HighestRound, Is.EqualTo(2));
            loaded.RecordRound("stage_01", 4, 12); Assert.That(store.TrySave(loaded, out _), Is.True);
            Assert.That(File.ReadAllText(_path + ".bak"), Is.EqualTo(backup));
            Assert.That(store.Load(out _).GetStage("stage_01").HighestRound, Is.EqualTo(4));
        }

        [TestCase("{invalid json")]
        [TestCase("{}")]
        [TestCase("{\"version\":5,\"futureField\":123}")]
        public void UnreadableOrNewerSave_IsPreservedAndNeverSilentlyOverwritten(string json)
        {
            Directory.CreateDirectory(_directory); File.WriteAllText(_path, json);
            var store = new PlayerProgressStore(_path); var loaded = store.Load(out string warning);
            Assert.That(warning, Is.Not.Empty); Assert.That(store.CanWrite, Is.False);
            loaded.SelectCharacter("allen"); Assert.That(store.TrySave(loaded, out string error), Is.False);
            Assert.That(error, Is.Not.Empty); Assert.That(File.ReadAllText(_path), Is.EqualTo(json));
        }

        [TestCase(0, false)]
        [TestCase(13, false)]
        [TestCase(5, true)]
        public void InvalidProgressIsRejected(int round, bool clear)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerProgress().RecordRound("stage_01", round, 12, clear));

        [Test]
        public void InvalidSavedRecordsAreRejected_WhileDuplicateRecordsMergeWithoutLosingClear()
        {
            var data = new ProfileSaveData { version = 1, stages = new()
            {
                new() { stageId = "stage_01", highestRound = 12, cleared = true },
                new() { stageId = "stage_01", highestRound = 2 }
            } };
            Assert.That(PlayerProgress.FromData(data).GetStage("stage_01").Cleared, Is.True);
            data.stages[1].highestRound = -1;
            Assert.Throws<FormatException>(() => PlayerProgress.FromData(data));
            Assert.Throws<ArgumentException>(() => new PlayerProgress().SelectCharacter(""));
            Assert.Throws<ArgumentException>(() => new PlayerProgress().SelectStage(" "));
        }

        [Test]
        public void VersionOneMigratesWithoutLosingSelectionsOrStageRecords_AndVersionTwoPersistsSharedExperienceAndSeparateLevels()
        {
            const string legacy = "{\"version\":1,\"selectedCharacterId\":\"serena\",\"selectedStageId\":\"stage_02\",\"stages\":[{\"stageId\":\"stage_01\",\"highestRound\":12,\"cleared\":true}]}";
            Directory.CreateDirectory(_directory); File.WriteAllText(_path, legacy);
            var store = new PlayerProgressStore(_path); var progress = store.Load(out string warning);
            Assert.That(warning, Is.Null); Assert.That(progress.SharedExperience, Is.Zero);
            Assert.That(progress.GetCharacterLevel("serena"), Is.EqualTo(1));
            Assert.That(progress.SelectedCharacterId, Is.EqualTo("serena")); Assert.That(progress.SelectedStageId, Is.EqualTo("stage_02"));
            Assert.That(progress.GetStage("stage_01").Cleared, Is.True);
            var table = UnityEngine.ScriptableObject.CreateInstance<ExperienceTableData>();
            try
            {
                progress.TryAddExperience(400); progress.TryLevelUp("allen", table); progress.TryLevelUp("allen", table);
                Assert.That(store.TrySave(progress, out _), Is.True);
                var loaded = new PlayerProgressStore(_path).Load(out warning);
                Assert.That(warning, Is.Null); Assert.That(loaded.SharedExperience, Is.EqualTo(175));
                Assert.That(loaded.GetCharacterLevel("allen"), Is.EqualTo(3)); Assert.That(loaded.GetCharacterLevel("serena"), Is.EqualTo(1));
                Assert.That(loaded.GetStage("stage_01").Cleared, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(table); }
        }
    }
}

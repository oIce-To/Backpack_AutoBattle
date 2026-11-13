using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public sealed partial class BackpackPrototypeController
    {
        [Header("Game Audio")]
        [SerializeField] private AudioClip _backgroundMusic;
        [SerializeField] private AudioClip _combatHitSound;
        private AudioSource _musicSource, _effectsSource;
        private PrototypeGameSettings _settings;
        private PrototypeSettingsScreen _settingsScreen;
        private Button _stageSettingsButton;
        private string _settingsPath;
        private bool _settingsDirty, _previousRunInBackground;
        private double _displayDeadline;
        private (Vector2Int size, FullScreenMode mode) _display, _previousDisplay;
        private static bool HasNativeDisplay => !Application.isEditor && !Application.isBatchMode;
        public bool IsSettingsOpen => _settingsScreen != null && _settingsScreen.IsOpen;
        internal bool IsDisplayPreviewActive => _displayDeadline > 0;

        private void InitializeSettings(Transform canvas)
        {
            _stageSettingsButton = PrototypeSettingsScreen.GearButton(canvas, "Stage Settings Button", Vector2.zero, () => TryOpenSettings());
            SetRect((RectTransform)_stageSettingsButton.transform, Vector2.one, new Vector2(-33, -36), new Vector2(42, 38));
            _settingsPath = PrototypeGameSettings.SavePath();
            _settings = PrototypeGameSettings.Load(_settingsPath);
            var requested = new Vector2Int(_settings.width, _settings.height);
            var choices = GetDisplaySizes();
            if (!choices.Contains(requested)) requested = DefaultDisplaySize(choices);
            ApplyDisplay(requested, _settings.fullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);

            _musicSource = CreateAudioSource("Background Music");
            _effectsSource = CreateAudioSource("Combat Effects");
            _musicSource.loop = true;
            _musicSource.clip = _backgroundMusic;
            ApplySettings();
            if (_backgroundMusic != null) _musicSource.Play();
            _settingsScreen = new PrototypeSettingsScreen(canvas, this);
        }

        private AudioSource CreateAudioSource(string name)
        {
            var audio = new GameObject(name, typeof(AudioSource));
            audio.transform.SetParent(transform, false);
            var source = audio.GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            return source;
        }

        private void PlayCombatHit()
        {
            if (_combatHitSound != null && _effectsSource.volume > 0)
                _effectsSource.PlayOneShot(_combatHitSound);
        }

        private void ApplySettings()
        {
            _musicSource.volume = _settings.master * _settings.music / 10000f;
            _effectsSource.volume = _settings.master * _settings.effects / 10000f;
            _battleController.ConfigurePlayback(_settings.battleSpeed, _settings.damageNumbers);
        }

        public bool TryOpenSettings()
        {
            if (IsSettingsOpen || _equipment.IsDetailsOpen || _rewardPopup.IsOpen || _mainMenu.IsPopupOpen || _stageSelection.IsEntryOpen
                || IsStageAbandonOpen || IsEnemyPreviewOpen || Phase == PrototypePhase.Fusion || _draggingItem != null) return false;
            _itemTooltip.HideAll();
            _battleController.SetSettingsSuspended(true);
            _characterSelection.Carousel.enabled = false;
            EventSystem.current?.SetSelectedGameObject(null);
            var actual = ReadDisplay();
            _settingsScreen.Show(_settings, GetDisplaySizes(actual.size), actual.size,
                actual.mode != FullScreenMode.Windowed, !IsMainMenu);
            return true;
        }

        public void CloseSettings()
        {
            if (!IsSettingsOpen) return;
            if (IsDisplayPreviewActive) RevertDisplay();
            if (!SaveSettings()) Debug.LogWarning("설정을 저장하지 못했습니다.");
            _settingsScreen.Hide();
            _battleController.SetSettingsSuspended(false);
            _characterSelection.Carousel.enabled = true;
            EventSystem.current?.SetSelectedGameObject(null);
        }

        internal void ChangeVolumes(int master, int music, int effects)
        {
            _settings.master = master;
            _settings.music = music;
            _settings.effects = effects;
            _settingsDirty = true;
            ApplySettings();
        }

        internal void ChangeBattleSettings(float speed, bool damageNumbers)
        {
            _settings.battleSpeed = speed;
            _settings.damageNumbers = damageNumbers;
            _settingsDirty = true;
            ApplySettings();
        }

        internal void PreviewDisplay(Vector2Int size, bool fullScreen)
        {
            if (!IsSettingsOpen || IsDisplayPreviewActive) return;
            var current = ReadDisplay();
            if (!GetDisplaySizes(current.size).Contains(size)) return;
            var mode = fullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            if (current == (size, mode)) return;
            _previousDisplay = current;
            _previousRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            ApplyDisplay(size, mode);
            _displayDeadline = Time.realtimeSinceStartupAsDouble + 15;
            _settingsScreen.ShowDisplayConfirmation(15);
        }

        internal void ConfirmDisplay()
        {
            if (!IsDisplayPreviewActive) return;
            if (Time.realtimeSinceStartupAsDouble >= _displayDeadline)
            {
                RevertDisplay();
                return;
            }
            _settings.width = _display.size.x;
            _settings.height = _display.size.y;
            _settings.fullScreen = _display.mode != FullScreenMode.Windowed;
            _settingsDirty = true;
            FinishDisplayPreview(_display);
            if (!SaveSettings()) _settingsScreen.ShowNotice("설정을 저장하지 못했습니다.");
        }

        internal void RevertDisplay()
        {
            if (!IsDisplayPreviewActive) return;
            ApplyDisplay(_previousDisplay.size, _previousDisplay.mode);
            FinishDisplayPreview(_previousDisplay);
        }

        private void FinishDisplayPreview((Vector2Int size, FullScreenMode mode) result)
        {
            _displayDeadline = 0;
            Application.runInBackground = _previousRunInBackground;
            _settingsScreen.EndDisplayConfirmation(result.size, result.mode != FullScreenMode.Windowed);
        }

        private void UpdateSettings()
        {
            if (!IsSettingsOpen) return;
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                if (IsDisplayPreviewActive) RevertDisplay();
                else CloseSettings();
            }
            if (!IsDisplayPreviewActive) return;
            double remaining = _displayDeadline - Time.realtimeSinceStartupAsDouble;
            if (remaining <= 0) RevertDisplay();
            else _settingsScreen.UpdateCountdown((int)Math.Ceiling(remaining));
        }

        private bool SaveSettings()
        {
            if (!_settingsDirty) return true;
            if (!_settings.TrySave(_settingsPath)) return false;
            _settingsDirty = false;
            return true;
        }

        private void OnDestroy()
        {
            if (IsDisplayPreviewActive)
            {
                ApplyDisplay(_previousDisplay.size, _previousDisplay.mode);
                Application.runInBackground = _previousRunInBackground;
            }
            SaveSettings();
        }

        private (Vector2Int size, FullScreenMode mode) ReadDisplay()
            => HasNativeDisplay ? (new Vector2Int(Screen.width, Screen.height), Screen.fullScreenMode) : _display;

        private void ApplyDisplay(Vector2Int size, FullScreenMode mode)
        {
            _display = (size, mode);
            if (HasNativeDisplay) Screen.SetResolution(size.x, size.y, mode);
        }

        private static List<Vector2Int> GetDisplaySizes(Vector2Int current = default)
        {
            int width = Display.main.systemWidth, height = Display.main.systemHeight;
            var sizes = new HashSet<Vector2Int>();
            foreach (Resolution resolution in Screen.resolutions)
                sizes.Add(new Vector2Int(resolution.width, resolution.height));
            foreach (var common in new[] { new Vector2Int(960, 540), new Vector2Int(1280, 720),
                         new Vector2Int(1600, 900), new Vector2Int(1920, 1080) })
                if (common.x <= width && common.y <= height) sizes.Add(common);
            if (current.x > 0 && current.y > 0) sizes.Add(current);
            if (sizes.Count == 0) sizes.Add(new Vector2Int(1280, 720));
            return sizes.OrderBy(s => (long)s.x * s.y).ThenBy(s => s.x).ToList();
        }

        internal static Vector2Int DefaultDisplaySize(IReadOnlyList<Vector2Int> choices)
            => choices.OrderBy(s => Mathf.Abs(s.x - 1280) + Mathf.Abs(s.y - 720)).First();
    }
}

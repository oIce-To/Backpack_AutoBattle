using System;
using System.Collections.Generic;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Items;
using UnityEngine;

namespace BackpackAutoBattle.Prototype
{
    /// <summary>Runs combat and updates only the dedicated battle screen.</summary>
    public sealed class PrototypeBattleController : MonoBehaviour
    {
        private PrototypeBattleScreen _screen;
        private PrototypeCombatStatisticsView _statisticsView;
        private PrototypeItemTooltip _tooltip;
        private Action<CombatResult> _finished;
        private Action _playHit;
        private float _defaultSpeed = DefaultPlaybackSpeed;
        private bool _settingsSuspended;

        public CombatSimulation Simulation { get; private set; }
        public bool AutoTick { get; set; } = true;
        public static IReadOnlyList<float> PlaybackSpeeds { get; } = Array.AsReadOnly(new[] { .5f, 1f, 2f, 3f });
        public const float DefaultPlaybackSpeed = 1f;
        public static bool SupportsSpeed(float speed) => speed is .5f or 1f or 2f or 3f;
        public float PlaybackSpeed { get; private set; } = DefaultPlaybackSpeed;
        public bool IsPaused { get; private set; }
        public RectTransform ScreenRoot => _screen.Root;

        public void Initialize(Transform canvas, Action<CombatResult> finished, Action continueAction, PrototypeItemTooltip tooltip,
            Action playHit)
        {
            _finished = finished;
            _tooltip = tooltip;
            _playHit = playHit;
            _screen = new PrototypeBattleScreen(canvas, continueAction, tooltip, () => ShowStatistics(),
                speed => SetPlaybackSpeed(speed), () => TogglePause());
            _statisticsView = new PrototypeCombatStatisticsView(canvas);
            SetVisible(false);
        }

        public void BeginBattle(CombatantSnapshot player, CombatantSnapshot enemy, int round, int income, float maximumSeconds = float.PositiveInfinity,
            StatusEffectData[] statusEffects = null, System.Random random = null)
        {
            UnbindSimulation();
            _statisticsView.Hide();
            PlaybackSpeed = _defaultSpeed;
            IsPaused = false;
            Simulation = new CombatSimulation(player, enemy, random, maximumDuration: maximumSeconds, statusEffects: statusEffects);
            Simulation.ItemActivated += ShowAttack;
            Simulation.StatusTicked += _screen.ShowStatusDamage;
            Simulation.ReactionTriggered += _screen.ShowReaction;
            _screen.Begin(Simulation, round, income);
            RefreshPlaybackControls();
        }

        private void Update()
        {
            if (AutoTick) TickCombat(Time.unscaledDeltaTime);
        }

        public void TickCombat(float deltaSeconds)
        {
            if (Simulation == null || Simulation.Result != CombatResult.Running || _settingsSuspended) return;
            if (deltaSeconds < 0 || float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds))
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            // Only the simulation clock is scaled. UI, tooltips and feedback use real time.
            if (!IsPaused) Simulation.Advance((double)deltaSeconds * PlaybackSpeed);
            _screen.Render(Simulation);
            RefreshPlaybackControls();
            if (Simulation.Result != CombatResult.Running) _finished(Simulation.Result);
        }

        public bool SetPlaybackSpeed(float speed)
        {
            if (Simulation == null || Simulation.Result != CombatResult.Running || _settingsSuspended || !SupportsSpeed(speed)) return false;
            PlaybackSpeed = speed;
            RefreshPlaybackControls();
            return true;
        }

        public bool TogglePause()
        {
            if (Simulation == null || Simulation.Result != CombatResult.Running || _settingsSuspended) return false;
            IsPaused = !IsPaused;
            RefreshPlaybackControls();
            return true;
        }

        /// <summary>Restores the default playback state when starting a new run.</summary>
        public void ResetPlayback()
        {
            PlaybackSpeed = _defaultSpeed; IsPaused = false;
            RefreshPlaybackControls();
        }

        private void RefreshPlaybackControls()
            => _screen.SetPlaybackState(Simulation != null && Simulation.Result == CombatResult.Running, PlaybackSpeed, IsPaused || _settingsSuspended);

        internal void ConfigurePlayback(float defaultSpeed, bool damageNumbers)
        {
            _defaultSpeed = defaultSpeed;
            _screen.SetDamageNumbersVisible(damageNumbers);
        }

        internal void SetSettingsSuspended(bool suspended)
        {
            _settingsSuspended = suspended;
            RefreshPlaybackControls();
        }

        private void ShowAttack(CombatItemEvent attack)
        {
            _screen.ShowAttack(attack);
            if (attack.Kind == ItemEffectKind.Attack && attack.Hit) _playHit?.Invoke();
        }

        public void ShowResult(PrototypePhase phase) => _screen.ShowResult(phase);
        public bool ShowStatistics()
        {
            if (Simulation == null || Simulation.Result == CombatResult.Running) return false;
            _tooltip.HideAll(); _statisticsView.Show(Simulation.CaptureStatistics()); return true;
        }
        public void HideStatistics() => _statisticsView.Hide();
        public void SetLives(int lives, int startingLives) => _screen.SetLives(lives, startingLives);
        public void ShowConfigurationError(string error) => _screen.ShowConfigurationError(error);
        public void SetVisible(bool visible) => _screen.Root.gameObject.SetActive(visible);

        public void EndBattle()
        {
            _statisticsView.Hide();
            UnbindSimulation();
            Simulation = null;
            IsPaused = false;
            _screen.Clear();
            SetVisible(false);
        }

        private void UnbindSimulation()
        {
            if (Simulation == null) return;
            Simulation.ItemActivated -= ShowAttack;
            Simulation.StatusTicked -= _screen.ShowStatusDamage;
            Simulation.ReactionTriggered -= _screen.ShowReaction;
        }
        private void OnDestroy() => UnbindSimulation();
    }
}

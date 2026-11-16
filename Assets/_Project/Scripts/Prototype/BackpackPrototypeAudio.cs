using System;
using System.Collections.Generic;
using UnityEngine;

namespace BackpackAutoBattle.Prototype
{
    public enum PrototypeSound { ButtonClick, ItemPick, ItemPlace, Reroll, Buying, Selling, SimpleHit, RoundClear, RoundFail, StageClear, ItemMerge }

    public sealed partial class BackpackPrototypeController
    {
        private readonly Dictionary<PrototypeSound, AudioClip> _soundClips = new();
        private AudioClip _mainMusic, _shopMusic, _battleMusic, _titleMusic;
        private float _musicFade = 1f;
        private bool _musicPaused;

        // Also provides an observable playback request for integration tests without an audio device.
        public event Action<PrototypeSound> SoundPlayed;
        public bool IsMusicPaused => _musicPaused;

        private void LoadAudioClips()
        {
            _titleMusic = Resources.Load<AudioClip>("Audio/TitleScene_BGM");
            _mainMusic = Resources.Load<AudioClip>("Audio/MainScene_BGM");
            _shopMusic = Resources.Load<AudioClip>("Audio/ShopPhase_BGM");
            _battleMusic = Resources.Load<AudioClip>("Audio/Battle_BGM");
            foreach (PrototypeSound sound in Enum.GetValues(typeof(PrototypeSound)))
                _soundClips[sound] = Resources.Load<AudioClip>("Audio/" + sound);
        }

        internal void PlaySound(PrototypeSound sound)
        {
            if (_effectsSource == null || _effectsSource.volume <= 0 || !_soundClips.TryGetValue(sound, out var clip) || clip == null) return;
            _effectsSource.PlayOneShot(clip, sound == PrototypeSound.ButtonClick ? 1.3f : 1f);
            SoundPlayed?.Invoke(sound);
        }

        private void RefreshMusic()
        {
            if (_musicSource == null) return;
            AudioClip next = IsTitleScreen ? _titleMusic : IsMainMenu ? _mainMusic
                : Phase is PrototypePhase.Preparation or PrototypePhase.Fusion ? _shopMusic : _battleMusic;
            bool pause = !IsMainMenu && Phase == PrototypePhase.Battle && _battleController.IsPlaybackSuspended;
            if (_musicSource.clip != next)
            {
                _musicSource.Stop();
                _musicSource.clip = next;
                _musicPaused = false;
                if (next != null) _musicSource.Play();
            }
            if (pause == _musicPaused) return;
            _musicPaused = pause;
            if (pause) _musicSource.Pause();
            else _musicSource.UnPause();
        }
    }
}

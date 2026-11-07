using System.Collections.Generic;
using UnityEngine;

namespace ITR
{
    public enum Sfx { Click, Coin, Crash, Scrape, Horn, Nitro, NearMiss, Reward, Beep, Go, Whoosh }

    /// <summary>Persistent audio service: music + pooled one-shot SFX, volumes from Settings.</summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }

        AudioSource music;
        readonly List<AudioSource> sfxSources = new List<AudioSource>();
        int nextSfx;
        readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        AudioClip menuMusic, raceMusic;
        public AudioClip EngineClip { get; private set; }

        public static void Ensure()
        {
            if (I != null) return;
            var go = new GameObject("AudioManager");
            DontDestroyOnLoad(go);
            I = go.AddComponent<AudioManager>();
            I.Init();
        }

        void Init()
        {
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            music.spatialBlend = 0f;
            for (int i = 0; i < 10; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                sfxSources.Add(s);
            }

            clips[Sfx.Click] = AudioSynth.Click();
            clips[Sfx.Coin] = AudioSynth.Coin();
            clips[Sfx.Crash] = AudioSynth.Crash();
            clips[Sfx.Scrape] = AudioSynth.Scrape();
            clips[Sfx.Horn] = AudioSynth.Horn();
            clips[Sfx.Nitro] = AudioSynth.Whoosh(1.1f, 0.05f, 0.6f, "Nitro");
            clips[Sfx.NearMiss] = AudioSynth.Whoosh(0.45f, 0.5f, 0.08f, "NearMiss");
            clips[Sfx.Whoosh] = AudioSynth.Whoosh(0.3f, 0.3f, 0.6f, "Whoosh");
            clips[Sfx.Reward] = AudioSynth.Reward();
            clips[Sfx.Beep] = AudioSynth.Countdown(false);
            clips[Sfx.Go] = AudioSynth.Countdown(true);
            EngineClip = AudioSynth.Engine();
            menuMusic = AudioSynth.Music(100f, 11, false);
            raceMusic = AudioSynth.Music(126f, 23, true);
            ApplyVolumes();
        }

        public float SfxVolume => SaveSystem.Data.settings.sfxVolume;

        public void ApplyVolumes()
        {
            var s = SaveSystem.Data.settings;
            music.volume = s.musicVolume * 0.55f;
        }

        public void PlayMusic(bool race)
        {
            var clip = race ? raceMusic : menuMusic;
            if (music.clip == clip && music.isPlaying) return;
            music.clip = clip;
            music.Play();
        }

        public void PauseMusic(bool paused)
        {
            if (paused) music.Pause(); else music.UnPause();
        }

        public void Play(Sfx id, float volume = 1f, float pitch = 1f)
        {
            if (!clips.TryGetValue(id, out var clip)) return;
            float v = volume * SfxVolume;
            if (v <= 0.001f) return;
            var src = sfxSources[nextSfx];
            nextSfx = (nextSfx + 1) % sfxSources.Count;
            src.pitch = pitch;
            src.volume = v;
            src.clip = clip;
            src.Play();
        }

        public static void Click() { if (I != null) I.Play(Sfx.Click, 0.7f, Random.Range(0.95f, 1.05f)); }
    }
}

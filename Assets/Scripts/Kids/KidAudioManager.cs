using System;
using UnityEngine;

namespace DigitDuelist
{
    /// <summary>
    /// Whimsical, child-friendly audio manager.
    /// Provides built-in procedural synthesis so sound works immediately out-of-the-box
    /// without requiring external sound files, while also accepting custom AudioClips in the Inspector.
    /// </summary>
    public class KidAudioManager : MonoBehaviour
    {
        public static KidAudioManager Instance { get; private set; }

        [Header("Audio Source")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;

        [Header("Custom Audio Clips (Optional Overrides)")]
        [SerializeField] private AudioClip customCorrectChime;
        [SerializeField] private AudioClip customWrongBoing;
        [SerializeField] private AudioClip customStreakFanfare;
        [SerializeField] private AudioClip customBubblePop;
        [SerializeField] private AudioClip customFreezeSound;
        [SerializeField] private AudioClip customButtonClick;

        [Header("Volume Settings (0.0 to 1.0)")]
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.85f;
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.65f;

        // Cached procedural clips
        private AudioClip proceduralChime;
        private AudioClip proceduralBoing;
        private AudioClip proceduralFanfare;
        private AudioClip proceduralBubblePop;
        private AudioClip proceduralFreeze;
        private AudioClip proceduralClick;

        private const int SampleRate = 44100;

        public float SfxVolume
        {
            get => sfxVolume;
            set
            {
                sfxVolume = Mathf.Clamp01(value);
                if (sfxSource != null) sfxSource.volume = sfxVolume;
                PlayerPrefs.SetFloat("KidAudio_SFXVolume", sfxVolume);
            }
        }

        public float MusicVolume
        {
            get => musicVolume;
            set
            {
                musicVolume = Mathf.Clamp01(value);
                if (musicSource != null) musicSource.volume = musicVolume;
                PlayerPrefs.SetFloat("KidAudio_MusicVolume", musicVolume);
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            EnsureAudioSources();
            LoadVolumePreferences();
            GenerateProceduralClips();
        }

        private void EnsureAudioSources()
        {
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = true;
            }

            sfxSource.volume = sfxVolume;
            musicSource.volume = musicVolume;
        }

        private void LoadVolumePreferences()
        {
            sfxVolume = PlayerPrefs.GetFloat("KidAudio_SFXVolume", 0.85f);
            musicVolume = PlayerPrefs.GetFloat("KidAudio_MusicVolume", 0.65f);
        }

        #region Public Playback API

        public void PlayCorrectAnswer()
        {
            PlayClip(customCorrectChime != null ? customCorrectChime : proceduralChime);
        }

        public void PlayWrongAnswer()
        {
            PlayClip(customWrongBoing != null ? customWrongBoing : proceduralBoing);
        }

        public void PlayStreakFanfare()
        {
            PlayClip(customStreakFanfare != null ? customStreakFanfare : proceduralFanfare);
        }

        public void PlayBubbleShield()
        {
            PlayClip(customBubblePop != null ? customBubblePop : proceduralBubblePop);
        }

        public void PlayFreezeSpell()
        {
            PlayClip(customFreezeSound != null ? customFreezeSound : proceduralFreeze);
        }

        public void PlayButtonClick()
        {
            PlayClip(customButtonClick != null ? customButtonClick : proceduralClick);
        }

        private void PlayClip(AudioClip clip)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, sfxVolume);
            }
        }

        #endregion

        #region Procedural Audio Synthesis

        private void GenerateProceduralClips()
        {
            proceduralChime = CreateArpeggioChime("KidChime", new float[] { 523.25f, 659.25f, 783.99f, 1046.50f }, 0.45f); // C5, E5, G5, C6
            proceduralBoing = CreateCartoonBoing("KidBoing", 340f, 120f, 0.45f);
            proceduralFanfare = CreateTriumphFanfare("KidFanfare", 0.65f);
            proceduralBubblePop = CreateBubblePop("KidBubble", 0.22f);
            proceduralFreeze = CreateSparkleFreeze("KidFreeze", 0.5f);
            proceduralClick = CreateSoftClick("KidClick", 0.08f);
        }

        private AudioClip CreateArpeggioChime(string name, float[] freqs, float duration)
        {
            int totalSamples = Mathf.CeilToInt(SampleRate * duration);
            float[] samples = new float[totalSamples];
            float noteDuration = duration / freqs.Length;
            int noteSamples = Mathf.CeilToInt(SampleRate * noteDuration);

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                int noteIndex = Mathf.Clamp(i / noteSamples, 0, freqs.Length - 1);
                float freq = freqs[noteIndex];

                // Sine wave with bell envelope decay
                float noteT = (i % noteSamples) / (float)noteSamples;
                float envelope = Mathf.Exp(-noteT * 3.5f);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                // Soft overtone
                float overtone = 0.3f * Mathf.Sin(2f * Mathf.PI * freq * 2f * t);

                samples[i] = (wave + overtone) * envelope * 0.7f;
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateCartoonBoing(string name, float startFreq, float endFreq, float duration)
        {
            int totalSamples = Mathf.CeilToInt(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float norm = (float)i / totalSamples;

                // Pitch bends down with spring bounce vibrato
                float currentFreq = Mathf.Lerp(startFreq, endFreq, norm * norm);
                float vibrato = Mathf.Sin(2f * Mathf.PI * 18f * t) * (1f - norm) * 25f;
                float freqWithVibrato = Mathf.Max(50f, currentFreq + vibrato);

                float wave = Mathf.Sin(2f * Mathf.PI * freqWithVibrato * t);
                float envelope = Mathf.Sin(norm * Mathf.PI * 0.5f) * Mathf.Exp(-norm * 2.8f);

                samples[i] = wave * envelope * 0.65f;
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateTriumphFanfare(string name, float duration)
        {
            int totalSamples = Mathf.CeilToInt(SampleRate * duration);
            float[] samples = new float[totalSamples];
            float[] chordFreqs = new float[] { 392f, 523.25f, 659.25f, 783.99f }; // G4, C5, E5, G5

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float norm = (float)i / totalSamples;
                float wave = 0f;

                foreach (float f in chordFreqs)
                {
                    wave += Mathf.Sin(2f * Mathf.PI * f * t);
                }
                wave /= chordFreqs.Length;

                // Shimmer vibrato
                float tremolo = 0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 12f * t);
                float envelope = Mathf.Sin(Mathf.Clamp01(norm * 4f) * Mathf.PI * 0.5f) * (1f - norm * 0.7f);

                samples[i] = wave * tremolo * envelope * 0.8f;
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateBubblePop(string name, float duration)
        {
            int totalSamples = Mathf.CeilToInt(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float norm = (float)i / totalSamples;

                // Frequency sweeps up then pops
                float freq = Mathf.Lerp(450f, 1300f, norm);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                float envelope = Mathf.Exp(-norm * 8f);

                samples[i] = wave * envelope * 0.75f;
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateSparkleFreeze(string name, float duration)
        {
            int totalSamples = Mathf.CeilToInt(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float norm = (float)i / totalSamples;

                float highChime1 = Mathf.Sin(2f * Mathf.PI * 1760f * t); // A6
                float highChime2 = Mathf.Sin(2f * Mathf.PI * 2637f * t); // E7
                float chime = (highChime1 + highChime2) * 0.5f;

                float envelope = Mathf.Sin(norm * Mathf.PI) * (1f - norm * 0.5f);
                samples[i] = chime * envelope * 0.5f;
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateSoftClick(string name, float duration)
        {
            int totalSamples = Mathf.CeilToInt(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float norm = (float)i / totalSamples;
                float wave = Mathf.Sin(2f * Mathf.PI * 800f * t);
                float envelope = Mathf.Exp(-norm * 25f);
                samples[i] = wave * envelope * 0.5f;
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        #endregion
    }
}

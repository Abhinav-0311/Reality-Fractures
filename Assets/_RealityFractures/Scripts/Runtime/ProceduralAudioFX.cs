using UnityEngine;

namespace RealityFractures
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class ProceduralAudioFX : MonoBehaviour
    {
        private AudioSource audioSource;
        private const int SampleRate = 44100;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D UI/System sound
            audioSource.volume = 0.85f;
        }

        public void PlayRiftOpenSound()
        {
            AudioClip clip = CreateSynthesizedClip("RiftOpen", 1.5f, (time, duration) =>
            {
                float t = time / duration;
                float baseFreq = Mathf.Lerp(150f, 600f, t * t);
                float modFreq = 25f * Mathf.Sin(t * 15f);
                float wave = Mathf.Sin(2f * Mathf.PI * (baseFreq + modFreq) * time);
                float envelope = Mathf.Sin(t * Mathf.PI);
                return wave * envelope * 0.7f;
            });
            PlayClip(clip);
        }

        public void PlayTimeShiftSound(TimeLayer layer)
        {
            float targetFreq = layer switch
            {
                TimeLayer.Past => 220f,     // Deep Amber A3
                TimeLayer.Present => 440f,  // Emerald A4
                _ => 660f                   // Crystalline Future E5
            };

            AudioClip clip = CreateSynthesizedClip("TimeShift", 0.4f, (time, duration) =>
            {
                float t = time / duration;
                float freq = Mathf.Lerp(targetFreq * 0.7f, targetFreq, Mathf.Sqrt(t));
                float wave = Mathf.Sin(2f * Mathf.PI * freq * time);
                float envelope = (1f - t) * Mathf.Sin(t * Mathf.PI);
                return wave * envelope * 0.6f;
            });
            PlayClip(clip);
        }

        public void PlayPuzzleSolveSound()
        {
            AudioClip clip = CreateSynthesizedClip("PuzzleSolve", 0.6f, (time, duration) =>
            {
                float t = time / duration;
                // 3-note rising chime: C5 (523Hz) -> G5 (784Hz) -> C6 (1046Hz)
                float freq = t < 0.33f ? 523.25f : (t < 0.66f ? 783.99f : 1046.50f);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * time);
                float subEnvelope = 1f - (time % 0.2f) / 0.2f;
                return wave * subEnvelope * 0.65f;
            });
            PlayClip(clip);
        }

        public void PlayOrbCollectSound()
        {
            AudioClip clip = CreateSynthesizedClip("OrbCollect", 0.75f, (time, duration) =>
            {
                float t = time / duration;
                // Sparkling major arpeggio
                float freq = t < 0.25f ? 587.33f : (t < 0.5f ? 739.99f : (t < 0.75f ? 880f : 1174.66f));
                float wave = Mathf.Sin(2f * Mathf.PI * freq * time);
                float envelope = (1f - t);
                return wave * envelope * 0.7f;
            });
            PlayClip(clip);
        }

        public void PlayVictoryChordSound()
        {
            AudioClip clip = CreateSynthesizedClip("VictoryChord", 2.2f, (time, duration) =>
            {
                float t = time / duration;
                // Major triad chord: C4 (261), E4 (329), G4 (392), C5 (523)
                float w1 = Mathf.Sin(2f * Mathf.PI * 261.63f * time);
                float w2 = Mathf.Sin(2f * Mathf.PI * 329.63f * time);
                float w3 = Mathf.Sin(2f * Mathf.PI * 392.00f * time);
                float w4 = Mathf.Sin(2f * Mathf.PI * 523.25f * time);
                float chord = (w1 + w2 + w3 + w4) * 0.25f;
                float envelope = Mathf.Sin(Mathf.Pow(t, 0.4f) * Mathf.PI);
                return chord * envelope * 0.8f;
            });
            PlayClip(clip);
        }

        private void PlayClip(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private static AudioClip CreateSynthesizedClip(string name, float duration, System.Func<float, float, float> waveformFunc)
        {
            int totalSamples = Mathf.RoundToInt(duration * SampleRate);
            float[] samples = new float[totalSamples];
            for (int i = 0; i < totalSamples; i++)
            {
                float time = (float)i / SampleRate;
                samples[i] = waveformFunc(time, duration);
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}

using UnityEngine;

namespace CleanMyRoom.ColorSorting
{
    /// <summary>
    /// Small audio helper for activity feedback.
    ///
    /// The repository has no pre-existing AudioManager or audio asset files,
    /// so short notification tones are generated at runtime with AudioClip.SetData.
    /// This avoids adding binary assets or external dependencies. The manager
    /// only plays sounds; all correctness decisions remain with MATLAB.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ActivityAudioManager : MonoBehaviour
    {
        public static ActivityAudioManager Instance { get; private set; }

        [SerializeField] private bool muted;
        [SerializeField, Range(0f, 1f)] private float volume = 0.45f;

        private AudioSource source;
        private AudioClip correctClip;
        private AudioClip wrongClip;
        private AudioClip completionClip;

        public bool Muted
        {
            get { return muted; }
            set
            {
                muted = value;
                if (source != null)
                {
                    source.mute = muted;
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            if (Object.FindAnyObjectByType<ActivityAudioManager>() != null)
            {
                return;
            }
            GameObject root = new GameObject("Activity Audio Manager");
            root.AddComponent<ActivityAudioManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            source = gameObject.GetComponent<AudioSource>();
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
            }
            source.playOnAwake = false;
            source.mute = muted;
            source.volume = volume;

            correctClip = MakeTone("CMR_Correct", 660f, 880f, 0.18f);
            wrongClip = MakeTone("CMR_Wrong", 300f, 190f, 0.24f);
            completionClip = MakeTone("CMR_Complete", 523f, 1046f, 0.55f);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            DestroyClip(correctClip);
            DestroyClip(wrongClip);
            DestroyClip(completionClip);
        }

        public void PlayCorrect()
        {
            Play(correctClip);
        }

        public void PlayWrong()
        {
            Play(wrongClip);
        }

        public void PlayCompletion()
        {
            Play(completionClip);
        }

        private void Play(AudioClip clip)
        {
            if (clip == null || source == null || muted)
            {
                return;
            }
            source.PlayOneShot(clip, volume);
        }

        private static AudioClip MakeTone(string name, float startHz, float endHz, float seconds)
        {
            const int sampleRate = 22050;
            int count = Mathf.Max(1, Mathf.RoundToInt(sampleRate * seconds));
            float[] samples = new float[count];
            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float frequency = Mathf.Lerp(startHz, endHz, t);
                phase += 2f * Mathf.PI * frequency / sampleRate;
                float envelope = Mathf.Sin(Mathf.PI * t);
                samples[i] = Mathf.Sin(phase) * envelope * 0.45f;
            }

            AudioClip clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static void DestroyClip(AudioClip clip)
        {
            if (clip != null)
            {
                Object.Destroy(clip);
            }
        }
    }
}

using System;
using CleanMyRoom.Gameplay;
using CleanMyRoom.Net;
using UnityEngine;

namespace CleanMyRoom.UI
{
    /// <summary>
    /// Centralized UI audio. The singleton is created at BeforeSceneLoad, so it
    /// exists before any scene object's Awake/OnEnable, survives every scene
    /// transition, and works when a gameplay scene is opened directly.
    ///
    /// It only reacts to authoritative MATLAB results/events; it never decides
    /// whether an answer is correct (AGENTS.md sections 3 and 4).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioManager : MonoBehaviour
    {
        private const string CatalogPath = "Audio/UiAudioCatalog";

        private static AudioManager instance;
        private static UiAudioCatalog catalog;
        private static bool catalogWarningIssued;
        private static bool networkSubscribed;

        private AudioSource source;

        public static AudioManager Instance
        {
            get
            {
                EnsureInstance();
                if (instance == null)
                {
                    // Never fail silently: a missing manager is a setup error.
                    Debug.LogError("[AudioManager] Singleton could not be created.");
                }
                return instance;
            }
        }

        // BeforeSceneLoad runs after scene objects are loaded but before Awake,
        // so UI components can safely use AudioManager.Instance in Awake.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInstance();
        }

        /// <summary>
        /// Idempotent singleton creation. There is deliberately no "creating"
        /// flag: during AddComponent/Awake the new component is already visible
        /// to FindAnyObjectByType, so a reentrant call adopts the same object
        /// instead of returning null.
        /// </summary>
        internal static void EnsureInstance()
        {
            if (instance != null)
            {
                return;
            }

            AudioManager existing = FindAnyObjectByType<AudioManager>();
            if (existing != null)
            {
                instance = existing;
                return;
            }

            GameObject root = new GameObject("Audio Manager");
            AudioManager created = root.AddComponent<AudioManager>();
            if (instance == null)
            {
                instance = created;
            }

            if (instance.transform.parent == null && Application.isPlaying)
            {
                DontDestroyOnLoad(instance.gameObject);
            }
        }

        private void Awake()
        {
            if (!Register(this))
            {
                return;
            }

            source = GetComponent<AudioSource>();
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
            }
            source.playOnAwake = false;
            source.spatialBlend = 0f;

            TryLoadCatalog();
            SubscribeStaticEvents();
        }

        private void Start()
        {
            // Retry after scene setup in case Resources was not ready earlier.
            TryLoadCatalog();
            SubscribeNetworkEvents();
        }

        private void Update()
        {
            // NetworkManager creates itself at AfterSceneLoad, which may be
            // later than this component. Same retry pattern as GameProgress.
            if (!networkSubscribed)
            {
                SubscribeNetworkEvents();
            }
        }

        private static bool Register(AudioManager candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            if (instance != null && !ReferenceEquals(instance, candidate))
            {
                Debug.LogWarning("[AudioManager] Duplicate instance removed.");
                if (Application.isPlaying)
                {
                    Destroy(candidate.gameObject);
                }
                else
                {
                    DestroyImmediate(candidate.gameObject);
                }
                return false;
            }

            instance = candidate;
            return true;
        }

        private void OnDisable()
        {
            UnsubscribeAll();
        }

        private void OnDestroy()
        {
            UnsubscribeAll();
            if (ReferenceEquals(instance, this))
            {
                instance = null;
            }
        }

        private static void TryLoadCatalog()
        {
            if (catalog != null)
            {
                return;
            }

            UiAudioCatalog loaded = Resources.Load<UiAudioCatalog>(CatalogPath);
            if (loaded != null)
            {
                catalog = loaded;
                return;
            }

            if (!catalogWarningIssued)
            {
                Debug.LogWarning(
                    "[AudioManager] UiAudioCatalog not found at Resources/" + CatalogPath +
                    "; UI audio is disabled until it is imported.");
                catalogWarningIssued = true;
            }
        }

        private void SubscribeStaticEvents()
        {
            GameProgress.ActivityCompleted -= HandleActivityCompleted;
            GameProgress.ActivityCompleted += HandleActivityCompleted;
            FloatingStar.StarCollected -= HandleStarCollected;
            FloatingStar.StarCollected += HandleStarCollected;
        }

        private void UnsubscribeAll()
        {
            GameProgress.ActivityCompleted -= HandleActivityCompleted;
            FloatingStar.StarCollected -= HandleStarCollected;

            NetworkManager manager = NetworkManager.Instance;
            if (manager != null && networkSubscribed)
            {
                manager.MessageReceived -= HandleMessage;
            }
            networkSubscribed = false;
        }

        private void SubscribeNetworkEvents()
        {
            NetworkManager manager = NetworkManager.Instance;
            if (manager == null || networkSubscribed)
            {
                return;
            }

            manager.MessageReceived -= HandleMessage;
            manager.MessageReceived += HandleMessage;
            networkSubscribed = true;
        }

        private void HandleActivityCompleted(string activityId)
        {
            PlaySuccess();
        }

        private void HandleStarCollected(string activityId)
        {
            PlayStarReward();
        }

        private void HandleMessage(WireMessage message)
        {
            if (message == null)
            {
                return;
            }

            if (message.IsType(ProtocolConstants.TypeError))
            {
                PlayError();
                return;
            }

            // A PLACE_RES is an authoritative MATLAB verdict; a rejected
            // placement is an error cue. No local validation is performed.
            if (message.IsType(ProtocolConstants.TypePlaceResponse) && !message.valid)
            {
                PlayError();
            }
        }

        public void PlayButtonClick()
        {
            Play(Select(delegate(UiAudioCatalog c) { return c.ButtonClick; }), "ButtonClick");
        }

        public void PlaySuccess()
        {
            Play(Select(delegate(UiAudioCatalog c) { return c.Success; }), "Success");
        }

        public void PlayError()
        {
            Play(Select(delegate(UiAudioCatalog c) { return c.Error; }), "Error");
        }

        public void PlayStarReward()
        {
            Play(Select(delegate(UiAudioCatalog c) { return c.StarReward; }), "StarReward");
        }

        private static AudioClip Select(Func<UiAudioCatalog, AudioClip> selector)
        {
            TryLoadCatalog();
            return catalog != null ? selector(catalog) : null;
        }

        private void Play(AudioClip clip, string cue)
        {
            if (source == null)
            {
                source = GetComponent<AudioSource>();
            }
            if (source == null || clip == null)
            {
                return;
            }
            source.PlayOneShot(clip);
        }
    }
}

using UnityEngine;

namespace CleanMyRoom.UI
{
    /// <summary>
    /// ScriptableObject holding the four approved Kenney interface sounds.
    /// It lives in Resources so an auto-created AudioManager can load it from
    /// any entry scene without Inspector wiring.
    /// </summary>
    [CreateAssetMenu(fileName = "UiAudioCatalog", menuName = "Clean My Room/UI Audio Catalog")]
    public sealed class UiAudioCatalog : ScriptableObject
    {
        [SerializeField] private AudioClip buttonClick;
        [SerializeField] private AudioClip success;
        [SerializeField] private AudioClip error;
        [SerializeField] private AudioClip starReward;

        public AudioClip ButtonClick { get { return buttonClick; } set { buttonClick = value; } }
        public AudioClip Success { get { return success; } set { success = value; } }
        public AudioClip Error { get { return error; } set { error = value; } }
        public AudioClip StarReward { get { return starReward; } set { starReward = value; } }
    }
}

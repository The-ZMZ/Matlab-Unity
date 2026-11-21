using System;
using UnityEngine;

namespace CleanMyRoom.UI
{
    /// <summary>
    /// Package-free English text catalog for the Issue #015 UI.
    /// Only new UI text is sourced from here; the existing Interaction Hint
    /// and connection overlay are deliberately left unchanged.
    /// </summary>
    [CreateAssetMenu(fileName = "UiTextCatalog", menuName = "Clean My Room/UI Text Catalog")]
    public sealed class UiTextCatalog : ScriptableObject
    {
        [Serializable]
        private sealed class Entry
        {
            public string key = string.Empty;
            public string text = string.Empty;
        }

        [SerializeField] private Entry[] entries = new Entry[0];

        /// <summary>Returns the English string for a key, or the key itself if absent.</summary>
        public string Get(string key)
        {
            string text;
            return TryGet(key, out text) ? text : key;
        }

        public bool TryGet(string key, out string text)
        {
            text = string.Empty;
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                Entry entry = entries[i];
                if (entry != null && string.Equals(entry.key, key, StringComparison.Ordinal))
                {
                    text = entry.text ?? string.Empty;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Used by the Issue #015 asset builder to populate the catalog asset.</summary>
        public void SetText(string key, string text)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                Entry entry = entries[i];
                if (entry != null && string.Equals(entry.key, key, StringComparison.Ordinal))
                {
                    entry.text = text ?? string.Empty;
                    return;
                }
            }

            Entry[] expanded = new Entry[entries.Length + 1];
            Array.Copy(entries, expanded, entries.Length);
            expanded[entries.Length] = new Entry { key = key, text = text ?? string.Empty };
            entries = expanded;
        }
    }
}

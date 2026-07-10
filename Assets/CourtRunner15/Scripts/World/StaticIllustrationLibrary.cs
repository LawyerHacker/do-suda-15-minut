using System.Collections.Generic;
using UnityEngine;

namespace CourtRunner15
{
    /// <summary>
    /// Runtime-каталог статичных иллюстраций. Сам asset лежит в Resources,
    /// а текстуры остаются в Assets/CourtRunner15/Art/StaticIllustrations.
    /// </summary>
    public class StaticIllustrationLibrary : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public string Name;
            public Texture2D Texture;
        }

        public Entry[] Entries = new Entry[0];

        [System.NonSerialized] private Dictionary<string, Texture2D> _lookup;

        public int Count
        {
            get { return Entries != null ? Entries.Length : 0; }
        }

        public static StaticIllustrationLibrary Load()
        {
            return Resources.Load<StaticIllustrationLibrary>("StaticIllustrationLibrary");
        }

        public Texture2D GetTexture(string name)
        {
            BuildLookup();
            Texture2D tex;
            return _lookup != null && _lookup.TryGetValue(name, out tex) ? tex : null;
        }

        public bool Has(string name)
        {
            return GetTexture(name) != null;
        }

        private void BuildLookup()
        {
            if (_lookup != null) return;
            _lookup = new Dictionary<string, Texture2D>();
            if (Entries == null) return;

            for (int i = 0; i < Entries.Length; i++)
            {
                Entry e = Entries[i];
                if (e == null || string.IsNullOrEmpty(e.Name) || e.Texture == null) continue;
                _lookup[e.Name] = e.Texture;
            }
        }
    }
}

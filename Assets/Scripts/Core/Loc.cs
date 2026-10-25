using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoneTruth.Core
{
    /// <summary>
    /// Runtime localization. Game text is authored in Russian (the source language) and the
    /// Russian line itself is the key; every other language is one JSON file in
    /// Resources/Localization/ that maps those keys to translations. Adding a language = copying
    /// en.json, changing its code/name and translating the values — it shows up in the language
    /// switcher on its own. Missing translations fall back to the Russian text.
    /// </summary>
    public static class Loc
    {
        public const string SourceCode = "ru";
        public const string SourceName = "Русский";
        public const string DefaultCode = "en";

        private const string PrefKey = "language";
        private const string ResourceFolder = "Localization";

        [Serializable]
        private class LanguageFile
        {
            public string code;
            public string name;
            public Entry[] entries;
        }

        [Serializable]
        private class Entry
        {
            public string key;
            public string value;
        }

        public struct Language
        {
            public string Code;
            public string Name;
        }

        private static List<Language> languages;
        private static Dictionary<string, Dictionary<string, string>> tables;
        private static Dictionary<string, string> current;
        private static string currentCode;
        private static HashSet<string> reportedMissing;

        /// <summary>Raised after the language changes; UI that caches text re-renders on it.</summary>
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            languages = null;
            tables = null;
            current = null;
            currentCode = null;
            reportedMissing = null;
            Changed = null;
        }

        public static IReadOnlyList<Language> Languages
        {
            get
            {
                EnsureLoaded();
                return languages;
            }
        }

        public static string CurrentCode
        {
            get
            {
                EnsureLoaded();
                return currentCode;
            }
        }

        public static void SetLanguage(string code)
        {
            EnsureLoaded();
            if (code == currentCode || (code != SourceCode && !tables.ContainsKey(code)))
            {
                return;
            }

            Apply(code);
            PlayerPrefs.SetString(PrefKey, code);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>Translates a Russian source line into the current language.</summary>
        public static string T(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return key;
            }

            EnsureLoaded();
            if (current == null)
            {
                return key;
            }

            if (current.TryGetValue(Normalize(key), out string value) && !string.IsNullOrEmpty(value))
            {
                return value;
            }

            ReportMissing(key);
            return key;
        }

        /// <summary>Whether a language file has a non-empty translation for this key (editor checks).</summary>
        public static bool HasTranslation(string code, string key)
        {
            EnsureLoaded();
            return code == SourceCode ||
                   (tables.TryGetValue(code, out var table) && table.TryGetValue(Normalize(key), out string value) &&
                    !string.IsNullOrEmpty(value));
        }

        /// <summary>T() for a format string: Loc.Format("Заряжено: {0}", n).</summary>
        public static string Format(string key, params object[] args)
        {
            return string.Format(T(key), args);
        }

        private static void EnsureLoaded()
        {
            if (tables != null)
            {
                return;
            }

            tables = new Dictionary<string, Dictionary<string, string>>();
            languages = new List<Language>();
            var found = new List<Language>();
            foreach (TextAsset asset in Resources.LoadAll<TextAsset>(ResourceFolder))
            {
                LanguageFile file;
                try
                {
                    file = JsonUtility.FromJson<LanguageFile>(asset.text);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Loc] Cannot parse {ResourceFolder}/{asset.name}: {e.Message}");
                    continue;
                }

                if (file == null || string.IsNullOrEmpty(file.code) || file.code == SourceCode)
                {
                    Debug.LogError($"[Loc] {ResourceFolder}/{asset.name} needs a unique \"code\" other than \"{SourceCode}\".");
                    continue;
                }

                var table = new Dictionary<string, string>();
                if (file.entries != null)
                {
                    foreach (Entry entry in file.entries)
                    {
                        if (!string.IsNullOrEmpty(entry.key))
                        {
                            table[Normalize(entry.key)] = entry.value;
                        }
                    }
                }

                tables[file.code] = table;
                found.Add(new Language { Code = file.code, Name = string.IsNullOrEmpty(file.name) ? file.code : file.name });
            }

            // Default language first, then the source language, then the rest by code.
            found.Sort((a, b) => string.CompareOrdinal(a.Code, b.Code));
            int def = found.FindIndex(l => l.Code == DefaultCode);
            if (def >= 0)
            {
                languages.Add(found[def]);
                found.RemoveAt(def);
            }

            languages.Add(new Language { Code = SourceCode, Name = SourceName });
            languages.AddRange(found);

            string saved = PlayerPrefs.GetString(PrefKey, DefaultCode);
            Apply(saved == SourceCode || tables.ContainsKey(saved) ? saved : languages[0].Code);
        }

        private static void Apply(string code)
        {
            currentCode = code;
            current = code == SourceCode ? null : tables[code];
        }

        private static string Normalize(string key)
        {
            return key.Replace("\r\n", "\n");
        }

        private static void ReportMissing(string key)
        {
#if UNITY_EDITOR
            // Only lines that are actually Russian need a translation (numbers, "$ 50" etc. do not).
            bool cyrillic = false;
            foreach (char c in key)
            {
                if (c >= 'Ѐ' && c <= 'ӿ')
                {
                    cyrillic = true;
                    break;
                }
            }

            reportedMissing ??= new HashSet<string>();
            if (cyrillic && reportedMissing.Add(key))
            {
                Debug.LogWarning($"[Loc] No \"{currentCode}\" translation for: {key}");
            }
#endif
        }
    }
}

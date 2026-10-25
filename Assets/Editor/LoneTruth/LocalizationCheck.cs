using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using LoneTruth.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LoneTruth.EditorTools
{
    /// <summary>
    /// Lists every Russian string stored in the build scenes and in ScriptableObjects (dialogue,
    /// prompts, item names…) that a language file in Resources/Localization does not translate yet.
    /// Strings written directly in runtime C# (toasts like "Заряжено: {0}") are not scanned — they
    /// are listed in Docs/Localization.md.
    /// </summary>
    public static class LocalizationCheck
    {
        private static readonly Regex Cyrillic = new Regex("[Ѐ-ӿ]");

        [MenuItem("Lone Truth/Localization/Check Translations")]
        public static void Check()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var keys = new List<string>();
            var seen = new HashSet<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/ScriptableObjects" }))
            {
                Collect(AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid)), keys, seen);
            }

            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
            {
                Scene scene = SceneManager.GetSceneByPath(buildScene.path);
                bool wasOpen = scene.isLoaded;
                if (!wasOpen)
                {
                    scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Additive);
                }

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        Collect(behaviour, keys, seen);
                    }
                }

                if (!wasOpen)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            foreach (Loc.Language language in Loc.Languages)
            {
                if (language.Code == Loc.SourceCode)
                {
                    continue;
                }

                var report = new StringBuilder();
                int missing = 0;
                foreach (string key in keys)
                {
                    if (!Loc.HasTranslation(language.Code, key))
                    {
                        missing++;
                        report.AppendLine(key.Replace("\n", "\\n"));
                    }
                }

                if (missing == 0)
                {
                    Debug.Log($"[Loc] {language.Name} ({language.Code}): all {keys.Count} scene/asset strings are translated.");
                }
                else
                {
                    Debug.LogWarning($"[Loc] {language.Name} ({language.Code}): {missing} of {keys.Count} strings have no translation:\n{report}");
                }
            }
        }

        private static void Collect(Object obj, List<string> keys, HashSet<string> seen)
        {
            if (obj == null)
            {
                return;
            }

            SerializedProperty it = new SerializedObject(obj).GetIterator();
            while (it.Next(true))
            {
                if (it.propertyType != SerializedPropertyType.String)
                {
                    continue;
                }

                string value = it.stringValue;
                if (!string.IsNullOrEmpty(value) && Cyrillic.IsMatch(value) && seen.Add(value))
                {
                    keys.Add(value);
                }
            }
        }
    }
}

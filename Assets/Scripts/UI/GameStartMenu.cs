using System.Collections;
using LoneTruth.Core;
using LoneTruth.Story;
using UnityEngine;

namespace LoneTruth.UI
{
    /// <summary>
    /// Sits in the first scene. If saves exist, holds the scene-start cutscene (the prologue) and
    /// asks «New game / Continue» over a black screen; with no saves it does nothing.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class GameStartMenu : MonoBehaviour
    {
        private static bool asked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            asked = false;
        }

        private void Awake()
        {
            if (asked || StoryState.Has(StoryFlags.PrologueDone) || !SaveSystem.AnySaves())
            {
                return;
            }

            CutsceneTrigger.HoldSceneStart = true;
        }

        private IEnumerator Start()
        {
            if (!CutsceneTrigger.HoldSceneStart)
            {
                yield break;
            }

            asked = true;
            if (ScreenFader.Instance != null)
            {
                ScreenFader.Instance.SetAlphaImmediate(1f);
            }

            while (SaveMenuUI.Instance == null || !SaveMenuUI.Instance.IsReady)
            {
                yield return null;
            }

            // Lives on the persistent UI: Continue unloads this scene while it runs.
            SaveMenuUI.Instance.StartCoroutine(SaveMenuUI.Instance.StartMenu(_ => CutsceneTrigger.HoldSceneStart = false));
        }
    }
}

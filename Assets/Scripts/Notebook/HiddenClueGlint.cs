using LoneTruth.Core;
using UnityEngine;

namespace LoneTruth.Investigation
{
    /// <summary>
    /// «Взгляд сыщика» (Truth soft bonus, level 1): a hidden clue glints until it is found. Without
    /// the bonus the clue is still there — it is found by inspecting the spot.
    /// </summary>
    public class HiddenClueGlint : MonoBehaviour
    {
        [SerializeField] private string clueFlag;
        [SerializeField] private GameObject glint;
        [SerializeField] private float pulseSpeed = 3f;

        public void Configure(string flag, GameObject glintObject)
        {
            clueFlag = flag;
            glint = glintObject;
        }

        private void OnEnable()
        {
            StoryState.Changed += Apply;
            Apply();
        }

        private void OnDisable()
        {
            StoryState.Changed -= Apply;
        }

        private void Apply()
        {
            if (glint != null)
            {
                glint.SetActive(ShardManager.BonusLevel(ShardType.Truth) >= 1 && !StoryState.Has(clueFlag));
            }
        }

        private void Update()
        {
            if (glint != null && glint.activeSelf)
            {
                glint.transform.localScale = Vector3.one * (0.8f + 0.25f * Mathf.Sin(Time.time * pulseSpeed));
            }
        }
    }
}

using UnityEngine;

namespace LoneTruth.Core
{
    /// <summary>
    /// Clears short-lived story flags whenever the scene opens — e.g. the bed's "rest once more
    /// to end the day" confirmation, so it never carries over from an earlier visit.
    /// </summary>
    public class ClearFlagsOnSceneStart : MonoBehaviour
    {
        [SerializeField] private string[] flags;

        public void Configure(params string[] flagsToClear)
        {
            flags = flagsToClear;
        }

        private void Start()
        {
            foreach (string flag in flags)
            {
                StoryState.Set(flag, false);
            }
        }
    }
}

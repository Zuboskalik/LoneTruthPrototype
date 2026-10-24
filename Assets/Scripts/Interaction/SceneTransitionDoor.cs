using LoneTruth.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LoneTruth.Interaction
{
    /// <summary>
    /// A transition between two built locations that live in separate scenes.
    /// Uses the existing ScreenFader, then places the player on the SpawnPoint
    /// with the matching id in the target scene.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SceneTransitionDoor : MonoBehaviour, IInteractable
    {
        [Header("Transition")]
        [SerializeField] private string transitionId = "1-01_to_1-02";
        [SerializeField] private string targetScene = "MainStreet";
        [SerializeField] private string targetSpawnId = "from_1-01";

        [Header("Prompts")]
        [SerializeField] private string prompt = "Go";

        [Header("Audio")]
        [SerializeField] private AudioClip transitionSound;

        private AudioSource audioSource;
        private bool isLoading;

        public string TransitionId => transitionId;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        public void Interact(GameObject interactor)
        {
            if (isLoading || string.IsNullOrEmpty(targetScene))
            {
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(targetScene))
            {
                Debug.LogWarning($"[{transitionId}] Scene '{targetScene}' is not in Build Settings.");
                return;
            }

            isLoading = true;

            if (transitionSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(transitionSound);
            }

            SpawnPoint.PendingSpawnId = targetSpawnId;

            if (ScreenFader.Instance != null)
            {
                ScreenFader.Instance.FadeAndExecute(() => SceneManager.LoadScene(targetScene));
            }
            else
            {
                SceneManager.LoadScene(targetScene);
            }
        }

        public string GetInteractionPrompt()
        {
            return prompt;
        }
    }
}

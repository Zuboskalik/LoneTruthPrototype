using LoneTruth.Core;
using LoneTruth.Inventory;
using LoneTruth.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LoneTruth.Interaction
{
    /// <summary>
    /// A transition between two built locations that live in separate scenes.
    /// Uses the existing ScreenFader, then places the player on the SpawnPoint
    /// with the matching id in the target scene. Optionally locked: the required key has to be
    /// used on the door from the inventory (select it, press G), after which it stays open.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SceneTransitionDoor : MonoBehaviour, IInteractable, IItemReceiver
    {
        [Header("Transition")]
        [SerializeField] private string transitionId = "1-01_to_1-02";
        [SerializeField] private string targetScene = "MainStreet";
        [SerializeField] private string targetSpawnId = "from_1-01";

        [Header("Prompts")]
        [SerializeField] private string prompt = "Go";

        [Header("Lock (optional)")]
        [SerializeField] private ItemSO requiredItem;
        [SerializeField] private string lockedLine = "Заперто.";
        [SerializeField] private string unlockedLine = "Открыто.";

        [Header("Audio")]
        [SerializeField] private AudioClip transitionSound;

        private AudioSource audioSource;
        private bool isLoading;

        public string TransitionId => transitionId;

        private string UnlockFlag => "unlocked_" + transitionId;

        private bool IsLocked => requiredItem != null && !StoryState.Has(UnlockFlag);

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        public void Interact(GameObject interactor)
        {
            if (IsLocked)
            {
                if (GameUI.Instance != null)
                {
                    GameUI.Instance.Toast(lockedLine);
                }

                return;
            }

            Go();
        }

        public bool TryUseItem(ItemSO item, GameObject user)
        {
            if (!IsLocked || item != requiredItem)
            {
                return false;
            }

            StoryState.Set(UnlockFlag);
            if (GameUI.Instance != null)
            {
                GameUI.Instance.Toast(unlockedLine);
            }

            return true;
        }

        public string GetInteractionPrompt()
        {
            return IsLocked ? prompt + " (заперто)" : prompt;
        }

        private void Go()
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
    }
}

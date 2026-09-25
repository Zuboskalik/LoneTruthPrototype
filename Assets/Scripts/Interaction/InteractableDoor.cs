using LoneTruth.Core;
using LoneTruth.Inventory;
using UnityEngine;

namespace LoneTruth.Interaction
{
    [RequireComponent(typeof(AudioSource))]
    public class InteractableDoor : MonoBehaviour, IInteractable
    {
        [Header("Door")]
        [SerializeField] private Transform targetDoor;
        [SerializeField] private ItemSO requiredKey;

        [Header("Prompts")]
        [SerializeField] private string unlockPrompt = "Open Door";
        [SerializeField] private string lockedPrompt = "Locked";

        [Header("Audio")]
        [SerializeField] private AudioClip doorSound;

        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        public void Interact(GameObject interactor)
        {
            if (targetDoor == null || interactor == null)
            {
                return;
            }

            InventoryManager inventory = interactor.GetComponent<InventoryManager>();
            if (inventory == null)
            {
                inventory = interactor.GetComponentInParent<InventoryManager>();
            }

            bool hasKey = requiredKey == null || (inventory != null && inventory.HasItem(requiredKey));

            if (!hasKey)
            {
                return;
            }

            PlayDoorSound();

            if (ScreenFader.Instance != null)
            {
                ScreenFader.Instance.FadeAndExecute(() => TeleportPlayer(interactor));
            }
            else
            {
                TeleportPlayer(interactor);
            }
        }

        public string GetInteractionPrompt()
        {
            if (requiredKey == null)
            {
                return unlockPrompt;
            }

            return unlockPrompt + " (" + lockedPrompt + " unless you have the key)";
        }

        private void TeleportPlayer(GameObject interactor)
        {
            if (interactor == null || targetDoor == null)
            {
                return;
            }

            Rigidbody2D rb = interactor.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.position = targetDoor.position;
            }

            interactor.transform.position = targetDoor.position;
        }

        private void PlayDoorSound()
        {
            if (doorSound == null || audioSource == null)
            {
                return;
            }

            audioSource.PlayOneShot(doorSound);
        }
    }
}

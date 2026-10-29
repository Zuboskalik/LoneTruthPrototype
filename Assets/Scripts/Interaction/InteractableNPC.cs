using System.Collections;
using LoneTruth.Core;
using LoneTruth.UI;
using UnityEngine;
using UnityEngine.UI;

namespace LoneTruth.Interaction
{
    public class InteractableNPC : MonoBehaviour, IInteractable
    {
        [Header("Dialogue")]
        [SerializeField] private string interactionPrompt = "Talk";
        [SerializeField] private string dialogueLine = "Take the key from the table.";
        [SerializeField] private float dialogueDisplayDuration = 3f;

        [Header("Floating Text UI")]
        [SerializeField] private Vector3 textOffset = new Vector3(0f, 2f, 0f);
        [SerializeField] private Vector2 textCanvasSize = new Vector2(3f, 0.8f);
        [SerializeField] private int textFontSize = 24;
        [SerializeField] private Color textColor = Color.white;

        [Header("Key Spawn")]
        [SerializeField] private GameObject keyItemPrefab;
        [SerializeField] private Transform tableSpawnPoint;

        private Canvas dialogueCanvas;
        private Text dialogueText;
        private Coroutine hideDialogueRoutine;

        private bool keySpawned;

        private void Awake()
        {
            BuildDialogueUI();
        }

        public void Interact(GameObject interactor)
        {
            PlayReaction();
            ShowDialogue(dialogueLine);
            SpawnKeyOnTable();
        }

        public string GetInteractionPrompt()
        {
            // No prompt while the line is up: both sit above the head and would overlap.
            return dialogueCanvas != null && dialogueCanvas.gameObject.activeSelf ? "" : interactionPrompt;
        }

        private void SpawnKeyOnTable()
        {
            if (keySpawned || keyItemPrefab == null || tableSpawnPoint == null)
            {
                return;
            }

            Instantiate(keyItemPrefab, tableSpawnPoint.position, tableSpawnPoint.rotation);
            keySpawned = true;
        }

        /// <summary>Plays the "React" trigger (a short gesture with the line), if the animator has one.</summary>
        private void PlayReaction()
        {
            Animator animator = GetComponentInChildren<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.name == "React" && p.type == AnimatorControllerParameterType.Trigger)
                {
                    animator.SetTrigger("React");
                    return;
                }
            }
        }

        private void ShowDialogue(string text)
        {
            dialogueText.text = Loc.T(text);
            dialogueCanvas.gameObject.SetActive(true);
            PositionDialogueUI();

            if (hideDialogueRoutine != null)
            {
                StopCoroutine(hideDialogueRoutine);
            }

            hideDialogueRoutine = StartCoroutine(HideDialogueAfterDelay());
        }

        private IEnumerator HideDialogueAfterDelay()
        {
            yield return new WaitForSeconds(dialogueDisplayDuration);
            dialogueCanvas.gameObject.SetActive(false);
            hideDialogueRoutine = null;
        }

        private void PositionDialogueUI()
        {
            dialogueCanvas.transform.position = transform.position + textOffset;

            Camera cam = Camera.main;
            if (cam != null)
            {
                dialogueCanvas.transform.forward = cam.transform.forward;
            }
        }

        private void LateUpdate()
        {
            if (dialogueCanvas.gameObject.activeSelf)
            {
                PositionDialogueUI();
            }
        }

        private void BuildDialogueUI()
        {
            GameObject canvasObject = new GameObject("NPCDialogueCanvas");
            canvasObject.transform.SetParent(transform, false);

            dialogueCanvas = canvasObject.AddComponent<Canvas>();
            dialogueCanvas.renderMode = RenderMode.WorldSpace;

            // The canvas RectTransform lives in its own local units, which is also the space
            // the Text component's fontSize is measured in. Using textCanvasSize directly (a
            // handful of local units) made the box far narrower than a 24pt glyph, so Wrap
            // broke the line after every single character. Scale up only the box (not the
            // transform) so fontSize keeps its original on-screen size and the text has room
            // to wrap by word instead of by letter.
            const float canvasUnitScale = 100f;
            RectTransform canvasRect = dialogueCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = textCanvasSize * canvasUnitScale;
            canvasObject.transform.localScale = Vector3.one * 0.01f;

            GameObject textObject = new GameObject("DialogueText");
            textObject.transform.SetParent(canvasObject.transform, false);

            dialogueText = textObject.AddComponent<Text>();
            dialogueText.font = UIFonts.Default;
            dialogueText.fontSize = textFontSize;
            dialogueText.alignment = TextAnchor.MiddleCenter;
            dialogueText.color = textColor;
            dialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
            dialogueText.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform textRect = dialogueText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            dialogueCanvas.gameObject.SetActive(false);
        }

        private void OnDrawGizmosSelected()
        {
            if (tableSpawnPoint == null)
            {
                return;
            }

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, tableSpawnPoint.position);
            Gizmos.DrawWireSphere(tableSpawnPoint.position, 0.2f);
        }
    }
}

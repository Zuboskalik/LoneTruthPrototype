using UnityEngine;
using UnityEngine.UI;

namespace LoneTruth.Interaction
{
    public class InteractionDetector : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] private float detectionRadius = 1.5f;
        [SerializeField] private LayerMask interactableLayer = ~0;
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        [Header("Prompt UI")]
        [SerializeField] private Vector3 promptOffset = new Vector3(0f, 1.5f, 0f);
        [SerializeField] private Vector2 promptCanvasSize = new Vector2(2.5f, 0.6f);
        [SerializeField] private int promptFontSize = 24;
        [SerializeField] private Color promptColor = Color.white;

        private Canvas promptCanvas;
        private Text promptText;

        private IInteractable currentTarget;
        private Transform currentTargetTransform;

        private void Awake()
        {
            BuildPromptUI();
        }

        private void Update()
        {
            FindNearestInteractable();
            UpdatePromptUI();

            if (currentTarget != null && Input.GetKeyDown(interactKey))
            {
                currentTarget.Interact(gameObject);
            }
        }

        private void FindNearestInteractable()
        {
            currentTarget = null;
            currentTargetTransform = null;

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, interactableLayer);

            float nearestDistanceSqr = float.MaxValue;

            foreach (Collider2D hit in hits)
            {
                IInteractable interactable = hit.GetComponentInParent<IInteractable>();
                if (interactable == null)
                {
                    continue;
                }

                float distanceSqr = (hit.transform.position - transform.position).sqrMagnitude;
                if (distanceSqr < nearestDistanceSqr)
                {
                    nearestDistanceSqr = distanceSqr;
                    currentTarget = interactable;
                    currentTargetTransform = hit.transform;
                }
            }
        }

        private void UpdatePromptUI()
        {
            if (currentTarget == null || currentTargetTransform == null)
            {
                promptCanvas.gameObject.SetActive(false);
                return;
            }

            promptCanvas.gameObject.SetActive(true);
            promptText.text = currentTarget.GetInteractionPrompt();
            promptCanvas.transform.position = currentTargetTransform.position + promptOffset;

            Camera cam = Camera.main;
            if (cam != null)
            {
                promptCanvas.transform.forward = cam.transform.forward;
            }
        }

        private void BuildPromptUI()
        {
            GameObject canvasObject = new GameObject("InteractionPromptCanvas");
            canvasObject.transform.SetParent(null);

            promptCanvas = canvasObject.AddComponent<Canvas>();
            promptCanvas.renderMode = RenderMode.WorldSpace;

            RectTransform canvasRect = promptCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = promptCanvasSize;
            canvasObject.transform.localScale = Vector3.one * 0.01f;

            GameObject textObject = new GameObject("PromptText");
            textObject.transform.SetParent(canvasObject.transform, false);

            promptText = textObject.AddComponent<Text>();
            promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            promptText.fontSize = promptFontSize;
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.color = promptColor;
            promptText.horizontalOverflow = HorizontalWrapMode.Overflow;
            promptText.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform textRect = promptText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            promptCanvas.gameObject.SetActive(false);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}

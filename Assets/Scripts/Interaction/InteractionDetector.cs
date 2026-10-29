using LoneTruth.Core;
using LoneTruth.UI;
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
        [Tooltip("World offset above the target where the prompt is shown.")]
        [SerializeField] private Vector3 promptOffset = new Vector3(0f, 1.5f, 0f);
        [Tooltip("Font size on the 1920×1080 reference screen.")]
        [SerializeField] private int promptTextSize = 32;
        [SerializeField] private Color promptColor = Color.white;

        private Canvas promptCanvas;
        private RectTransform promptCanvasRect;
        private Text promptText;

        private IInteractable currentTarget;
        private Transform currentTargetTransform;

        private void Awake()
        {
            BuildPromptUI();
            weapon = GetComponent<LoneTruth.Combat.WeaponController>();
        }

        public IInteractable CurrentTarget => currentTarget;

        private LoneTruth.Combat.WeaponController weapon;

        private void Update()
        {
            if (LoneTruth.Core.GameMaster.Instance != null && !LoneTruth.Core.GameMaster.Instance.IsInState(LoneTruth.Core.GameState.Exploration))
            {
                currentTarget = null;
                promptCanvas.gameObject.SetActive(false);
                return;
            }

            // In aiming E fires the revolver instead of interacting.
            if (weapon != null && weapon.IsAiming)
            {
                currentTarget = null;
                promptCanvas.gameObject.SetActive(false);
                return;
            }

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

            Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, interactableLayer);

            float nearestDistanceSqr = float.MaxValue;

            foreach (Collider hit in hits)
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

            // The prompt is screen-space UI placed over the target, so walls never cut it.
            Camera cam = Camera.main;
            Vector3 screen = cam != null ? cam.WorldToScreenPoint(currentTargetTransform.position + promptOffset) : Vector3.back;
            if (screen.z <= 0f)
            {
                promptCanvas.gameObject.SetActive(false);
                return;
            }

            string prompt = currentTarget.GetInteractionPrompt();
            promptCanvas.gameObject.SetActive(!string.IsNullOrEmpty(prompt));
            promptText.text = Loc.T(prompt);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(promptCanvasRect, screen, null, out Vector2 local);
            promptText.rectTransform.anchoredPosition = local;
        }

        private void BuildPromptUI()
        {
            GameObject canvasObject = new GameObject("InteractionPromptCanvas");
            // Parented to the player so the prompt survives scene loads with a persistent player.
            canvasObject.transform.SetParent(transform, false);

            promptCanvas = canvasObject.AddComponent<Canvas>();
            promptCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Below the game UI (dialogue, cards: 50+), so cutscene text covers it.
            promptCanvas.sortingOrder = 40;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            promptCanvasRect = (RectTransform)canvasObject.transform;

            GameObject textObject = new GameObject("PromptText");
            textObject.transform.SetParent(canvasObject.transform, false);

            promptText = textObject.AddComponent<Text>();
            promptText.font = UIFonts.Default;
            promptText.fontSize = promptTextSize;
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.color = promptColor;
            promptText.horizontalOverflow = HorizontalWrapMode.Overflow;
            promptText.verticalOverflow = VerticalWrapMode.Overflow;
            promptText.raycastTarget = false;
            var shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(2f, -2f);

            RectTransform textRect = promptText.rectTransform;
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(900f, 60f);

            promptCanvas.gameObject.SetActive(false);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}

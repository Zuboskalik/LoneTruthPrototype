using System.Collections;
using System.Collections.Generic;
using LoneTruth.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LoneTruth.UI
{
    /// <summary>
    /// Screen-space game UI that survives scene loads: full-screen text card, toasts and the
    /// current objective. Built from code in Awake so the prefab stays a single component; the
    /// dialogue panel (DialogueUI), inventory bar, watch and notebook live on the same object.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        public static GameUI Instance { get; private set; }

        [Header("Look")]
        [SerializeField] private Color speakerColor = new Color(0.78f, 0.57f, 0.18f);
        [SerializeField] private Color textColor = new Color(0.93f, 0.9f, 0.85f);
        [SerializeField] private Color mutedColor = new Color(0.64f, 0.62f, 0.56f);
        [Tooltip("Seconds a card that waits for input stays up before the \"press E\" hint appears.")]
        [SerializeField] private float cardHintDelay = 1f;

        private Font font;
        private Canvas canvas;
        private GameObject cardPanel;
        private Text cardText;
        private Text cardHint;
        private Text toastText;
        private Text objectiveText;
        private Coroutine toastRoutine;

        // Untranslated text currently on screen, so a language switch can re-render it.
        private string rawCard;
        private string rawToast;
        private string rawObjective;

        private RectTransform languageRoot;
        private readonly List<Text> languageLabels = new List<Text>();
        private readonly List<string> languageCodes = new List<string>();
        private int hoveredLanguage = -1;

        public Font Font => font;
        public RectTransform Root => canvas != null ? (RectTransform)canvas.transform : null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            font = UIFonts.Default;
            Build();
        }

        private void OnEnable()
        {
            Loc.Changed += Relocalize;
        }

        private void OnDisable()
        {
            Loc.Changed -= Relocalize;
        }

        private void Update()
        {
            UpdateLanguageSwitch();
        }

        // ---------------------------------------------------------------- public API

        /// <summary>Debug / automated tests: dialogue and cards advance on their own.</summary>
        public static bool AutoAdvance;

        // The project enters Play mode without a domain reload, so statics survive between runs:
        // a test that switched AutoAdvance on must not leave the next session skipping dialogue.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            AutoAdvance = false;
        }

        public static bool AdvancePressed()
        {
            if (AutoAdvance)
            {
                return Time.frameCount % 20 == 0;
            }

            return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) ||
                   Input.GetKeyDown(KeyCode.Return) || (Input.GetMouseButtonDown(0) && !PointerOverHud);
        }

        /// <summary>True while the mouse is over clickable HUD (the language switch), so a click
        /// there does not also advance dialogue or fire the revolver.</summary>
        public static bool PointerOverHud =>
            Instance != null && Instance.languageRoot != null && Instance.languageRoot.gameObject.activeInHierarchy &&
            RectTransformUtility.RectangleContainsScreenPoint(Instance.languageRoot, Input.mousePosition, null);

        /// <summary>One line of dialogue in the dialogue panel. Waits for the player.</summary>
        public IEnumerator ShowLine(string speaker, string text)
        {
            DialogueUI dialogue = GetComponent<DialogueUI>();
            if (dialogue != null)
            {
                yield return dialogue.ShowLine(speaker, text);
            }
        }

        private const string CardHintText = "Нажмите E, чтобы продолжить";

        /// <summary>Full-screen card over black (titles, letters). duration ≤ 0 waits for input.</summary>
        public IEnumerator ShowCard(string text, float duration)
        {
            cardPanel.SetActive(true);
            rawCard = text;
            cardText.text = Loc.T(text);
            Graphic[] graphics = { cardPanel.GetComponent<Image>(), cardText };
            yield return FadeGraphics(graphics, 0f, 1f, 0.6f);

            if (duration > 0f)
            {
                yield return new WaitForSeconds(duration);
            }
            else
            {
                // A letter can look like the game is idle: after a while, say how to go on.
                yield return new WaitForSeconds(0.3f);
                float waited = 0f;
                while (!AdvancePressed())
                {
                    waited += Time.unscaledDeltaTime;
                    if (waited >= cardHintDelay && !cardHint.gameObject.activeSelf)
                    {
                        cardHint.text = Loc.T(CardHintText);
                        cardHint.gameObject.SetActive(true);
                        StartCoroutine(FadeGraphics(new Graphic[] { cardHint }, 0f, 1f, 0.8f));
                    }

                    yield return null;
                }
            }

            if (cardHint.gameObject.activeSelf)
            {
                graphics = new Graphic[] { graphics[0], cardText, cardHint };
            }

            yield return FadeGraphics(graphics, 1f, 0f, 0.6f);
            cardHint.gameObject.SetActive(false);
            cardPanel.SetActive(false);
            rawCard = null;
            yield return null;
        }

        public void Toast(string text, float seconds = 3.5f)
        {
            if (toastRoutine != null)
            {
                StopCoroutine(toastRoutine);
            }

            toastRoutine = StartCoroutine(ToastRoutine(text, seconds));
        }

        public void SetObjective(string text)
        {
            rawObjective = text;
            objectiveText.text = string.IsNullOrEmpty(text) ? "" : Loc.T("ЦЕЛЬ") + "  ·  " + Loc.T(text);
        }

        public void SetHudVisible(bool visible)
        {
            objectiveText.gameObject.SetActive(visible);
            InventoryBarUI bar = GetComponent<InventoryBarUI>();
            if (bar != null)
            {
                bar.SetVisible(visible);
            }

            PocketWatchUI watch = GetComponent<PocketWatchUI>();
            if (watch != null)
            {
                watch.SetVisible(visible);
            }
        }

        // ---------------------------------------------------------------- internals

        private void Relocalize()
        {
            if (cardPanel.activeSelf && rawCard != null)
            {
                cardText.text = Loc.T(rawCard);
                cardHint.text = Loc.T(CardHintText);
            }

            if (toastText.gameObject.activeSelf && rawToast != null)
            {
                toastText.text = Loc.T(rawToast);
            }

            SetObjective(rawObjective);
            RenderLanguageSwitch();
        }

        // ---------------------------------------------------------------- language switch

        private void BuildLanguageSwitch()
        {
            // Own canvas above the fader and cards, so the language can be changed at any moment.
            var go = new GameObject("LanguageSwitch", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(transform, false);
            var switchCanvas = go.GetComponent<Canvas>();
            switchCanvas.overrideSorting = true;
            switchCanvas.sortingOrder = 130;
            languageRoot = (RectTransform)go.transform;

            const float labelWidth = 56f;
            IReadOnlyList<Loc.Language> langs = Loc.Languages;
            Place(languageRoot, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -32), new Vector2(labelWidth * langs.Count, 40));
            for (int i = 0; i < langs.Count; i++)
            {
                Text label = MakeText("Lang_" + langs[i].Code, languageRoot, 24, mutedColor, TextAnchor.MiddleCenter);
                label.text = langs[i].Code.ToUpperInvariant();
                label.fontStyle = FontStyle.Bold;
                Place(label.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(i * labelWidth, 0), new Vector2(labelWidth, 40));
                languageLabels.Add(label);
                languageCodes.Add(langs[i].Code);
            }

            RenderLanguageSwitch();
        }

        private void UpdateLanguageSwitch()
        {
            int hovered = -1;
            if (PointerOverHud)
            {
                for (int i = 0; i < languageLabels.Count; i++)
                {
                    if (RectTransformUtility.RectangleContainsScreenPoint(languageLabels[i].rectTransform, Input.mousePosition, null))
                    {
                        hovered = i;
                        break;
                    }
                }
            }

            if (hovered != hoveredLanguage)
            {
                hoveredLanguage = hovered;
                RenderLanguageSwitch();
            }

            if (hovered >= 0 && Input.GetMouseButtonDown(0))
            {
                Loc.SetLanguage(languageCodes[hovered]);
            }
        }

        private void RenderLanguageSwitch()
        {
            for (int i = 0; i < languageLabels.Count; i++)
            {
                bool active = languageCodes[i] == Loc.CurrentCode;
                languageLabels[i].color = active ? speakerColor : i == hoveredLanguage ? textColor : mutedColor;
            }
        }

        private IEnumerator ToastRoutine(string text, float seconds)
        {
            rawToast = text;
            toastText.text = Loc.T(text);
            toastText.gameObject.SetActive(true);
            yield return FadeGraphics(new Graphic[] { toastText }, 0f, 1f, 0.25f);
            yield return new WaitForSeconds(seconds);
            yield return FadeGraphics(new Graphic[] { toastText }, 1f, 0f, 0.5f);
            toastText.gameObject.SetActive(false);
            toastRoutine = null;
        }

        private static IEnumerator FadeGraphics(Graphic[] graphics, float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(from, to, t / duration);
                foreach (Graphic g in graphics)
                {
                    Color c = g.color;
                    c.a = a;
                    g.color = c;
                }

                yield return null;
            }

            foreach (Graphic g in graphics)
            {
                Color c = g.color;
                c.a = to;
                g.color = c;
            }
        }

        private void Build()
        {
            canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Objective, top-left.
            objectiveText = MakeText("Objective", transform, 24, mutedColor, TextAnchor.UpperLeft);
            Place(objectiveText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(48, -40), new Vector2(1200, 40));

            // Toast, top-centre.
            toastText = MakeText("Toast", transform, 28, textColor, TextAnchor.MiddleCenter);
            Place(toastText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(1400, 48));
            toastText.gameObject.SetActive(false);

            // Full-screen card on its own canvas, above the ScreenFader (sorting order 100).
            cardPanel = new GameObject("Card", typeof(RectTransform), typeof(Canvas), typeof(Image));
            cardPanel.transform.SetParent(transform, false);
            var cardCanvas = cardPanel.GetComponent<Canvas>();
            cardCanvas.overrideSorting = true;
            cardCanvas.sortingOrder = 120;
            cardPanel.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.025f, 1f);
            var crt = (RectTransform)cardPanel.transform;
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = Vector2.zero;
            cardText = MakeText("CardText", cardPanel.transform, 34, textColor, TextAnchor.MiddleCenter);
            cardText.lineSpacing = 1.25f;
            Place(cardText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300, 700));
            cardHint = MakeText("CardHint", cardPanel.transform, 24, mutedColor, TextAnchor.MiddleCenter);
            Place(cardHint.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(1300, 40));
            cardHint.gameObject.SetActive(false);
            cardPanel.SetActive(false);

            BuildLanguageSwitch();
        }

        public Text MakeText(string name, Transform parent, int size, Color color, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using LoneTruth.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LoneTruth.UI
{
    /// <summary>
    /// Screen-space game UI that survives scene loads: dialogue box, full-screen text card,
    /// toasts and the current objective. Built from code in Awake so the prefab stays a single
    /// component; the inventory bar (InventoryBarUI) lives on the same object.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        public static GameUI Instance { get; private set; }

        [Header("Look")]
        [SerializeField] private Color panelColor = new Color(0.04f, 0.045f, 0.055f, 0.88f);
        [SerializeField] private Color speakerColor = new Color(0.78f, 0.57f, 0.18f);
        [SerializeField] private Color textColor = new Color(0.93f, 0.9f, 0.85f);
        [SerializeField] private Color mutedColor = new Color(0.64f, 0.62f, 0.56f);
        [SerializeField] private float charactersPerSecond = 55f;
        [Tooltip("Seconds a card that waits for input stays up before the \"press E\" hint appears.")]
        [SerializeField] private float cardHintDelay = 4.5f;

        private Font font;
        private Canvas canvas;
        private GameObject dialoguePanel;
        private Text speakerText;
        private Text lineText;
        private Text continueHint;
        private GameObject cardPanel;
        private Text cardText;
        private Text cardHint;
        private Text toastText;
        private Text objectiveText;
        private Coroutine toastRoutine;

        // Untranslated text currently on screen, so a language switch can re-render it.
        private string rawSpeaker;
        private string rawLine;
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

        /// <summary>One line of dialogue. Waits for the player; a press during the reveal completes it.</summary>
        public IEnumerator ShowLine(string speaker, string text)
        {
            dialoguePanel.SetActive(true);
            rawSpeaker = speaker;
            rawLine = text;
            RenderSpeaker();
            speakerText.gameObject.SetActive(!string.IsNullOrEmpty(speaker));
            // Holt's inner voice (speaker "") is set in italics, like a voice-over.
            lineText.fontStyle = string.IsNullOrEmpty(speaker) ? FontStyle.Italic : FontStyle.Normal;
            continueHint.gameObject.SetActive(false);

            yield return Reveal(lineText);
            continueHint.gameObject.SetActive(true);
            yield return null;
            while (!AdvancePressed())
            {
                yield return null;
            }

            dialoguePanel.SetActive(false);
            rawLine = null;
            yield return null;
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

        private IEnumerator Reveal(Text target)
        {
            target.text = "";
            float shown = 0f;
            yield return null;
            // Re-read the translation every frame: the language may be switched mid-line.
            while (shown < Loc.T(rawLine).Length)
            {
                if (AdvancePressed())
                {
                    break;
                }

                string text = Loc.T(rawLine);
                shown += Time.unscaledDeltaTime * charactersPerSecond;
                target.text = text.Substring(0, Mathf.Min(text.Length, Mathf.FloorToInt(shown)));
                yield return null;
            }

            target.text = Loc.T(rawLine);
        }

        private void RenderSpeaker()
        {
            speakerText.text = string.IsNullOrEmpty(rawSpeaker) ? "" : Loc.T(rawSpeaker).ToUpperInvariant();
        }

        private void Relocalize()
        {
            if (dialoguePanel.activeSelf && rawLine != null)
            {
                RenderSpeaker();
                if (continueHint.gameObject.activeSelf)
                {
                    lineText.text = Loc.T(rawLine);
                }
            }

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

            // Dialogue box, bottom, above the inventory bar.
            dialoguePanel = new GameObject("Dialogue", typeof(RectTransform), typeof(Image));
            dialoguePanel.transform.SetParent(transform, false);
            dialoguePanel.GetComponent<Image>().color = panelColor;
            Place((RectTransform)dialoguePanel.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 200), new Vector2(1400, 190));
            speakerText = MakeText("Speaker", dialoguePanel.transform, 24, speakerColor, TextAnchor.UpperLeft);
            speakerText.fontStyle = FontStyle.Bold;
            Place(speakerText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -22), new Vector2(800, 34));
            lineText = MakeText("Line", dialoguePanel.transform, 30, textColor, TextAnchor.UpperLeft);
            Place(lineText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -62), new Vector2(1328, 110));
            continueHint = MakeText("Continue", dialoguePanel.transform, 20, mutedColor, TextAnchor.LowerRight);
            continueHint.text = "[E]";
            Place(continueHint.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-28, 16), new Vector2(200, 30));
            dialoguePanel.SetActive(false);

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

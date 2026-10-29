using System;
using System.Collections;
using System.Collections.Generic;
using LoneTruth.Core;
using LoneTruth.Dialogue;
using LoneTruth.Investigation;
using UnityEngine;
using UnityEngine.UI;

namespace LoneTruth.UI
{
    /// <summary>
    /// The dialogue panel at the bottom of the screen (design doc 3.2): the speaker's portrait,
    /// the line, and 2–4 answers. Answers that give a shard are marked with a coloured gem only
    /// after the choice — or always, with the accessibility option ShardColorsBeforeChoice.
    /// Cutscene lines (GameUI.ShowLine) use the same panel.
    /// </summary>
    [RequireComponent(typeof(GameUI))]
    public class DialogueUI : MonoBehaviour
    {
        public static DialogueUI Instance { get; private set; }

        private const string ShardColorsPref = "a11y_shard_colors_before_choice";
        private const int MaxChoices = 4;

        [SerializeField] private SpeakerPortraitsSO portraits;
        [SerializeField] private Color panelColor = new Color(0.04f, 0.045f, 0.055f, 0.9f);
        [SerializeField] private Color speakerColor = new Color(0.78f, 0.57f, 0.18f);
        [SerializeField] private Color textColor = new Color(0.93f, 0.9f, 0.85f);
        [SerializeField] private Color mutedColor = new Color(0.55f, 0.53f, 0.49f);
        [SerializeField] private Color clueColor = new Color(0.8f, 0.9f, 1f);
        [SerializeField] private Color hopeColor = new Color(0.96f, 0.66f, 0.2f);
        [SerializeField] private Color despairColor = new Color(0.62f, 0.32f, 0.78f);
        [SerializeField] private Color truthColor = new Color(0.86f, 0.94f, 1f);
        [SerializeField] private float charactersPerSecond = 55f;
        [SerializeField] private float shardRevealSeconds = 0.9f;

        /// <summary>Accessibility: show the shard colour of answers before choosing (saved per player).</summary>
        public static bool ShardColorsBeforeChoice
        {
            get => PlayerPrefs.GetInt(ShardColorsPref, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(ShardColorsPref, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Tests: with GameUI.AutoAdvance on, the answer picked automatically.</summary>
        public static int AutoPick;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            AutoPick = 0;
        }

        private class Row
        {
            public RectTransform Root;
            public Text Label;
            public Image Gem;
        }

        private GameUI ui;
        private GameObject panel;
        private RectTransform portraitFrame;
        private Image portrait;
        private Text speakerText;
        private Text lineText;
        private Text continueHint;
        private Text choiceHint;
        private readonly List<Row> rows = new List<Row>();

        private string rawSpeaker;
        private string rawLine;
        private List<ChoiceView> shownChoices;
        private int selected;

        public void SetPortraits(SpeakerPortraitsSO speakerPortraits)
        {
            portraits = speakerPortraits;
        }

        private void Awake()
        {
            if (GameUI.Instance != null && GameUI.Instance.gameObject != gameObject)
            {
                return; // a duplicate GameUI is destroyed this frame
            }

            Instance = this;
            ui = GetComponent<GameUI>();
        }

        private void Start()
        {
            // After GameUI.Awake, which sets up the font MakeText uses.
            if (Instance == this)
            {
                Build();
            }
        }

        private void OnEnable()
        {
            Loc.Changed += Relocalize;
        }

        private void OnDisable()
        {
            Loc.Changed -= Relocalize;
        }

        // ---------------------------------------------------------------- lines

        /// <summary>One line. Waits for the player; a press during the reveal completes it.</summary>
        public IEnumerator ShowLine(string speaker, string text)
        {
            Open(speaker, text);
            SetRows(null);
            continueHint.gameObject.SetActive(false);
            yield return Reveal();
            continueHint.gameObject.SetActive(true);
            yield return null;
            while (!GameUI.AdvancePressed())
            {
                yield return null;
            }

            Hide();
            yield return null;
        }

        // ---------------------------------------------------------------- choices

        /// <summary>Shows the line with its answers and reports the picked index.</summary>
        public IEnumerator ShowChoices(string speaker, string text, List<ChoiceView> choices, Action<int> picked)
        {
            Open(speaker, text);
            continueHint.gameObject.SetActive(false);
            SetRows(null);
            yield return Reveal();

            selected = FirstOpen(choices, 0, 1);
            SetRows(choices);
            yield return null;

            int chosen = -1;
            float autoTimer = 0f;
            while (chosen < 0)
            {
                if (GameUI.AutoAdvance)
                {
                    autoTimer += Time.unscaledDeltaTime;
                    if (autoTimer > 0.4f)
                    {
                        chosen = FirstOpen(choices, Mathf.Clamp(AutoPick, 0, choices.Count - 1), 1);
                    }
                }
                else
                {
                    chosen = ReadInput(choices);
                }

                yield return null;
            }

            selected = chosen;
            RenderRows();
            if (choices[chosen].HasShard)
            {
                // The shard colour is revealed by the choice itself.
                Row row = rows[chosen];
                row.Gem.color = ShardColor(choices[chosen].Shard);
                row.Gem.gameObject.SetActive(true);
                for (float t = 0f; t < shardRevealSeconds; t += Time.unscaledDeltaTime)
                {
                    row.Gem.rectTransform.localScale = Vector3.one * (1f + 0.35f * Mathf.Sin(Mathf.Clamp01(t / 0.35f) * Mathf.PI));
                    yield return null;
                }
            }

            picked(chosen);
            SetRows(null);
        }

        public void Hide()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }

            rawLine = null;
            shownChoices = null;
        }

        private int ReadInput(List<ChoiceView> choices)
        {
            for (int i = 0; i < choices.Count && i < MaxChoices; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) && !choices[i].Locked)
                {
                    return i;
                }
            }

            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.mouseScrollDelta.y > 0.1f)
            {
                selected = FirstOpen(choices, selected - 1, -1);
                RenderRows();
            }

            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) || Input.mouseScrollDelta.y < -0.1f)
            {
                selected = FirstOpen(choices, selected + 1, 1);
                RenderRows();
            }

            for (int i = 0; i < choices.Count; i++)
            {
                if (!choices[i].Locked && RectTransformUtility.RectangleContainsScreenPoint(rows[i].Root, Input.mousePosition, null))
                {
                    if (selected != i)
                    {
                        selected = i;
                        RenderRows();
                    }

                    if (Input.GetMouseButtonDown(0))
                    {
                        return i;
                    }
                }
            }

            bool confirm = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
            return confirm && selected >= 0 && !choices[selected].Locked ? selected : -1;
        }

        /// <summary>The next answer that can be picked, walking from start in direction dir (wraps).</summary>
        private static int FirstOpen(List<ChoiceView> choices, int start, int dir)
        {
            int n = choices.Count;
            for (int k = 0; k < n; k++)
            {
                int i = ((start + k * dir) % n + n) % n;
                if (!choices[i].Locked)
                {
                    return i;
                }
            }

            return -1;
        }

        // ---------------------------------------------------------------- rendering

        private void Open(string speaker, string text)
        {
            panel.SetActive(true);
            rawSpeaker = speaker;
            rawLine = text;
            speakerText.text = string.IsNullOrEmpty(speaker) ? "" : Loc.T(speaker).ToUpperInvariant();
            // Holt's inner voice (speaker "") is set in italics, like a voice-over.
            lineText.fontStyle = string.IsNullOrEmpty(speaker) ? FontStyle.Italic : FontStyle.Normal;
            Sprite face = portraits != null ? portraits.Find(speaker) : null;
            portraitFrame.gameObject.SetActive(face != null);
            if (face != null)
            {
                portrait.sprite = face;
                // Show the head and shoulders: the top ~40% of a full-figure sprite.
                float height = portraitFrame.rect.height / 0.4f;
                float width = height * face.rect.width / face.rect.height;
                portrait.rectTransform.sizeDelta = new Vector2(width, height);
            }

            float textX = face != null ? 230f : 40f;
            speakerText.rectTransform.anchoredPosition = new Vector2(textX, -22f);
            lineText.rectTransform.anchoredPosition = new Vector2(textX, -60f);
            lineText.rectTransform.sizeDelta = new Vector2(1460f - textX, 96f);
            foreach (Row row in rows)
            {
                row.Root.anchoredPosition = new Vector2(textX, row.Root.anchoredPosition.y);
            }
        }

        private IEnumerator Reveal()
        {
            lineText.text = "";
            float shown = 0f;
            yield return null;
            // Re-read the translation every frame: the language may be switched mid-line.
            while (shown < Loc.T(rawLine).Length)
            {
                if (GameUI.AdvancePressed())
                {
                    break;
                }

                string text = Loc.T(rawLine);
                shown += Time.unscaledDeltaTime * charactersPerSecond;
                lineText.text = text.Substring(0, Mathf.Min(text.Length, Mathf.FloorToInt(shown)));
                yield return null;
            }

            lineText.text = Loc.T(rawLine);
        }

        private void SetRows(List<ChoiceView> choices)
        {
            shownChoices = choices;
            choiceHint.gameObject.SetActive(choices != null);
            RenderRows();
        }

        private void RenderRows()
        {
            for (int i = 0; i < rows.Count; i++)
            {
                Row row = rows[i];
                bool on = shownChoices != null && i < shownChoices.Count;
                row.Root.gameObject.SetActive(on);
                if (!on)
                {
                    continue;
                }

                ChoiceView c = shownChoices[i];
                bool isSelected = i == selected;
                string label = Loc.T(c.Text);
                if (!string.IsNullOrEmpty(c.ClueTitle))
                {
                    label = $"[{Loc.T("Предъявить")}: {Loc.T(c.ClueTitle)}]  {label}";
                }

                if (c.Locked && !string.IsNullOrEmpty(c.LockedHint))
                {
                    label += $"  ({Loc.T(c.LockedHint)})";
                }

                row.Label.text = $"{(isSelected ? "›" : " ")} {i + 1}.  {label}";
                row.Label.color = c.Locked ? mutedColor : isSelected ? speakerColor : !string.IsNullOrEmpty(c.ClueTitle) ? clueColor : textColor;
                row.Label.fontStyle = isSelected && !c.Locked ? FontStyle.Bold : FontStyle.Normal;
                row.Gem.gameObject.SetActive(c.HasShard && ShardColorsBeforeChoice);
                row.Gem.color = ShardColor(c.Shard);
                row.Gem.rectTransform.localScale = Vector3.one;
            }

            choiceHint.text = Loc.T("W / S или 1–4 — выбрать ответ   ·   E — ответить");
        }

        private Color ShardColor(ShardType type)
        {
            switch (type)
            {
                case ShardType.Hope: return hopeColor;
                case ShardType.Despair: return despairColor;
                default: return truthColor;
            }
        }

        private void Relocalize()
        {
            if (panel == null || !panel.activeSelf || rawLine == null)
            {
                return;
            }

            speakerText.text = string.IsNullOrEmpty(rawSpeaker) ? "" : Loc.T(rawSpeaker).ToUpperInvariant();
            if (continueHint.gameObject.activeSelf || shownChoices != null)
            {
                lineText.text = Loc.T(rawLine);
            }

            RenderRows();
        }

        // ---------------------------------------------------------------- building

        private void Build()
        {
            panel = new GameObject("DialoguePanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            panel.GetComponent<Image>().color = panelColor;
            panel.GetComponent<Image>().raycastTarget = false;
            GameUI.Place((RectTransform)panel.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(1500, 300));

            var frameGo = new GameObject("PortraitFrame", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            frameGo.transform.SetParent(panel.transform, false);
            frameGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);
            portraitFrame = (RectTransform)frameGo.transform;
            GameUI.Place(portraitFrame, new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -26), new Vector2(170, 210));
            var faceGo = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            faceGo.transform.SetParent(portraitFrame, false);
            portrait = faceGo.GetComponent<Image>();
            portrait.raycastTarget = false;
            GameUI.Place(portrait.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(170, 500));

            speakerText = ui.MakeText("Speaker", panel.transform, 24, speakerColor, TextAnchor.UpperLeft);
            speakerText.fontStyle = FontStyle.Bold;
            GameUI.Place(speakerText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(230, -22), new Vector2(900, 34));
            lineText = ui.MakeText("Line", panel.transform, 28, textColor, TextAnchor.UpperLeft);
            GameUI.Place(lineText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(230, -60), new Vector2(1230, 96));

            for (int i = 0; i < MaxChoices; i++)
            {
                var rowGo = new GameObject($"Choice_{i}", typeof(RectTransform));
                rowGo.transform.SetParent(panel.transform, false);
                var row = new Row { Root = (RectTransform)rowGo.transform };
                GameUI.Place(row.Root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(230, -160 - i * 32), new Vector2(1240, 32));
                row.Label = ui.MakeText("Label", row.Root, 24, textColor, TextAnchor.MiddleLeft);
                GameUI.Place(row.Label.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(26, 0), new Vector2(1210, 32));

                var gemGo = new GameObject("ShardGem", typeof(RectTransform), typeof(Image));
                gemGo.transform.SetParent(row.Root, false);
                row.Gem = gemGo.GetComponent<Image>();
                row.Gem.raycastTarget = false;
                GameUI.Place(row.Gem.rectTransform, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(8, 0), new Vector2(11, 11));
                row.Gem.rectTransform.localEulerAngles = new Vector3(0, 0, 45f);
                gemGo.SetActive(false);
                rowGo.SetActive(false);
                rows.Add(row);
            }

            continueHint = ui.MakeText("Continue", panel.transform, 20, mutedColor, TextAnchor.LowerRight);
            continueHint.text = "[E]";
            GameUI.Place(continueHint.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-28, 14), new Vector2(200, 30));
            choiceHint = ui.MakeText("ChoiceHint", panel.transform, 18, mutedColor, TextAnchor.LowerRight);
            GameUI.Place(choiceHint.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-28, 12), new Vector2(900, 28));

            panel.SetActive(false);
        }
    }
}
